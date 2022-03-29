using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace ParallelHelper.Analyzer.BestPractices {
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class LockObjectAnalyzer : DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new NotImplementedException();

    public override void Initialize(AnalysisContext context) {
      throw new NotImplementedException();
    }
  }
}
