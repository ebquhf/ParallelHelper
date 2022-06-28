using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace ParallelHelper.Analyzer.BestPractices {
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class LockObjectAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_BT002";
    private const string Category = "Locking";

    //Why localizable strings
    private static readonly LocalizableString Title = "inconsistent locking";
    private static readonly LocalizableString MessageFormat = "Analyze inconsistent locking";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
     DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error,
     isEnabledByDefault: true, description: Description, helpLinkUri: ""//gets the .md file from parallell helper github HelpLinkFactory.CreateUri(DiagnosticId)
   );
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.EnableConcurrentExecution();
      context.RegisterSyntaxNodeAction(AnalyzeLockStatement, SyntaxKind.LockStatement);
      
    }

    private void AnalyzeLockStatement(SyntaxNodeAnalysisContext ctx) {
      var lockStatement = ctx.Node as LockStatementSyntax;
      if(lockStatement != null) {
        var location = lockStatement.GetLocation();
        var diagnostic = Diagnostic.Create(Rule, location, "lockThingy");

        ctx.ReportDiagnostic(diagnostic);
      }
    }
  }
}
