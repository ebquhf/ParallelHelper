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
    public void PrivateFieldLockedInClass() {
      var source = @"
      public class Class
      {
            private readonly object lockObject = new object();
            
            private int MyNumber;
            public string MyText;
[ThreadStatic] volatile ThisEventHandler _localEvent = null; //need volatile to ensure we get the latest
public virtual void OnThisEvent (EventArgs args)
{
   _localEvent=ThisEvent; //assign the event to a local variable – notice is volatile class field
   if (_localEvent!=null)
   {
     _localEvent(this,args);
   }
}
            public void DoWork()
            {
               
                lock (lockObject)
                {
                
                    MyNumber=1;
                    OnThisEvent(MyNumber);
                }
        }
      }";
      VerifyDiagnostic(source);
    }
  }
}
