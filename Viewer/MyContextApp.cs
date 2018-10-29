using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Timers;
using System.Windows.Forms;
using HtmlAgilityPack;
using ScrapeApp;
using Viewer.Properties;
using Timer = System.Timers.Timer;

namespace Viewer
{
    public class MyContextApp : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private int refreshIntervalHours = 3;
        private string _lastStoredHrefBCPEA;
        private string _lastStoredHrefNap;
        private string _filenameBCPEA = Directory.GetCurrentDirectory() + "/LastCarHref.txt";
        private string _filenameNap = Directory.GetCurrentDirectory() + "/LastCarHrefNap.txt";
        private string _websiteBCPEALink = "http://sales.bcpea.org";
        private string _websiteNapLink = "https://izpalniteli.com";
        private string _urlBcpea;
        private string _urlNap;
        private string _html = "";
        private string sendToEmail;
        SplashScreen _splashScreen = new SplashScreen();

        public MyContextApp()
        {
            _urlBcpea = ConfigurationManager.AppSettings["URL_BCPEA"];
            _urlNap = ConfigurationManager.AppSettings["URL_NAP"];
            sendToEmail = ConfigurationManager.AppSettings["SEND_TO_MAIL"];
            refreshIntervalHours = int.Parse(ConfigurationManager.AppSettings["REFRESH_INTERVAL_HOURS"]);

            Logger.Write($"Initializing REFRESH_INTERVAL_HOURS:{refreshIntervalHours} \r\n URL_BCPEA:{_urlBcpea} \r\n URL_NAP:{_urlNap}  \r\n SEND_TO_MAIL:{sendToEmail}");

            // Initialize Tray Icon
            trayIcon = new NotifyIcon()
            {
                Icon = Icon.FromHandle(Resources.images.GetHicon()),
                ContextMenu = new ContextMenu(new[]
                {
                    new MenuItem("Open list viewer", OpenViewer),
                    new MenuItem("Update now!", OnUpdateNow),
                    new MenuItem("Exit", Exit)
                }),
                Visible = true
            };

            Timer timer = new Timer(1000 * 60 * 60 * refreshIntervalHours);
            timer.Elapsed += OnTimedEvent;
            timer.Start();
            UpdateNow(false);
        }

        private void OnUpdateNow(object sender, EventArgs e)
        {
            UpdateNow(true);
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            UpdateNow(false);
        }

        private void UpdateLastStoredHref(IReadOnlyList<HtmlNode> lastestCars, string filename, ref string lastStoredHref)
        {
            if (lastestCars.Any())
            {
                lastStoredHref = GetHrefForCar(lastestCars[0]);

                File.WriteAllText(filename, lastStoredHref);
            }
        }

        private List<HtmlNode> GetLastestCars(IEnumerable<HtmlNode> carsList, string lastStoredHref)
        {
            return carsList.TakeWhile(x => GetHrefForCar(x) != lastStoredHref).ToList();
        }

        private static string GetHrefForCar(HtmlNode x)
        {
            try
            {
                return x.SelectNodes(".//a").First().GetAttributeValue("href", "");
                //return x.ChildNodes["a"].GetAttributeValue("href", "");
            }
            catch (Exception ex)
            {
                Logger.Write("Error on GetHrefForCar ", ex);
                return "";
            }
        }

        private string ReadLastCarHref(string filename)
        {
            return File.Exists(filename) ? File.ReadAllText(filename) : "";
        }

