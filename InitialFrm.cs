using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RouterUpgrade
{
    public partial class InitialFrm : Form
    {
        public InitialFrm()
        {
            InitializeComponent();
            webBrowser1.ScriptErrorsSuppressed = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {

            var routerAgent = new AgentBase(ConfigurationManager.AppSettings["url"]);
            //Process.Start(routerAgent.BaseAddress);

            webBrowser1.Document.GetElementsByTagName("input").GetElementsByName("Password")[0].InnerText = "GAxDSHuV";

            Console.WriteLine(webBrowser1.Document.GetElementsByTagName("input").GetElementsByName("Password")[0].InnerText);

            var passwordHex = webBrowser1.Document.InvokeScript("mess_userpass", new object[] { "GAxDSHuV" });

            //var passwordHex = webBrowser1.Document.GetElementsByTagName("input").GetElementsByName("loginPassword")[0].OuterHtml
            //    .Replace("<INPUT id=syspasswd type=hidden value=", string.Empty)
            //    .Replace(" name=syspasswd>", string.Empty);

            Console.WriteLine(passwordHex);

            Task.Run(async () =>
            {
                Console.WriteLine(await routerAgent.GetFormUrlEncodedAsync<string>("/", 61));

                Console.WriteLine(await routerAgent.PostFormUrlEncodedAsync<string>("/cgi-bin/te_acceso_router.cgi",
                        new Dictionary<string, string>
                {
                        { "curWebPage", "/te_wifi.asp" },
                        { "loginUsername", ".-,+" },
                        { "loginPassword", passwordHex.ToString() }
                }));

                try
                {
                    // Subida del fichero ( CUIDADO!!! genera excepción cuando se corta la conexión en el reinicio del router)
                    //var sessionKey = await routerAgent.GetFormUrlEncodedAsync<string>("http://192.168.1.1/cgi-bin/sessionkey.cgi");
                    //var ruta = ConfigurationManager.AppSettings["filePath"];
                    //var fichero = File.ReadAllBytes(ruta);
                    // Subida del fichero ( CUIDADO!!! genera excepción cuando se corta la conexión en el reinicio del router)
                    //var httpdSessionId = string.Empty;
                    if (routerAgent.Cookies.Count > 0)
                    {
                        foreach (var cook in routerAgent.Cookies)
                        {
                            if (cook.Name == "_httpdSessionId_")
                            {
                                //httpdSessionId = cook.Value;
                            }

                        }

                    }
                    var nvc = new NameValueCollection();
                    //nvc.Add("_httpdSessionId_", !string.IsNullOrEmpty(httpdSessionId)? httpdSessionId:string.Empty);
                    //nvc.Add("LoginName", "-%2C%2B");
                    //nvc.Add("TimeOut", "600");
                    //nvc.Add("SessionID", "NotUsed");
                    //nvc.Add("SessionStatus", "Valid");
                    //nvc.Add("LoginTime", "09%3A02");
                    //nvc.Add("LoginDate", "01%2F07");
                    //nvc.Add("LoginRole", "admin");
                    //Console.WriteLine(await routerAgent.PostFormUrlEncodedAsync<string>("http://192.168.1.1/te_uploadinfo.asp", "firmware",
                    //    fichero, 600));
                    await routerAgent.HttpUploadFile("http://192.168.1.1/te_uploadinfo.asp",
                        ConfigurationManager.AppSettings["filePath"], "file", "application/octet-stream", nvc);
                    MessageBox.Show(@"Ok");
                }
                catch (Exception es)
                {
                    Console.WriteLine(es.Message);
                    MessageBox.Show(@"Router reiniciándose");
                }
            });
        }
    }
}