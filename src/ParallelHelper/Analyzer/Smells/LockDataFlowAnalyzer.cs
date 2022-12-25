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

    private static readonly LocalizableString Title = "Unsafe collection";
    private static readonly LocalizableString MessageFormat = "Dangerous assignment of a collection";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
     DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
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
        if(classNode != null) {
          //Gets all the lock syntaxes in the class
          var locks = classNode.DescendantNodes().OfType<LockStatementSyntax>().ToList();

          //selects every assignment expression inside the every lock
          var assignments = locks.SelectMany(l => l.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>()).ToList();

          //gets if an assigned value is also written outside the lock
          GetCandidatesForConcurrencyError(assignments, candidates, leftOperands);


          //checks if the written outside reference is the same as the assigned in lock
          var foundIssues = candidates.Where(c => leftOperands.Contains(c)).ToList();
          if(foundIssues.Any()) {
            foreach(var issue in foundIssues) {
              foreach(var location in issue.Locations) {
                var diagnostic = Diagnostic.Create(Rule, location, MessageFormat);

                Context.ReportDiagnostic(diagnostic);
              }

            }

          }
        }
      }

      private void GetCandidatesForConcurrencyError(IEnumerable<AssignmentExpressionSyntax> assignments, List<ISymbol> candidates, List<ISymbol> leftOperands) {
        foreach(var ass in assignments) {
          leftOperands.Add(SemanticModel.GetSymbolInfo(ass.Left).Symbol);
          var dataFlow = SemanticModel.AnalyzeDataFlow(ass);
          candidates.AddRange(dataFlow.WrittenOutside);
        }
      }
    }
  }
}
