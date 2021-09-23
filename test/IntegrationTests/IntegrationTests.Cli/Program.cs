using System;
using System.Threading;

namespace IntegrationTests.Cli {
  class Program {
    static void Main(string[] args) {
      Console.WriteLine("Hello World!");
    }
  }

  class PH007Example {
    public PH007Example() {
      var thread = new Thread(() => { });

      // PH_S007 warning expected below:
      thread.Start();
    }
  }
}
