using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Util;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace ParallelHelper.Analyzer.Smells {
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class EventLockAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "BT_007";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Raise event in lock";
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
      context.RegisterSyntaxNodeAction(AnalyzeExpressionStatement, SyntaxKind.ClassDeclaration);
    }
    private static void AnalyzeExpressionStatement(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }
    private class Analyzer : MonitorAwareAnalyzerWithSyntaxWalkerBase<ClassDeclarationSyntax> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper(context)) {
        var node = context.Node as ClassDeclarationSyntax;
        var candidateDelegates = new Dictionary<InvocationExpressionSyntax, List<DelegateDeclarationSyntax>>();

        //delegates inside the class
        var delegates = node.DescendantNodes().OfType<DelegateDeclarationSyntax>();

        //gets the invocation syntaxes from inside locks
        var invocations = GetLockedInvocations(node);

        //gets the reference to each delegate from the invocations
        GetCandidatesFromInvocations(invocations, candidateDelegates);

        //if its a delegate from the same class then its sure the invocation method calls an event
        var foundIssues = candidateDelegates.Where(candidates =>
                              delegates.Any(classDelegate => candidates.Value
                                         .Any(candidateDelegate => candidateDelegate == classDelegate)));

        //reports the diagnostic on each raise event call inside the lock
        ReportPossibleDiagnostic(foundIssues);

      }

      private void ReportPossibleDiagnostic(IEnumerable<KeyValuePair<InvocationExpressionSyntax, List<DelegateDeclarationSyntax>>> foundIssues) {
        if(foundIssues.Any()) {

          foreach(var issue in foundIssues) {
            var diagnostic = Diagnostic.Create(Rule, issue.Key.GetLocation(), MessageFormat);
            Context.ReportDiagnostic(diagnostic);
          }

        }
      }

      private void GetCandidatesFromInvocations(IEnumerable<InvocationExpressionSyntax> invocations, Dictionary<InvocationExpressionSyntax, List<DelegateDeclarationSyntax>> candidateDelegates) {
        foreach(var invo in invocations) {
          var methodSymbol = SemanticModel.GetSymbolInfo(invo).Symbol as IMethodSymbol;
          var syntaxReference = methodSymbol?.DeclaringSyntaxReferences.FirstOrDefault();
          candidateDelegates.Add(invo, syntaxReference.SyntaxTree.GetRoot()
            .DescendantNodesAndSelf().OfType<DelegateDeclarationSyntax>().ToList());
        }
      }

      private IEnumerable<InvocationExpressionSyntax> GetLockedInvocations(ClassDeclarationSyntax node) {
        return node.DescendantNodesAndSelf().OfType<LockStatementSyntax>()
          .SelectMany(l => l.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>());
      }
    }
  }
}

