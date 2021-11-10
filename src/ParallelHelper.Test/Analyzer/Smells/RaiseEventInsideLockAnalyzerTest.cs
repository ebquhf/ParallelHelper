using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class RaiseEventInsideLockAnalyzerTest: AnalyzerTestBase<RaiseEventInsideLockAnalyzer> {
    [TestMethod]
    public void RaiseEventInsideLockAndForget() {
      const string source = @"using System;

class Sample
{
    private readonly object syncObject = new object();
    
    public event EventHandler ThresholdReached;

    public void RaiseEventTest1()
    {
        lock(syncObject)
        {
            ThresholdReached?.Invoke(this, null);
        }
    }
    
    
    private void DoRaise()
    {
        this.DoRaiseForReal();
    }
    
    private void DoRaiseForReal()
    {
        this.ThresholdReached(this, null);
    }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(10, 5));
    }

  }
}
