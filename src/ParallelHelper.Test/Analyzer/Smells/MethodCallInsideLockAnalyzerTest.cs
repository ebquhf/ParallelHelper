using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class MethodCallInsideLockAnalyzerTest:AnalyzerTestBase<MethodCallInsideLockAnalyzer> {
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
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void PublicFieldMethodCallChain() {
      var source = @"public class Class
      {
            private readonly object lockObject = new object();
            
            private int MyNumber;
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
      VerifyDiagnostic(source);
    }

  }
}
