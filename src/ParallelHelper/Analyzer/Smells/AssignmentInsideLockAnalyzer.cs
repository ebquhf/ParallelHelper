using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
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
    protected List<string> publicMembers;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      publicMembers = new List<string>();
      context.EnableConcurrentExecution();
      //get public members first
      context.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
      context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);

      context.RegisterOperationAction(AnalyzeOperation, OperationKind.Binary);
      context.RegisterSyntaxNodeAction(AnalyzeLockStatement, SyntaxKind.LockStatement);
    }

    private void AnalyzeOperation(OperationAnalysisContext context) {
      var operation = context.Operation as IBinaryOperation;
      if(operation != null) {
        var left = operation.LeftOperand as IFieldReferenceOperation;
        if(left != null && !IsFieldPublic(left.Field)) {

        }
      }
    }

    private void AnalyzeProperty(SyntaxNodeAnalysisContext context) {
      var declarationSyntax = context.Node as PropertyDeclarationSyntax;


      if(declarationSyntax != null) {

        //theres no need to check the accessor list as intellisense check if the operation is legal with the property
        if(declarationSyntax.Modifiers.Any(SyntaxKind.PublicKeyword)) {
          publicMembers.Add(declarationSyntax.Identifier.Text);
        }

      }
    }

    private void AnalyzeField(SyntaxNodeAnalysisContext context) {
      var declarationSyntax = context.Node as FieldDeclarationSyntax;


      if(declarationSyntax != null) {
        foreach(var variableDeclaration in declarationSyntax.Declaration.Variables) {
          if(context.SemanticModel.GetDeclaredSymbol(variableDeclaration) is IFieldSymbol variableDeclarationSymbol
            && IsFieldPublic(variableDeclarationSymbol)) {
            var fieldSymbol = variableDeclarationSymbol as IFieldSymbol;
            publicMembers.Add(fieldSymbol.Name);
          }
        }
      }
    }

    private bool IsFieldPublic(IFieldSymbol fieldSymbol) {
      return fieldSymbol.DeclaredAccessibility == Accessibility.Public;
    }
    private void AnalyzeLockStatement(SyntaxNodeAnalysisContext ctx) {
      var lockStatement = ctx.Node as LockStatementSyntax;


      if(lockStatement != null) {
        var assignment = lockStatement.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>().FirstOrDefault();
        var ident = assignment.Left.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().FirstOrDefault();
        if(publicMembers.Any(pf => pf == ident.Identifier.Text)) {
          var location = lockStatement.GetLocation();
          var diagnostic = Diagnostic.Create(Rule, location, "Assignment is used");

          ctx.ReportDiagnostic(diagnostic);
        }

      }
    }
  }
}

