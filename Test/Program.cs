using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YTY.DrsLib
{
  class Program
  {
    static void Main(string[] args)
    {
      var drs=DrsFile.Load(@"D:\HawkEmpire\Manager\drs\graphics.drs");
      drs.Save(@"d:\graphics.drs");
      Console.WriteLine("Done");
      Console.ReadKey();
    }
  }
}
