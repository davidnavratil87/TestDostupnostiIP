using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace TestDostupnostiIP
{
    public class Test
    {
        public string Adresa;
        public bool Dostupnost;
        public Test(string adresa, bool dostupnost)
        {
            Adresa = adresa;
            Dostupnost = dostupnost;
        }
        // --------------------------------------

        public static bool JeAdresaDostupna(string adresa)
        {
            int timeout = 300;
            Ping tester = new Ping();
            PingReply odpoved = tester.Send(adresa, timeout);
            bool dostupna = odpoved.Status == IPStatus.Success;
            tester.Dispose();
            return dostupna;
        }
        // --------------------------------------
    }
}
