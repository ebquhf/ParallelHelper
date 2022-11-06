using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class LockDataFlowAnalyzerTest : AnalyzerTestBase<LockDataFlowAnalyzer> {
  

    [TestMethod]
    public void LeakyReference() {
      var source = @"
    using System.Collections.Generic;
  
    public class Class
      {
             private readonly object lockObject = new object();

        private List<int> MyNumbers = new List<int>() { 1 };
        public string MyText;

        public void LeakyMethod()
        {
            List<int> hack = null;
            lock (lockObject)
            {
                hack = MyNumbers;
            }

            hack[0] = 42; //ERR: data flow - we are modifying MyNumbers object outside of the lock!
        }
      }";
      VerifyDiagnostic(source, new DiagnosticResultLocation(12, 23));
    }

  }

}
