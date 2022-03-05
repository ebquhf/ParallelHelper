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

    //Finds inner class as it is a NamedTypeSymbol in the syntax tree
    [TestMethod]
    public void NestedClassConatainsLetterA() {
      const string source = @"
 public class C {
public class CA{}
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(2, 14));

    }

    // it passes as A is in a property not a NamedType, 
    [TestMethod]
    public void ClassPropConatainsLetterA() {
      const string source = @"
 public class C
    {
        public int Aproperty { get; set; }
    }";
      VerifyDiagnostic(source);

    }
  }
}
