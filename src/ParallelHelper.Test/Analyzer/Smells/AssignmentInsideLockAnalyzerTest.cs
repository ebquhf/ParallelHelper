using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;
using System.Collections.Generic;

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
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void PrivateFieldLockedInsufficiently() {
      var source = @"public class Class
      {
            private readonly object lockObject = new object();
            
            private int MyNumber;
            public string MyText;

            public void IncrementUntilMax2()
            {
                if (MyNumber < 2) //ERR: this would suggest that MyNumber should never become >2, but it can if multiple thread execute this simultaneously
                {
                  lock (lockObject)
                  {
                      MyNumber+=1;
                  }
                }
            }
      }";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void PublicFieldLockedInClass() {
      var source = @"public class Class
      {
            private readonly object lockObject = new object();
            
            private int MyNumber;
            public string MyText;

            public void DoWork()
            {
                lock (lockObject) //ERR: public field - maybe the error should be on the field declaration line instead?
                {
                    MyText=""text1"" + MyText;
                }
        }
      }";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void PublicPropertyLockedInClass() {

      var source = @"public class Class {
    
    private readonly object lockObject = new object();

    private int testProperty;
    public int MyProperty {
      get { return testProperty; }
      set { testProperty = value; }
    }

    public void DoWork() {
      lock(lockObject) { //ERR: public property
        MyProperty += 1;
      }
    }
  }";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void PublicPropertyPublicBackingField() {
      var source = @"public class Class {
    
    private readonly object lockObject = new object();

    private int testProperty;
    public int MyProperty {
      get { return testProperty; }
      set { testProperty = value; }
    }

    public void DoWork() {
      lock(lockObject) { //ERR: modifies backing field
        testProperty += 1;
      }
    }
  }";
      VerifyDiagnostic(source);
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
