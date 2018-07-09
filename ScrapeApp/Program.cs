using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using HtmlAgilityPack;

namespace ScrapeApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string url = "http://sales.bcpea.org/bg/auto.html?price1=5000&price2=20000&types%5B%5D=1&types%5B%5D=2";

            WebClient w = new WebClient();
            string s = w.DownloadString(url);

            List<LinkItem> allItems = LinkFinder.Find(s);
            List<LinkItem> carLinks=LinkFinder.FindLinksStartWith(allItems, "/bg/auto.html");
            
            // 2.
            foreach (LinkItem i in carLinks)
            {
                Debug.WriteLine(i);
            }


        }
    }
}
