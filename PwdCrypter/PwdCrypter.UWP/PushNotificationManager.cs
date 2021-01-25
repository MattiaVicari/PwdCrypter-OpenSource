using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Networking.PushNotifications;
using Windows.Storage;
using Windows.Web.Http;
using Xamarin.Essentials;
using Xamarin.Forms;

[assembly: Dependency(typeof(PwdCrypter.UWP.PushNotificationManager))]
namespace PwdCrypter.UWP
{
    class PushNotificationManager : IPushNotificationManager
    {
        private const string FakeDeviceId = "YOUR_FAKE_DEVICE_ID";
        private const string SaltDevicePwd = "YOUR_DEVICE_SALT";

        private const string OneSignalWindowsDeviceType = "6";   // WNS

        private string PushNotificationToken = "";
        private string PushNotificationDeviceID = "";

        private Uri OneSignalUrl = null;
        private string OneSignalAppId = "";

        /// <summary>
        /// Restituisce una password che dipende dal dispositivo
        /// </summary>
        /// <returns>Password</returns>
        private byte[] GetDevicePwd()
        {
            string defDeviceId = FakeDeviceId;
            string pwd = SaltDevicePwd;

            try
            {
                pwd += new DeviceInfo().GetDeviceID();
            }
            catch (Exception ex)
            {
                // API non trovate o non funzionanti
                Debug.WriteLine("GetDevicePwd failed. Error: " + ex.Message);
                pwd += defDeviceId + "napi";
            }

            return EncDecHelper.SHA256Bytes(pwd);
        }

        public async Task Init(string AppId, Uri url, Func<NotificationInfo, Task<bool>> pushNotificationOpened)
        {
            OneSignalAppId = AppId;
            OneSignalUrl = url;

            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile pushNotificationFile = await localFolder.GetFileAsync("push.notification");
                if (pushNotificationFile != null)
                {
                    string content = await FileIO.ReadTextAsync(pushNotificationFile, Windows.Storage.Streams.UnicodeEncoding.Utf8);
                    if (content.Length > 0)
                    {
                        EncDecHelper encDecHelper = new EncDecHelper
                        {
                            Password = GetDevicePwd()
                        };
                        string decryptedContent = encDecHelper.AESDecrypt(content, out bool warning);
                        if (decryptedContent.Length > 0)
                        {
                            JObject json = JObject.Parse(decryptedContent);
                            PushNotificationToken = json.Value<string>("token");
                            PushNotificationDeviceID = json.Value<string>("playerid");
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                // Il file non esiste
                PushNotificationToken = "";
                PushNotificationDeviceID = "";
            }
            catch (Exception ex)
            {
                PushNotificationToken = "";
                PushNotificationDeviceID = "";
                Debug.WriteLine("Errore durante la lettura del file di configurazione delle notifiche push. Exception: " + ex.Message);
                throw;
            }
        }

        public bool IsActive()
        {
            return !string.IsNullOrEmpty(PushNotificationToken) && !string.IsNullOrEmpty(PushNotificationDeviceID);
        }

        public bool CanReceiveNotification()
        {
            var notifier = Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier();
            return (notifier.Setting == Windows.UI.Notifications.NotificationSetting.Enabled);
        }

        public async Task Subscribe()
        {
            bool ret = IsActive();
            Windows.ApplicationModel.Resources.ResourceLoader ResLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

            try
            {
                // Ottiene il channel URI da WNS
                PushNotificationChannel pushNotificationChannel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                if (pushNotificationChannel != null)
                {
                    pushNotificationChannel.PushNotificationReceived += PushNotificationReceived;

                    // Usa il channel uri come token
                    string token = pushNotificationChannel.Uri;
                    if (token.Length > 0 && (!ret || (ret && PushNotificationToken.CompareTo(token) != 0)))
                    {
                        // Invia il token al server di OneSignal
                        string url = OneSignalUrl.ToString();
                        string appId = OneSignalAppId;
                        string lang = ResLoader.GetString("langID");
                        HttpClient httpClient = new HttpClient();
                        try
                        {
                            List<KeyValuePair<string, string>> param = new List<KeyValuePair<string, string>>
                            {
                                new KeyValuePair<string, string>("app_id", appId),
                                new KeyValuePair<string, string>("device_type", OneSignalWindowsDeviceType),
                                new KeyValuePair<string, string>("identifier", token),
                                new KeyValuePair<string, string>("language", lang)
                            };
                            HttpFormUrlEncodedContent content = new HttpFormUrlEncodedContent(param);

                            HttpResponseMessage response = await httpClient.PostAsync(new Uri(url), content);
                            if (!response.IsSuccessStatusCode)
                                throw new Exception("OneSignal: device registration failed!");
                            
                            JObject jsonObj = JObject.Parse(response.Content.ToString());
                            if (jsonObj == null || !jsonObj.ContainsKey("id"))
                                throw new Exception("PlayerID not found");
                               
                            string playerID = jsonObj.Value<string>("id");
                            // Registrare il playerID e il token
                            await SaveNotificationCredentials(playerID, token);
                            // Salva nelle variabili locali le credenziali
                            PushNotificationDeviceID = playerID;
                            PushNotificationToken = token;   
                        }
                        catch (Exception ex)
                        {
                            PushNotificationDeviceID = "";
                            PushNotificationToken = "";
                            Debug.WriteLine("Errore di registrazione del device sul server di OneSignal. Exception: " + ex.Message);
                        }
                        finally
                        {
                            httpClient.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Errore di apertura del canale per le notifiche push. Exception: " + ex.Message);
            }

            Debug.WriteLine("OneSignal DeviceID: " + PushNotificationDeviceID);
            Debug.WriteLine("OneSignal Token: " + PushNotificationToken);
        }

        /// <summary>
        /// Salva su file le credenziali di accesso al canale
        /// </summary>
        /// <param name="playerID">Id del dispositivo</param>
        /// <param name="token">Token di accesso o id del canale</param>
        /// <returns></returns>
        private async Task SaveNotificationCredentials(string playerID, string token)
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile pushNotificationFile = await localFolder.CreateFileAsync("push.notification", CreationCollisionOption.ReplaceExisting);
                if (pushNotificationFile != null)
                {
                    JObject jObject = new JObject
                    {
                        { "token", new JValue(token) },
                        { "playerid", new JValue(playerID) }
                    };

                    EncDecHelper encDecHelper = new EncDecHelper
                    {
                        Password = GetDevicePwd()
                    };
                    string encryptedContent = encDecHelper.AESEncrypt(jObject.ToString());
                    await FileIO.WriteTextAsync(pushNotificationFile, encryptedContent, Windows.Storage.Streams.UnicodeEncoding.Utf8);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Errore di scrittura del file di configurazione delle notifiche push. Exception: " + ex.Message);
            }
        }

        private void PushNotificationReceived(PushNotificationChannel sender, PushNotificationReceivedEventArgs args)
        {
            Debug.WriteLine("Push notification received");
        }

        public async Task Unsubscribe()
        {
            if (!IsActive())
                return;
            await Launcher.OpenAsync(new Uri("ms-settings:notifications"));
        }
    }
}
