using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.BestPractices;
using System;
using System.Collections.Generic;
using System.Text;

namespace ParallelHelper.Test.Analyzer.BestPractices {
  [TestClass]
  public class LockObjectAnalyzerTest : AnalyzerTestBase<LockObjectAnalyzer> {
    [TestMethod]
    public void ObjectLockedInClass() {
      var source = @"public class Class
      {
            private readonly object lockObject = new object();
            public int MyNumber;

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
  }
}
