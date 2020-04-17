﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.Smells {
  /// <summary>
  /// Analyzer that analyzes sources for accidential leaks of collections inside monitor locks.
  /// 
  /// <example>Illustrates a class with a method synchronizes method accesses but returns the plain reference to the collection.
  /// <code>
  /// class Sample {
  ///   private readonly object syncObject = new object();
  ///   private readonly ISet&lt;string&gt; entries = new HashSet&lt;string&gt;();
  /// 
  ///   public void SetAll(ISet&lt;string&gt; entries) {
  ///     lock(syncObject) {
  ///       this.entries = entries;
  ///     }
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class LeakedInboundCollectionAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S028";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Leaked Inbound Collection";
    private static readonly LocalizableString MessageFormat = "Taking a reference to the collection '{0}' allows unsynchronized accesses to it; create a copy of it instead.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSemanticModelAction(AnalyzeSemanticModel);
    }

    private static void AnalyzeSemanticModel(SemanticModelAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : MonitorAwareSemanticModelAnalyzerWithSyntaxWalkerBase {
      private readonly CollectionAnalysis _collectionAnalysis;
      private ISet<IFieldSymbol>? _unsafeCollectionFields;

      public Analyzer(SemanticModelAnalysisContext context) : base(context) {
        _collectionAnalysis = new CollectionAnalysis(context.SemanticModel, context.CancellationToken);
      }

      public override void Analyze() {
        foreach(var classDeclaration in GetClassDeclarations()) {
          AnalyzeClassDeclaration(classDeclaration);
        }
      }

      private void AnalyzeClassDeclaration(ClassDeclarationSyntax classDeclaration) {
        _unsafeCollectionFields = _collectionAnalysis.GetPotentiallyMutableCollectionFields(classDeclaration).ToImmutableHashSet();
        if(_unsafeCollectionFields.Count == 0) {
          return;
        }
        foreach(var member in classDeclaration.Members) {
          Visit(member);
        }
      }

      public override void VisitMethodDeclaration(MethodDeclarationSyntax node) {
        if(node.Modifiers.Any(SyntaxKind.PublicKeyword)) {
          base.VisitMethodDeclaration(node);
        }
      }

      public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node) {
        if (node.Modifiers.Any(SyntaxKind.PublicKeyword)) {
          base.VisitPropertyDeclaration(node);
        }
      }

      public override void VisitAccessorDeclaration(AccessorDeclarationSyntax node) {
        // Since it is required that the enclosing property declaration is public,
        // only more restrictive modifiers may be used here.
        if(!node.Modifiers.Any()) {
          base.VisitAccessorDeclaration(node);
        }
      }

      public override void VisitAssignmentExpression(AssignmentExpressionSyntax node) {
        var field = TryGetFieldSymbol(node.Left);
        if (field != null && IsInsideLock && _unsafeCollectionFields!.Contains(field) && IsParameterAndNoSafeCollection(node.Right)) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation(), field.Name));
        }
      }

      private bool IsParameterAndNoSafeCollection(ExpressionSyntax? expression) {
        return expression != null
          && SemanticModel.GetSymbolInfo(expression, CancellationToken).Symbol is IParameterSymbol parameter
          && !_collectionAnalysis.IsImmutableCollection(parameter.Type);
      }

      private IEnumerable<ClassDeclarationSyntax> GetClassDeclarations() {
        return Root.DescendantNodesAndSelf()
          .WithCancellation(CancellationToken)
          .OfType<ClassDeclarationSyntax>();
      }

      private IFieldSymbol? TryGetFieldSymbol(ExpressionSyntax? expression) {
        if(expression == null) {
          return null;
        }
        return SemanticModel.GetSymbolInfo(expression, CancellationToken).Symbol as IFieldSymbol;
      }

      public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) {
        // Lambdas start a new activation frame.
      }

      public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) {
        // Lambdas start a new activation frame.
      }

      public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node) {
        // Anonymous methods start a new activation frame.
      }

      public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node) {
        // Local functions start a new activation frame.
      }
    }
  }
}