        private void SendMail(string body)
        {
            Logger.Write("Sending mail to " + sendToEmail);

            var fromAddress = new MailAddress("mariyan87@gmail.com", "ЧСИ - new cars");
            var toAddress = new MailAddress(sendToEmail, "Mariyan Marinov");
            const string fromP = "!L0v3D3s1";
            const string subject = "коли в ЧСИ";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromP)
            };

            using (var message = new MailMessage(fromAddress, toAddress) { Subject = subject, Body = body, IsBodyHtml = true })
            {
                try
                {
                    smtp.Send(message);
                }
                catch (Exception ex)
                {
                    Logger.Write("Error sending mail to " + sendToEmail, ex);
                }
            }
        }

        private void Exit(object sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            trayIcon.Visible = false;

            Application.Exit();
        }

        private void OpenViewer(object sender, EventArgs e)
        {
            string content = _html;
            if (content == "")
            {
                content = "There is no new cars.<hr>" +
                          "Original link: <a href=\"" + _urlBcpea + "\">" + _urlBcpea + "</a><hr>" +
                          "Original link: <a href=\"" + _urlNap + "\">" + _urlNap + "</a><hr>";
            }
            CarsViewer frm = new CarsViewer(content);
            frm.Show();
        }

        private void UpdateNow(bool showWait)
        {
            if (showWait)
            {
                _splashScreen.Show();
            }

            trayIcon.ContextMenu.MenuItems[0].Enabled = false;
            trayIcon.ContextMenu.MenuItems[1].Enabled = false;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (s, ea) =>
            {
                int trials = 0;
                while (trials <= 3)
                {
                    trials++;

                    try
                    {
                        Update();
                        if (_html != "")
                        {
                            SendMail(_html);
                        }

                        break; //success
                    }
                    catch (Exception ex)
                    {
                        Logger.Write("Error on worker - trial : " + trials, ex);
                    }
                }
            };
            bw.RunWorkerCompleted += (s, ea) =>
            {
                trayIcon.ContextMenu.MenuItems[0].Enabled = true;
                trayIcon.ContextMenu.MenuItems[1].Enabled = true;

                if (showWait)
                {
                    _splashScreen.Close();

                    if (ea.Error != null)
                    {
                        Logger.Write("Error on worker completed: ", ea.Error);
                        MessageBox.Show("Failed!" + Environment.NewLine + ea.Error);
                    }
                }
            };
            bw.RunWorkerAsync();
        }

        private void Update()
        {
            _html = "";

            _lastStoredHrefBCPEA = ReadLastCarHref(_filenameBCPEA);
            _lastStoredHrefNap = ReadLastCarHref(_filenameNap);


            try
            {
                var docNap = new HtmlWeb().Load(_urlNap).DocumentNode;
                var nDivs = docNap.SelectNodes("//div[@class]");
                var carsNapDivs = nDivs.Where(at => at.GetAttributeValue("class", "").Trim() == "aoh-item").ToList();
                carsNapDivs.ForEach(div => div.InnerHtml = div.InnerHtml.Replace("href=\"/targ/", "href=\"" + _websiteNapLink + "/targ/")
                    .Replace("src=\"/", "src=\"" + _websiteNapLink + "/"));
                var lastestCarsNap = GetLastestCars(carsNapDivs, _lastStoredHrefNap);
                UpdateLastStoredHref(lastestCarsNap, _filenameNap, ref _lastStoredHrefNap);

                if (lastestCarsNap.Any())
                {
                    string title = "Original link: <a href=\"" + _urlNap + "\">" + _urlNap + "</a><hr>";
                    _html += title + string.Join("<hr>", lastestCarsNap.Select(s => s.InnerHtml)) + "<br/>";
                    Logger.Write("lastestCarsNap: " + lastestCarsNap.Count);
                }

                var doc = new HtmlWeb().Load(_urlBcpea).DocumentNode;
                var nodeBody = doc.SelectSingleNode("//body");
                var n = nodeBody.SelectNodes("//ul[@class]");
                var carsCsiLis = n.First(at => at.Attributes.AttributesWithName("class").Select(a => a.Value == "results_list").First()).SelectNodes("li").ToList();
                carsCsiLis.ForEach(li => li.InnerHtml = li.InnerHtml.Replace("href=\"/bg/auto/", "href=\"" + _websiteBCPEALink + "/bg/auto/"));
                var lastestCars = GetLastestCars(carsCsiLis, _lastStoredHrefBCPEA);
                UpdateLastStoredHref(lastestCars, _filenameBCPEA, ref _lastStoredHrefBCPEA);

                if (lastestCars.Any())
                {
                    string title = "Original link: <a href=\"" + _urlBcpea + "\">" + _urlBcpea + "</a><hr>";
                    _html += title + string.Join("<hr>", lastestCars.Select(s => s.InnerHtml));
                    Logger.Write("lastestCars: " + lastestCars.Count);
                }
            }
            catch (Exception ex)
            {
                Logger.Write("Error in Update(): ", ex);
            }
        }


    }

}
