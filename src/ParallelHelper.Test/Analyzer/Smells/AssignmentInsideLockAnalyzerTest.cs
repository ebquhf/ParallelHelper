using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public void PublicFieldLockedInClass() {
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
      VerifyDiagnostic(source, new DiagnosticResultLocation(9, 17));
    }
  }
}
