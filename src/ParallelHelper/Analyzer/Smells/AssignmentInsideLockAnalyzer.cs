using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
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
    protected List<IFieldSymbol> publicFields;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      publicFields = new List<IFieldSymbol>();
      context.EnableConcurrentExecution();
      //get public members first
      context.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
      context.RegisterSyntaxNodeAction(AnalyzeLockStatement, SyntaxKind.LockStatement);
    }

    private void AnalyzeField(SyntaxNodeAnalysisContext context) {
      var declarationSyntax = context.Node as FieldDeclarationSyntax;


      if(declarationSyntax != null) {
        foreach(var variableDeclaration in declarationSyntax.Declaration.Variables) {
          if(context.SemanticModel.GetDeclaredSymbol(variableDeclaration) is IFieldSymbol variableDeclarationSymbol 
            && IsFieldPublic(variableDeclarationSymbol)) {
            var asd = variableDeclarationSymbol as IFieldSymbol;
            publicFields.Add(asd);
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
        if(publicFields.Any(pf => pf.Name == ident.Identifier.Text)) {
          var location = lockStatement.GetLocation();
          var diagnostic = Diagnostic.Create(Rule, location, "Assignment is used");

          ctx.ReportDiagnostic(diagnostic);
        }

      }
    }
  }
}
