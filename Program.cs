using System;
using System.Text;
using System.Threading.Tasks;
using Fiddler;

namespace FiddlerCore_ModifyResponse
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // If you want to use CertEnroll instead of BouncyCastle, remove the BCMakeCert and CertMaker references
            // and then enable the PreferCertEnroll
            // Fiddler.FiddlerApplication.Prefs.SetBoolPref("fiddler.certmaker.PreferCertEnroll", true);
            //
            FiddlerApplication.Prefs.SetBoolPref("fiddler.certmaker.bc.Debug", true);

            //// Add some logging
            //Fiddler.FiddlerApplication.OnNotification += delegate (object sender, NotificationEventArgs oNEA) { Console.WriteLine("** NotifyUser: " + oNEA.NotifyString); };
            //Fiddler.FiddlerApplication.Log.OnLogString += delegate (object sender, LogEventArgs oLEA) { Console.WriteLine("** LogString: " + oLEA.LogString); };

            // Custom cert provider can be used.
            BCCertMaker.BCCertMaker certProvider = new BCCertMaker.BCCertMaker();
            CertMaker.oCertProvider = certProvider;
            FiddlerApplication.ResponseHeadersAvailable += FiddlerApplication_ResponseHeadersAvailable;

            Fiddler.FiddlerApplication.BeforeResponse += FiddlerApplication_BeforeResponse;
            Fiddler.FiddlerApplication.AfterSessionComplete += FiddlerApplication_AfterSessionComplete;

            if (!CertMaker.createRootCert())
            {
                Console.WriteLine("Unable to create cert for FiddlerCore.");
                return;
            }

            if (!CertMaker.trustRootCert())
            {
                Console.WriteLine("Unable to install FiddlerCore's cert.");
                return;
            }

            FiddlerCoreStartupSettings startupSettings =
                                            new FiddlerCoreStartupSettingsBuilder()
                                                .ListenOnPort(8887)
                                                .DecryptSSL()
                                                .RegisterAsSystemProxy()
                                                .Build();


            FiddlerApplication.Startup(startupSettings);
            Console.WriteLine("\nPROXY IS NOW SET, open https://yahoo.com in your browser");
            Console.WriteLine("To remove the proxy and close the app, press Enter");

            Console.ReadLine();
            FiddlerApplication.Shutdown();
        }


        private static void FiddlerApplication_BeforeResponse(Session oSession)
        {

            if (oSession.fullUrl.Contains("httpbin.org/status/404") && oSession.responseCode == 404)
            {
                oSession.responseCode = 200;
                oSession.oResponse.headers.HTTPResponseCode = 200;
                oSession.oResponse.headers.HTTPResponseStatus = "200 OK";
            }

 
            if (oSession.fullUrl.Contains("yahoo.com") && oSession.HTTPMethodIs("GET"))
            {
                oSession.bBufferResponse = true;
                oSession.utilDecodeResponse();

                // Remove any compression or chunking
                oSession.utilDecodeResponse();
                var oBody = System.Text.Encoding.UTF8.GetString(oSession.responseBodyBytes);
                // Modify the body as you want
                oBody = "Replaced body";

                // Set the response body to the div-less string
                oSession.utilSetResponseBody(oBody);
            }
  
        }

        private static void FiddlerApplication_AfterSessionComplete(Session oSession) 
        {
            if (oSession.fullUrl.Contains("httpbin.org/status/404"))
            {
                Console.WriteLine(">>>>>>>>>>>> oSession.oResponse.headers.HTTPResponseCode is: " + oSession.oResponse.headers.HTTPResponseCode);
            }
            
        }

        private static void FiddlerApplication_ResponseHeadersAvailable(Session oSession)
        {

            if (oSession.fullUrl.Contains("yahoo.com"))
            {
                // Set this to true, so in BeforeResponse you'll be able to modify the the body.
                // If the value is false (default one), the response that you'll work with in the BeforeResponse handler
                // will be just a copy. The original one will already be streamed to the client and all of your modifications
                // will not be visible there.
                oSession.bBufferResponse = true;
            }
        }
    }
}
