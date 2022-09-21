using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class RaiseEventInsideLockAnalyzerTest : AnalyzerTestBase<RaiseEventInsideLockAnalyzer> {
    [TestMethod]
    public void RaiseEventNotInLock() {
      const string source = @"public event MyEventHandler MyEvent;

protected virtual OnMyEvent(MyEventArgs args)
{
    if (this.MyEvent != null)
    {
        this.MyEvent(this, args);
    }
}";
      VerifyDiagnostic(source);

    }
  }
}
