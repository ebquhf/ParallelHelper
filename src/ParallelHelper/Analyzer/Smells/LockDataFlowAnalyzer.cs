using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Util;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.Smells {
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class LockDataFlowAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_BT005";
    private const string Category = "Locking";

    private static readonly LocalizableString Title = "Dataflow inside lock";
    private static readonly LocalizableString MessageFormat = "A variable is used in a safe way regarding concurrency inside the lock";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
     DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error,
     isEnabledByDefault: true, description: Description, helpLinkUri: ""//gets the .md file from parallell helper github HelpLinkFactory.CreateUri(DiagnosticId)
   );
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
    public override void Initialize(AnalysisContext context) {
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.EnableConcurrentExecution();
      context.RegisterSyntaxNodeAction(AnalyzeDataflow, SyntaxKind.ClassDeclaration);
    }

    private void AnalyzeDataflow(SyntaxNodeAnalysisContext obj) {
      new Analyzer(obj).Analyze();
    }
    private class Analyzer : InternalAnalyzerBase<SyntaxNode> {
      private TaskAnalysis _taskAnalysis;
      private SyntaxNodeAnalysisContext _nodeAnalysisContext;

      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper(context)) {
        _taskAnalysis = new TaskAnalysis(context.SemanticModel, context.CancellationToken);
        _nodeAnalysisContext = context;

      }
      public override void Analyze() {
        var classNode = _nodeAnalysisContext.Node as ClassDeclarationSyntax;
        var declarations = classNode.DescendantNodesAndSelf().OfType<VariableDeclarationSyntax>();
        var assignments = classNode.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>();
        var locks = classNode.DescendantNodes().OfType<LockStatementSyntax>();
        var asd = SemanticModel.AnalyzeDataFlow(locks.FirstOrDefault());
        Console.WriteLine(declarations);
      }
    }
  }
}
