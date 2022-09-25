using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class AssignmentInsideLockAnalyzerTest : AnalyzerTestBase<AssignmentInsideLockAnalyzer> {

    [TestMethod]
    public void PrivateFieldLockedInClass() {
      var source = @"public class Class
      {
            private readonly object lockObject = new object();
            
            private int MyNumber;
            public string MyText;

            public void DoWork()
            {
                lock (lockObject)
                {
                    MyNumber+=1;
                }
        }
      }";
      //no location provided as there is no detactable error
      VerifyDiagnostic(source);
    }
    [TestMethod]
    public void PublicFieldLockedInClass() {
      //this is the location of the assignment inside the lock
      int line = 9;
      int column = 17;
      var source = @"public class Class
      {
            private readonly object lockObject = new object();
            
            private int MyNumber;
            public string MyText;

            public void DoWork()
            {
                lock (lockObject)
                {
                    MyText=""text1"";
                }
        }
      }";

      VerifyDiagnostic(source, new DiagnosticResultLocation(line, column));
    }

    [TestMethod]
    public void PublicPropertyLockedInClass() {
      //this is the location of the assignment in the lock
      int line = 11;
      int column = 7;

      var source = @"public class Class {
    
    private readonly object lockObject = new object();

    private int testProperty;
    public int MyProperty {
      get { return testProperty; }
      set { testProperty = value; }
    }

    public void DoWork() {
      lock(lockObject) {
        MyProperty += 1;
      }
    }
  }";
      VerifyDiagnostic(source, new DiagnosticResultLocation(line, column));
    }

    [TestMethod]
    public void PrivatePropertyLockedInClass() {
      var source = @"public class Class {
    
    private readonly object lockObject = new object();

    private int testProperty;
    private int MyProperty {
      get { return testProperty; }
      set { testProperty = value; }
    }

    public void DoWork() {
      lock(lockObject) {
        MyProperty += 1;
      }
    }
  }";
      //no location provided as there is no detactable error
      VerifyDiagnostic(source);
    }
  }
}
