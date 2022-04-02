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
            
            private int MyNumber;

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
    public void ObjectLockedWithPublicAccess() {
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
      VerifyDiagnostic(source,new DiagnosticResultLocation(2,15));
    }
    [TestMethod]
    public void ObjectSetFromOutsideInClass() {
      var source = @"public class Class
      {
            private readonly object lockObject = new object();
            
            private int MyNumber;
            public int MyProperty
                {
                    get { return myVar; }
                    set { myVar = value; }
                }
            public void DoWork()
            {
                lock (lockObject)
                {
                    MyNumber+=1;
                }
        }
      }";
      VerifyDiagnostic(source,new DiagnosticResultLocation(3,15));
    }
  }
}
