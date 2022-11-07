using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class EventLockAnalyzerTest : AnalyzerTestBase<EventLockAnalyzer> {
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
    [TestMethod]
    public void PrivateFieldLockedInClass() {
      var source = @"
      public class EventClass
    {
        private readonly object lockObject = new object();
        public delegate void ThisEvent(int number);
        private int MyNumber;
        public string MyText;

        private event ThisEvent MyEvent;

        public virtual void OnThisEvent(int number)
        {
            MyEvent?.Invoke(number);
        }
        public void DoWork()
        {

            lock (lockObject)
            {
                MyEvent += OnThisEvent;
                MyNumber = 1;
                OnThisEvent(MyNumber);
            }
        }


    }";
      VerifyDiagnostic(source);
    }
  }
}
