using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.Smells {
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class UsedLetterAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_BT001";
    private const string Category = "Naming";

    //Why localizable strings
    private static readonly LocalizableString Title = "Has A letter analyzer";
    private static readonly LocalizableString MessageFormat = "The name of the class contains letter A";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
     DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
     isEnabledByDefault: true, description: Description, helpLinkUri: ""//gets the .md file from parallell helper github HelpLinkFactory.CreateUri(DiagnosticId)
   );
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {

      //Describe what are these doing:
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.EnableConcurrentExecution();

      context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    private void AnalyzeSymbol(SymbolAnalysisContext context) {
      var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;
      // Find just those named type symbols with names containing letter A.
      if(namedTypeSymbol.Name.ToLowerInvariant().ToCharArray().Any(c=>c.Equals('a'))) {
        // For all such symbols, produce a diagnostic.
        var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

        context.ReportDiagnostic(diagnostic);
      }
    }
  }
}
