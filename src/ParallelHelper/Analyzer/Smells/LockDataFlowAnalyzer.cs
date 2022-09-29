using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

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
      context.RegisterSyntaxNodeAction(AnalyzeLockStatement, SyntaxKind.LockStatement);
    }

    private void AnalyzeLockStatement(SyntaxNodeAnalysisContext context) {
      var model = context.SemanticModel;
      var lockstatement = context.Node as LockStatementSyntax;
      if(model != null && lockstatement != null) {

        DataFlowAnalysis result = model.AnalyzeDataFlow(lockstatement);

        Console.WriteLine(result.Succeeded);
      }


    }

    private void AnalyzeField(SyntaxNodeAnalysisContext context) {
      var declarationSyntax = context.Node as FieldDeclarationSyntax;
      if(declarationSyntax != null) {
        foreach(var variableDeclaration in declarationSyntax.Declaration.Variables) {
          //  if(context.SemanticModel.GetDeclaredSymbol(variableDeclaration) is IFieldSymbol variableDeclarationSymbol
          //    && IsFieldPublic(variableDeclarationSymbol)) {
          //    var fieldSymbol = variableDeclarationSymbol as IFieldSymbol;
          //    // publicMembers.Add(fieldSymbol.Name);
          //  }
          //}
        }
      }
    }
  }
}
