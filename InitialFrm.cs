using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RouterUpgrade
{
    public partial class InitialFrm : Form
    {
        public InitialFrm()
        {
            InitializeComponent();
            //webBrowser1.Url = new Uri(ConfigurationManager.AppSettings["url"], UriKind.Absolute);
            //webBrowser1.DocumentCompleted += webBrowser1_DocumentCompleted;
        }

        private void InitialFrm_Load(object sender, EventArgs e)
        {
            LLamadas();
        }

        private void LLamadas()
        {
            var routerAgent = new AgentBase(ConfigurationManager.AppSettings["url"]);
            //, new Cookie("_httpdSessionId_", "aa00a2b7977888c1b3c7863d36443950", "/", "192.168.1.1"));

            Task.Run(async () =>
            {
                try
                {
                    await IniciarSesionConLaCookieYUsuarioYPassCifrados(routerAgent);

                    await ObtenerCookieDeSesion(routerAgent);

                    await SubirFicheroFirmware(routerAgent);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($@"Router reiniciándose {ex.Message}");
                }
            });
        }

        private static async Task ObtenerCookieDeSesion(AgentBase routerAgent)
        {
            await routerAgent.GetAsync<string>("/te_wifi.asp");
        }

        private static async Task IniciarSesionConLaCookieYUsuarioYPassCifrados(AgentBase routerAgent)
        {
            await routerAgent
                .PostFormUrlEncodedAsync<string>("/cgi-bin/te_acceso_router.cgi",
                    new Dictionary<string, string>
                    {
                        {"curWebPage", "/te_wifi.asp"},
                        {
                            "loginUsername",
                            MetodoMessUserPass.MessUserPass(ConfigurationManager.AppSettings["username"])
                        },
                        {
                            "loginPassword",
                            MetodoMessUserPass.MessUserPass(ConfigurationManager.AppSettings["password"])
                        }
                    });
        }

        private static async Task SubirFicheroFirmware(AgentBase routerAgent)
        {
            // Subida del fichero ( CUIDADO!!! genera excepción cuando se corta la conexión en el reinicio del router)
            var fichero = ConfigurationManager.AppSettings["filePath"];
            var nombre = Path.GetFileName(fichero);
            var bytes = File.ReadAllBytes(fichero);
            await routerAgent
                .PostFormUrlEncodedAsync<string>("/te_uploadinfo.asp", nombre, bytes);
        }


    }
}