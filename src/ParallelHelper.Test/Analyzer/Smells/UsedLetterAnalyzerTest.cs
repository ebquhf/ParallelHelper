using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class UsedLetterAnalyzerTest : AnalyzerTestBase<UsedLetterAnalyzer> {
    [TestMethod]
    public void ClassContainsLetterA() {
      const string source = @"
 public class Class1
    {
    }";
      VerifyDiagnostic(source,new DiagnosticResultLocation(1,15));
    }

    [TestMethod]
    public void ClassNotConatainsLetterA() {
      const string source = @"
 public class ThisIsIt
    {
    }";
      VerifyDiagnostic(source);

    }

  }
}
