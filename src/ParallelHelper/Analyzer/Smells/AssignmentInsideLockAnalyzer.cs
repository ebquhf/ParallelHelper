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
     DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
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
        //the accessor list will be checked for the properties
        var publicMembers = classNode.Members.Where(m => m is MemberDeclarationSyntax && m.Modifiers.Any(SyntaxKind.PublicKeyword));

        // write to report: I can do this here as SyntaxNodeAnalysisContext is C# implementation dependent and I know the first variable is what needed.
        var fieldVariables = publicMembers.Where(p => p is FieldDeclarationSyntax).Select(p => ((FieldDeclarationSyntax)p).Declaration.Variables.FirstOrDefault());
        var properties = publicMembers.Where(p => p is PropertyDeclarationSyntax);
        var propertyIdentifiers = properties.Select(p => ((PropertyDeclarationSyntax)p).Identifier);

        //gets all the fields behind public properties
        AnalyzeFieldsBehindProperties(propertyIdentifiers, properties);

        //Get the insufficiently locked fields
        AnalyzeFieldsInBinaryOperations(propertyIdentifiers, publicMembers);

        publicIdentifiers.AddRange(propertyIdentifiers);
        publicIdentifiers.AddRange(fieldVariables.Select(s => s.Identifier));

        //get the locks
        var locks = classNode.DescendantNodes().OfType<LockStatementSyntax>();
        if(locks != null) {
          AnalyzeLocks(locks, publicIdentifiers);
        }
      }

      private void AnalyzeLocks(IEnumerable<LockStatementSyntax> locks, List<SyntaxToken> publicIdentifiers) {
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

      private void AnalyzeFieldsInBinaryOperations(IEnumerable<SyntaxToken> propertyIdentifiers, IEnumerable<MemberDeclarationSyntax> publicMembers) {
        foreach(var item in publicMembers) {
          var expressionSyntaxes = item.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>();
          foreach(var expression in expressionSyntaxes) {
            if(expression != null && expression.Left.Kind() == SyntaxKind.IdentifierName) {
              var leftIdentifier = (IdentifierNameSyntax)expression.Left;
              if(IsNameSyntaxNotPublicField(leftIdentifier)) {
                publicIdentifiers.Add(leftIdentifier.Identifier);
                foundLocation = expression.GetLocation();
              }
            }

          }
        }
      }

      private void AnalyzeFieldsBehindProperties(IEnumerable<SyntaxToken> propertyIdentifiers, IEnumerable<MemberDeclarationSyntax> properties) {
        foreach(var identifierSyntax in properties.SelectMany(pm => pm.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())) {

          //even if its not public its accesible from a public property, this should raise error
          if(IsNameSyntaxNotPublicField(identifierSyntax)) {
            publicIdentifiers.Add(identifierSyntax.Identifier);
          }
        }
      }

      private bool IsNameSyntaxNotPublicField(IdentifierNameSyntax identifierSyntax) {
        var symbolInfo = SemanticModel.GetSymbolInfo(identifierSyntax).Symbol;
        return symbolInfo is IFieldSymbol && symbolInfo.DeclaredAccessibility != Accessibility.Public;
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

