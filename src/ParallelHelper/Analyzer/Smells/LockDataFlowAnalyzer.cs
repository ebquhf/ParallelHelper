using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Util;
using System;
using System.Collections.Generic;
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
        List<ISymbol> candidates = new List<ISymbol>();
        List<ISymbol> leftOperands = new List<ISymbol>();
        var classNode = _nodeAnalysisContext.Node as ClassDeclarationSyntax;
        var declarations = classNode.DescendantNodesAndSelf().OfType<VariableDeclarationSyntax>();
        var methods = classNode.DescendantNodesAndSelf().OfType<MemberDeclarationSyntax>();
        var locks = classNode.DescendantNodes().OfType<LockStatementSyntax>();
        var assignments = locks.SelectMany(l => l.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>());

        var asd = SemanticModel.GetMethodBodyDiagnostics(methods.FirstOrDefault().Span);
        foreach(var ass in assignments) {
          leftOperands.Add(SemanticModel.GetSymbolInfo(ass.Left).Symbol);
          var basf = SemanticModel.AnalyzeDataFlow(ass);
          candidates.AddRange(basf.WrittenOutside);
        }
        //TODO fix the naming
        var foundIssues = candidates.Where(c => leftOperands.Contains(c));
        if(foundIssues.Any()) {
          foreach(var issue in foundIssues) {
            var diagnostic = Diagnostic.Create(Rule, issue.Locations.First(), "Assignment is used");

            Context.ReportDiagnostic(diagnostic);
          }

        }
        Console.WriteLine(declarations);
      }
    }
  }
}
