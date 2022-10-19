using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelHelper.Analyzer.Smells {
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class MethodCallInsideLockAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_BT004";
    private const string Category = "Locking";

    private static readonly LocalizableString Title = "Method call in a lock";
    private static readonly LocalizableString MessageFormat = "A variable has been assigned inside the lock";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
     DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error,
     isEnabledByDefault: true, description: Description, helpLinkUri: ""//gets the .md file from parallell helper github HelpLinkFactory.CreateUri(DiagnosticId)
   );
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
    public override void Initialize(AnalysisContext context) {
      context.RegisterSyntaxNodeAction(AnalyzeLockStatement, SyntaxKind.ClassDeclaration);
    }

    private void AnalyzeLockStatement(SyntaxNodeAnalysisContext context) {
      var classNode = context.Node as ClassDeclarationSyntax;
      var model = context.SemanticModel;

      if(model != null && classNode != null) {

        var lockstatement = classNode.DescendantNodes().OfType<LockStatementSyntax>().FirstOrDefault();
        var publicMembers = classNode.Members.Where(m => m is MethodDeclarationSyntax && m.Modifiers.Any(SyntaxKind.PublicKeyword));
        foreach(var publicMember in publicMembers) {
          // if the lef in the assginment is a private && not locked then its trouble!!
          var expressions = publicMember.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>();
          foreach(var exp in expressions) {
            if(IsNameSyntaxPublicField(GetLeftAssingment(exp),model)) {
              var diagnostic = Diagnostic.Create(Rule, exp.GetLocation(), "some clever name");

              context.ReportDiagnostic(diagnostic);
            }
           
          }
        }

      }


    }

    private IdentifierNameSyntax GetLeftAssingment(AssignmentExpressionSyntax assignmentExpression) {
      return assignmentExpression.Left.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().FirstOrDefault();
    }
    private bool IsNameSyntaxPublicField(IdentifierNameSyntax identifierSyntax, SemanticModel model) {
      var symbolInfo = model.GetSymbolInfo(identifierSyntax).Symbol;
      return symbolInfo is IFieldSymbol && symbolInfo.DeclaredAccessibility == Accessibility.Public;
    }
  }
}
