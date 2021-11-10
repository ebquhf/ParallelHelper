using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace ParallelHelper.Analyzer.Smells {
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class RaiseEventInsideLockAnalyzer : DiagnosticAnalyzer {
    // TODO Make it raise an event in lock
    public const string DiagnosticId = "PH_S900";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Raise event in constructor";
    private static readonly LocalizableString MessageFormat = "Rasing an event inside a lock is discouraged.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    // TODO Timer (Special case): Can start upon instantiation.
    private static readonly StartDescriptor[] StartMethods = {
      new StartDescriptor("System.Threading.Tasks.Task", "Run"),
      new StartDescriptor("System.Threading.Tasks.TaskFactory", "StartNew"),
      new StartDescriptor("System.Threading.Thread", "Start")
    };

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
      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper(context)) { }

      public override void Analyze() {
        foreach(var threadStart in GetThreadStartInvocations()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, threadStart.GetLocation()));
        }
      }

      private IEnumerable<InvocationExpressionSyntax> GetThreadStartInvocations() {
        return Root.DescendantNodesInSameActivationFrame()
          .WithCancellation(CancellationToken)
          .OfType<InvocationExpressionSyntax>()
          .Where(IsThreadStart);
      }

      private bool IsThreadStart(InvocationExpressionSyntax invocation) {
        return SemanticModel.GetSymbolInfo(invocation, CancellationToken).Symbol is IMethodSymbol method
          && IsThreadStart(method);
      }

      private bool IsThreadStart(IMethodSymbol method) {
        return StartMethods.WithCancellation(CancellationToken)
          .Where(descriptor => SemanticModel.IsEqualType(method.ContainingType, descriptor.Type))
          .Any(descriptor => method.Name.Equals(descriptor.Method));
      }
      private IFieldSymbol? TryGetUnsafeCollectionFieldSymbol(ExpressionSyntax? expression) {
        if(expression == null) {
          return null;
        }
        var field = SemanticModel.GetSymbolInfo(expression, CancellationToken).Symbol as IFieldSymbol;
        if(field == null) {
          return null;
        }
        return field;
      }

      public override void VisitAssignmentExpression(AssignmentExpressionSyntax node) {
        var field = TryGetUnsafeCollectionFieldSymbol(node.Right);
        if(field != null && IsInsideLock ) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, node.Right.GetLocation(), field.Name));
        }
      }
    }

    private class StartDescriptor {
      public string Type { get; }
      public string Method { get; }

      public StartDescriptor(string type, string method) {
        Type = type;
        Method = method;
      }
    }
  }
}
