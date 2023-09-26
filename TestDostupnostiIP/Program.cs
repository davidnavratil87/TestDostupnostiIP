using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDostupnostiIP
{
    class Program
    {
        static void Main(string[] args)
        {
            string parametr = "60 IP1 IP2 ...";
            Tester t = new Tester();
            t.Testuj(parametr);
        }
    }
}
