using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using ParallelHelper.Util;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.Smells {
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class AssignmentInsideLockAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_BT003";
    private const string Category = "Locking";

    private static readonly LocalizableString Title = "Assignment inside a lock";
    private static readonly LocalizableString MessageFormat = "A variable has been assigned inside the lock";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
     DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error,
     isEnabledByDefault: true, description: Description, helpLinkUri: ""//gets the .md file from parallell helper github HelpLinkFactory.CreateUri(DiagnosticId)
   );


    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.EnableConcurrentExecution();
      context.RegisterSyntaxNodeAction(AnalyzeLock, SyntaxKind.ClassDeclaration);
    }

    private void AnalyzeLock(SyntaxNodeAnalysisContext obj) {
      new Analyzer(obj).Analyze();
    }

    //private void AnalyzeOperation(OperationAnalysisContext context) {
    //  var operation = context.Operation as IBinaryOperation;
    //  if(operation != null) {
    //    var left = operation.LeftOperand as IFieldReferenceOperation;
    //    //If the field is not public yet used in an operation it still can cause error
    //    //the issue is the same as with pubic members, it shoulld be locked
    //    if(left != null && !IsFieldPublic(left.Field)) {
    //      //     publicMembers.Add(left.Field.Name);
    //    }
    //  }
    //}

    //private void AnalyzeLockStatement(SyntaxNodeAnalysisContext ctx) {
    //  var lockStatement = ctx.Node as LockStatementSyntax;


    //  if(lockStatement != null) {
    //    var assignment = lockStatement.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>().FirstOrDefault();
    //    var ident = assignment.Left.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().FirstOrDefault();
    //    if(publicIdentifiers.Any(pf => pf == ident.Identifier)) {
    //      var location = lockStatement.GetLocation();
    //      var diagnostic = Diagnostic.Create(Rule, location, "Assignment is used");

    //      ctx.ReportDiagnostic(diagnostic);
    //    }

    //  }
    //}

    private class Analyzer : InternalAnalyzerBase<SyntaxNode> {
      private readonly TaskAnalysis _taskAnalysis;
      protected List<SyntaxToken> publicIdentifiers;
      private SyntaxNodeAnalysisContext _nodeAnalysisContext;
      Location foundLocation = null;
      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper(context)) {
        _taskAnalysis = new TaskAnalysis(context.SemanticModel, context.CancellationToken);
        publicIdentifiers = new List<SyntaxToken>();
        _nodeAnalysisContext = context;
        
      }

      public override void Analyze() {
        var classNode = _nodeAnalysisContext.Node as ClassDeclarationSyntax;

        //get the public members
        //theres no need to check the accessor list as intellisense check if the operation is legal with the property
        var publicMembers = classNode.Members.Where(m => m is MemberDeclarationSyntax && m.Modifiers.Any(SyntaxKind.PublicKeyword));

        // write to report: I can do this here as SyntaxNodeAnalysisContext is C# implementation dependent and I know the first variable is what needed.
        var fieldVariables = publicMembers.Where(p => p is FieldDeclarationSyntax).Select(p => ((FieldDeclarationSyntax)p).Declaration.Variables.FirstOrDefault());
        var properties = publicMembers.Where(p => p is PropertyDeclarationSyntax);
        var propertyIdentifiers = properties.Select(p => ((PropertyDeclarationSyntax)p).Identifier);

        //gets all the fields behind public properties
        foreach(var identifierSyntax in properties.SelectMany(pm => pm.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())) {

          var symbolInfo = SemanticModel.GetSymbolInfo(identifierSyntax).Symbol;

          //even if its not public its accesible from a public property, this should raise error
          if(symbolInfo is IFieldSymbol && symbolInfo.DeclaredAccessibility != Accessibility.Public) {
            publicIdentifiers.Add(identifierSyntax.Identifier);
          }
        }

        foreach(var item in publicMembers) {
          var sd = item.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>();
          foreach(var bitem in sd) {
            if(bitem != null && bitem.Left.Kind() == SyntaxKind.IdentifierName) {
              var leftIdentifier = (IdentifierNameSyntax)bitem.Left;
              //TODO make this into a func I use it elsewhere also
              var symbolInfo = SemanticModel.GetSymbolInfo(leftIdentifier).Symbol;
              if(symbolInfo.DeclaredAccessibility == Accessibility.Private) {
                publicIdentifiers.Add(leftIdentifier.Identifier);
                foundLocation = bitem.GetLocation();
              }
            }

          }
        }
          publicIdentifiers.AddRange(propertyIdentifiers);
          publicIdentifiers.AddRange(fieldVariables.Select(s => s.Identifier));

          //get the locks
          var locks = classNode.DescendantNodes().OfType<LockStatementSyntax>();

          foreach(var lockStatement in locks) {
            var assignment = lockStatement.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>().FirstOrDefault();
            var ident = assignment.Left.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().FirstOrDefault();
            if(publicIdentifiers.Any(pf => IsSyntaxTokenEquals(pf, ident.Identifier))) {
              var location = foundLocation ?? lockStatement.GetLocation();
              var diagnostic = Diagnostic.Create(Rule, location, "Assignment is used");

              Context.ReportDiagnostic(diagnostic);
            }
          }

        }
        private bool IsFieldPublic(IFieldSymbol fieldSymbol) {
          return fieldSymbol.DeclaredAccessibility == Accessibility.Public;
        }
        private bool IsSyntaxTokenEquals(SyntaxToken first, SyntaxToken other) {
          return first.Text == other.Text;
        }
      }
    }
  }

