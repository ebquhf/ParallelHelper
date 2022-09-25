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
      context.RegisterSyntaxNodeAction(AnalyzeLockStatement, SyntaxKind.LockStatement);
    }

    private void AnalyzeLockStatement(SyntaxNodeAnalysisContext context) {
      var model = context.SemanticModel;
      var lockstatement = context.Node as LockStatementSyntax;
      if(model != null && lockstatement!=null) {

        ControlFlowAnalysis result = model.AnalyzeControlFlow(lockstatement);

        Console.WriteLine(result.Succeeded);
      }
      

    }
  }
}
