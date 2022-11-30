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

    private static readonly LocalizableString Title = "Use case 3 - Volatile mutator method";
    private static readonly LocalizableString MessageFormat = "Mutator method is called in an unsynchronized manner.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
     DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
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
        //continues analysis if there is a lock, it's a sign of multi-threaded usage.
        if(!classNode.DescendantNodes().OfType<LockStatementSyntax>().Any())
          return;

        //getsevery method with an assignment an without lock
        IEnumerable<MemberDeclarationSyntax> methodMembers = GetMethods(classNode);

        foreach(var publicMember in methodMembers) {
          // if the lef in the assginment is a private && not locked then its trouble!!
          var expressions = publicMember.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>();

          foreach(var exp in expressions) {
            //public fields would be flagged by AssignmentInsideLockAnalyzer
            if(IsNameSyntaxPrivateField(GetLeftAssingment(exp), model)) {
              var diagnostic = Diagnostic.Create(Rule, exp.GetLocation(), MessageFormat);
              context.ReportDiagnostic(diagnostic);
            }

          }
        }

      }


    }

    private IEnumerable<MemberDeclarationSyntax> GetMethods(ClassDeclarationSyntax classNode) {
      return classNode.Members.Where(m => m is MethodDeclarationSyntax && m.DescendantNodesAndSelf().All(dn => !(dn is LockStatementSyntax)));
    }

    private IdentifierNameSyntax GetLeftAssingment(AssignmentExpressionSyntax assignmentExpression) {
      return assignmentExpression.Left.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().FirstOrDefault();
    }
    private bool IsNameSyntaxPrivateField(IdentifierNameSyntax identifierSyntax, SemanticModel model) {
      var symbolInfo = model.GetSymbolInfo(identifierSyntax).Symbol;
      return symbolInfo is IFieldSymbol && symbolInfo.DeclaredAccessibility == Accessibility.Private;
    }
  }
}
