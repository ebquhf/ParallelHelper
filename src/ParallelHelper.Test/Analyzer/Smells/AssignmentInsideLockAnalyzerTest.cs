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
      VerifyDiagnostic2(source);
    }

    [TestMethod]
    public void PrivateFieldPartiallyLockedInClass() {
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

            public void DoDirtyWork()
            {
                MyNumber+=1; //ERR: non-synchronized access from a public method
            }
      }";
      VerifyDiagnostic2(source);
    }

    [TestMethod]
    public void PublicFieldMethodCallChain() {
      var source = @"public class Class
      {
            private readonly object lockObject = new object();
            
            public int MyNumber;
            public string MyText;

            public void DoWork()
            {
                lock (lockObject)
                {
                    this.DoDirtyWork(); //ERR: control flow - non-synchronized access via called method
                }
            }

            private void DoDirtyWork()
            {
                MyNumber+=1;
            }
      }";
      VerifyDiagnostic2(source);
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
      VerifyDiagnostic2(source);
    }

    [TestMethod]
    public void LeakyReference() {
      var source = @"public class Class
      {
            private readonly object lockObject = new object();
            
            private int[] MyNumbers = new int[1];
            public string MyText;

            public void IncrementUntilMax2()
            {
                int[] hack = null;
                lock (lockObject)
                {
                    hack = MyNumbers;
                }

                hack[0] = 42; //ERR: data flow - we are modifying MyNumbers object outside of the lock!
            }
      }";
      VerifyDiagnostic2(source);
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
                lock (lockObject) //ERR: public field - maybe the error should be on the field declaration line instead?
                {
                    MyText=""text1"" + MyText;
                }
        }
      }";
      VerifyDiagnostic(source, new DiagnosticResultLocation(9, 17));
      VerifyDiagnostic2(source);
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
      VerifyDiagnostic(source, new DiagnosticResultLocation(11, 7));
      VerifyDiagnostic2(source);
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
      VerifyDiagnostic2(source);
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
      VerifyDiagnostic2(source);
    }

    private void VerifyDiagnostic2(string source)
    {
      string[] lines = source.Split("\r\n");
      List<DiagnosticResultLocation> diagnostics = new List<DiagnosticResultLocation>();
      for(int i = 0; i < lines.Length; i++)
      {
        if(lines[i].Contains("//ERR"))
        {
          diagnostics.Add(new DiagnosticResultLocation(i, lines[i].IndexOf(lines[i].Trim()) + 1));
          break;
        }
      }

      VerifyDiagnostic(source, diagnostics.ToArray());
    }
  }
}
