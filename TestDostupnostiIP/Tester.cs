using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Xml;
using System.Threading;
using System.Diagnostics;
using System.Xml.Linq;

namespace TestDostupnostiIP
{
    public class Tester
    {
        private const int timeout = 300;
        private const int perioda = 100;
        private readonly string soubor = "testy.xml";
        private readonly object zamek = new object();

        public void Testuj(string parametr)
        {
            string[] casti = parametr.Split(new char[] { ' ' });
            TimeSpan dobaTrvani = TimeSpan.FromSeconds(Convert.ToInt32(casti[0]));

            VytvorXmlDok();

            int pocet = casti.Length - 1;
            Thread[] vlakna = new Thread[pocet];
            for (int i = 1; i <= pocet; i++)
            {
                string adresa = casti[i];
                int vlakno = i - 1;
                vlakna[vlakno] = new Thread(
                    () =>
                    {
                        PosliZapis(adresa, dobaTrvani);
                    });
                vlakna[vlakno].Start();
            }

            while (vlakna.Any(a => a.IsAlive))
            {
                Thread.Sleep(2000);
            }
            ProjdiXml(soubor);
        }
        // --------------------------------------

        private void PosliZapis(string adresa, TimeSpan dobaTrvani)
        {
            Stopwatch stopky = new Stopwatch();
            stopky.Start();
            while (stopky.Elapsed <= dobaTrvani)
            {
                bool dostupna = JeAdresaDostupna(adresa);
                lock (zamek)
                {
                    PridejDoXml(adresa, dostupna);
                }
                Thread.Sleep(perioda);
            }
            stopky.Stop();
        }
        // --------------------------------------

        private bool JeAdresaDostupna(string adresa)
        {
            Ping tester = new Ping();
            PingReply odpoved = tester.Send(adresa, timeout);
            bool dostupna = odpoved.Status == IPStatus.Success;
            tester.Dispose();
            return dostupna;
        }
        // --------------------------------------

        private void VytvorXmlDok()
        {
            XDocument xml = new XDocument();
            xml.Add(new XElement("testy"));
            xml.Save(soubor);
        }
        // --------------------------------------

        private void PridejDoXml(string adresa, bool dostupna)
        {
            XDocument xml = XDocument.Load(soubor);
            XElement el = xml.Element("testy");

            el.Add(new XElement("test",
                new XAttribute("adresa", adresa),
                new XElement("dostupnost", dostupna)));

            xml.Save(soubor);
        }
        // --------------------------------------

        private void ProjdiXml(string soubor)
        {
            Dictionary<string, List<bool>> adresa2ping = Adresa2ping(soubor);

            foreach (KeyValuePair<string, List<bool>> kv in adresa2ping)
            {
                double pocetUspesnych = kv.Value.Where(v => v).Count();
                double procenta = Math.Round(pocetUspesnych / kv.Value.Count * 100, 3);
                Console.WriteLine($"adresa {kv.Key} - dostupnost: {procenta}%");
            }
            Console.ReadKey();
        }
        // --------------------------------------

        private Dictionary<string, List<bool>> Adresa2ping(string soubor)
        {
            Dictionary<string, List<bool>> adresa2ping = new Dictionary<string, List<bool>>();
            using (XmlReader xmlRdr = XmlReader.Create(AppDomain.CurrentDomain.BaseDirectory + soubor))
            {
                xmlRdr.MoveToContent();
                while (xmlRdr.Read())
                {
                    if (xmlRdr.NodeType == XmlNodeType.Element && xmlRdr.Name == "test")
                    {
                        string adresa = xmlRdr.GetAttribute("adresa");
                        if (!adresa2ping.ContainsKey(adresa))
                        {
                            adresa2ping.Add(adresa, new List<bool>());
                        }
                        if (xmlRdr.ReadToFollowing("dostupnost"))
                        {
                            bool dostupnost = Convert.ToBoolean(xmlRdr.ReadInnerXml());
                            adresa2ping[adresa].Add(dostupnost);
                        }
                    }
                }
            }
            return adresa2ping;
        }
        // --------------------------------------
    }
}
