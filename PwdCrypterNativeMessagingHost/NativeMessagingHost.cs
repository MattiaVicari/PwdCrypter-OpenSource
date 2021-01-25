using System;
using System.IO;
using Windows.ApplicationModel.AppService;
using System.Threading;
using System.Linq;
using Windows.Foundation.Collections;
using System.Threading.Tasks;
using Windows.Data.Json;
using System.Diagnostics;

namespace PwdCrypterNativeMessagingHost
{
    class NativeMessagingHost
    {
        const string PACKAGE_FAMILY_NAME = "YOUR_PACKAGE_FAMILY_NAME";
        static private AppServiceConnection appServiceConnection = null;
        static private bool StopProccess = false;

        public static void Main(string[] args)
        {
            Thread appServiceThread = new Thread(new ThreadStart(ThreadProc));
            appServiceThread.Start();
            while (!StopProccess)
                Task.Delay(1000);
        }

        static private async void StartListening()
        {
            JsonObject dataRequest = ReadData();
            if (dataRequest != null)
            {
                Log("Request received...", false);
                JsonObject dataResponse = await ProcessRequest(dataRequest);
                Log("Response...", false);
                WriteData(dataResponse);
            }

            Log("Stopping", false);
            StopProccess = true;
        }

        /// <summary>
        /// Thread che stabilisce la connessione con l'app service
        /// </summary>
        static async void ThreadProc()
        {
            try
            {
                appServiceConnection = new AppServiceConnection
                {
                    AppServiceName = "PwdCrypterService",
                    PackageFamilyName = PACKAGE_FAMILY_NAME
                };

                AppServiceConnectionStatus status = await appServiceConnection.OpenAsync();
                switch (status)
                {
                    case AppServiceConnectionStatus.Success:
                        Log("Connection established - waiting for requests", false);
                        StartListening();
                        break;
                    case AppServiceConnectionStatus.AppNotInstalled:
                        Log("The app AppServicesProvider is not installed.", true);
                        StopProccess = true;
                        return;
                    case AppServiceConnectionStatus.AppUnavailable:
                        Log("The app AppServicesProvider is not available.", true);
                        StopProccess = true;
                        return;
                    case AppServiceConnectionStatus.AppServiceUnavailable:
                        Log(string.Format("The app AppServicesProvider is installed but it does not provide the app service {0}.", appServiceConnection.AppServiceName), true);
                        StopProccess = true;
                        return;
                    case AppServiceConnectionStatus.Unknown:
                        Log(string.Format("An unkown error occurred while we were trying to open an AppServiceConnection."), true);
                        StopProccess = true;
                        return;
                    default:
                        Log(string.Format("An unkown error occurred while we were trying to open an AppServiceConnection. Status: " + status), true);
                        StopProccess = true;
                        return;
                }
            }
            catch(Exception e)
            {
                Log("Connection error: " + e.Message, true);
                StopProccess = true;
            }
        }

        private static async Task<JsonObject> ProcessRequest(JsonObject dataRequest)
        {
            try
            {
                if (dataRequest == null)
                    throw new Exception("No data to process");

                ValueSet message = new ValueSet
                {
                    { "request", dataRequest.ToString() }
                };

                AppServiceResponse response = await appServiceConnection.SendMessageAsync(message);
                if (response != null && response.Status == AppServiceResponseStatus.Success)
                {
                    string responseMessage = response.Message.First().Value.ToString();
                    return JsonObject.Parse(responseMessage);
                }
            }
            catch(Exception e)
            {
                Log("Error occurred: " + e.Message, true);
                return null;
            }

            return null;
        }

        private static void WriteData(JsonObject dataResponse)
        {
            try
            {
                if (dataResponse == null)
                    throw new Exception("No response received from App");

                string response = dataResponse.ToString();
                //Log(response, false);

                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(response);
                Log("Len: " + bytes.Length, false);

                // Mette in testa la lunghezza del messaggio nei primi 4 byte
                Stream stdout = Console.OpenStandardOutput();
                stdout.WriteByte((byte)((bytes.Length >> 0) & 0xFF));
                stdout.WriteByte((byte)((bytes.Length >> 8) & 0xFF));
                stdout.WriteByte((byte)((bytes.Length >> 16) & 0xFF));
                stdout.WriteByte((byte)((bytes.Length >> 24) & 0xFF));
                stdout.Write(bytes, 0, bytes.Length);
                stdout.Flush();

                // Ecco il messaggio
                Console.Write(response);
            }
            catch(Exception e)
            {
                Log(e.Message, true);
            }
        }

        private static JsonObject ReadData()
        {
            Stream stdin = Console.OpenStandardInput();

            byte[] lengthBytes = new byte[4];
            try
            {
                // Legge la lunghezza del messaggio nei primi 4 byte
                stdin.Read(lengthBytes, 0, 4);
                int length = BitConverter.ToInt32(lengthBytes, 0);

                char[] buffer = new char[length];
                StreamReader reader = new StreamReader(stdin);
                while (reader.Peek() >= 0)
                    reader.Read(buffer, 0, buffer.Length);

                string stringBuffer = new string(buffer);
                //Log(stringBuffer, false);
                return JsonObject.Parse(stringBuffer);
            }
            catch(Exception e)
            {
                Log(e.Message, true);
                return null;
            }
        }

        private static void Log(string message, bool error)
        {
            try
            {
                string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string roamingFolder = Path.Combine(appDataFolder, "PwdCrypterNativeMessagingHost");
                string logFilePath = Path.Combine(roamingFolder, "NativeMessagingHost.log");
                Directory.CreateDirectory(roamingFolder);

                // Tipo log rotate: elimia il file se supera i 10MB
                FileInfo fileInfo = new FileInfo(logFilePath);
                if (fileInfo.Length > 10 * 1024 * 1024)
                    File.Delete(logFilePath);

                FileStream logFile = File.OpenWrite(logFilePath);
                logFile.Seek(0, SeekOrigin.End);
                StreamWriter streamWriter = new StreamWriter(logFile);
                streamWriter.WriteLine(DateTime.Now + "\t" + (error ? "ERR\t" : "INF\t") + message);
                streamWriter.Flush();
                logFile.Close();
            }
            catch(Exception Ex)
            {
                Debug.WriteLine("Unable to update the log file. Error: " + Ex.Message);
            }
        }
    }
}
