using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;

namespace ScrapeApp
{
    public partial class CarsViewer : Form
    {

        public CarsViewer(string html)
        {
            InitializeComponent();
            webBrowser1.DocumentText = html;
        }

     

    
    }
}
