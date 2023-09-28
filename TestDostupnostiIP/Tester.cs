using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Threading;
using System.Diagnostics;

namespace TestDostupnostiIP
{
    public class Tester
    {
        private const int perioda = 100;
        private readonly string soubor = "testy.xml";
        private readonly object zamek = new object();
        private int aktivniVlakno;
        private List<Test> provedeneTesty = new List<Test>();

        public void Testuj(string parametr)
        {
            string[] casti = parametr.Split(new char[] { ' ' });
            TimeSpan dobaTrvani = TimeSpan.FromSeconds(Convert.ToInt32(casti[0]));

            aktivniVlakno = 0;

            int pocet = casti.Length - 1;
            for (int i = 1; i <= pocet; i++)
            {
                ++aktivniVlakno;
                string adresa = casti[i];
                Thread vlakno = new Thread(
                    () =>
                    {
                        ProvedTest(adresa, dobaTrvani);
                    });
                vlakno.Start();
            }

            Thread writer = new Thread(
                () =>
                {
                    ZapisDoXml();
                });
            writer.Start();
            writer.Join();

            ProjdiXml();
        }
        // --------------------------------------

        private void ProvedTest(string adresa, TimeSpan dobaTrvani)
        {
            AutoResetEvent resetEvent = new AutoResetEvent(false); 
            Timer timer = new Timer(TimerEvent, adresa, 0, perioda);
            resetEvent.WaitOne(dobaTrvani);
            timer.Dispose();
            lock (zamek)
            {
                --aktivniVlakno;
            }
        }
        // --------------------------------------

        private void TimerEvent(object state)
        {
            string adresa = state.ToString();
            bool dostupna = Test.JeAdresaDostupna(adresa);
            lock (provedeneTesty)
            {
                provedeneTesty.Add(new Test(adresa, dostupna));
            }
        }
        // --------------------------------------

        public void ZapisDoXml()
        {
            int pocetTestu;
            lock (provedeneTesty)
            {
                pocetTestu = provedeneTesty.Count;
            }

            XmlWriterSettings settings = new XmlWriterSettings()
            {
                Indent = true
            };

            using (XmlWriter writer = XmlWriter.Create(soubor, settings))
            {
                writer.WriteStartElement("testy");
                while (aktivniVlakno + pocetTestu > 0)
                {
                    if (pocetTestu > 0)
                    {
                        lock (provedeneTesty)
                        {
                            Test test = provedeneTesty.First();
                            writer.WriteStartElement("test");
                            writer.WriteAttributeString("adresa", test.Adresa);
                            writer.WriteStartElement("dostupnost");
                            writer.WriteValue(test.Dostupnost);
                            writer.WriteEndElement();
                            writer.WriteEndElement();
                            writer.Flush();
                            provedeneTesty.RemoveAt(0);
                        }
                    }
                    lock (provedeneTesty)
                    {
                        pocetTestu = provedeneTesty.Count;
                    }
                }
                writer.WriteEndElement();
            }
        }
        // --------------------------------------

        private void ProjdiXml()
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
