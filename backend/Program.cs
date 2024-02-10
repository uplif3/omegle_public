using Serilog;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using WebSocketSharp.Server;


namespace Backend
{
    class Program
    {
        //private static List<Client> clients = new List<Client>();   
        private static Lobby lobby;

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .MinimumLevel.Debug() // Set the minimum log level to Information
            .CreateLogger();


            StartServer();
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();
        }

        private static async void StartServer()
        {
            HttpServer listener = new HttpServer(System.Net.IPAddress.Any, 8080, true);
            var cert = "..\\..\\..\\ssl\\server.pfx";
            var passwd = "1234";
            listener.SslConfiguration.ServerCertificate = new X509Certificate2(cert, passwd);
            listener.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            listener.DocumentRootPath = "..\\..\\..\\..\\frontend\\";
            listener.AddWebSocketService<WebSocket> ("/signaling");
            Log.Debug("shit worked so far");
            // Set the HTTP GET request event.
            listener.OnGet += (sender, e) => {
                var req = e.Request;
                var res = e.Response;

                var path = req.RawUrl;

                if (path == "/")
                    path += "index.html";

                byte[] contents;

                if (!e.TryReadFile(path, out contents))
                {
                    res.StatusCode = (int)HttpStatusCode.NotFound;

                    return;
                }

                if (path.EndsWith(".html"))
                {
                    res.ContentType = "text/html";
                    res.ContentEncoding = Encoding.UTF8;
                }
                else if (path.EndsWith(".js"))
                {
                    res.ContentType = "application/javascript";
                    res.ContentEncoding = Encoding.UTF8;
                }
                else if (path.EndsWith(".json"))
                {
                    res.ContentType = "application/json";
                    res.ContentEncoding = Encoding.UTF8;
                }
                else if (path.EndsWith(".css"))
                {
                    res.ContentType = "text/css";
                    res.ContentEncoding = Encoding.UTF8;
                }

                res.ContentLength64 = contents.LongLength;

                res.Close(contents, true);
            };

            

            listener.Start();

            string hostName = Dns.GetHostName();

            IPAddress[] addresses = Dns.GetHostAddresses(hostName);

            // Filtern der Liste, um nur IPv4-Adressen zu erhalten
            IPAddress ipv4Address = addresses.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

            if (ipv4Address != null)
            {
                Console.WriteLine($"Listening for connections https://{ipv4Address}:8080/");
            }
            else
            {
                Console.WriteLine("Listening for connections on http://localhost:8080/");
            }
        }
    }
}
