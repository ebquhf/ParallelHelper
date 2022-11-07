using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Util;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace ParallelHelper.Analyzer.Smells {
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class EventLockAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "BT_007";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Raise event in constructor";
    private static readonly LocalizableString MessageFormat = "Rasing an event inside a lock is discouraged.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeExpressionStatement, SyntaxKind.ConstructorDeclaration);
    }
    private static void AnalyzeExpressionStatement(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }
    private class Analyzer : MonitorAwareAnalyzerWithSyntaxWalkerBase<ConstructorDeclarationSyntax> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper(context)) {
        var node = context.Node as SimpleNameSyntax;

        // we're only interested in delegates
        var type = SemanticModel.GetTypeInfo(node, context.CancellationToken).ConvertedType;

        if(type == null || type.TypeKind != TypeKind.Delegate) {
          return;
        }

        // we're only interested in methods from the current assembly
        var symbol = context.SemanticModel.GetSymbolInfo(node, context.CancellationToken).Symbol;

        if(symbol == null ||
            symbol.Kind != SymbolKind.Method ||
            !symbol.ContainingAssembly.Equals(context.SemanticModel.Compilation.Assembly)) {
          return;
        }
      }
    }
  }
}
