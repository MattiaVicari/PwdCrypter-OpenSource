using Newtonsoft.Json.Linq;
using PwdCrypter.Cloud;
using PwdCrypter.Extensions.ResxLocalization.Resx;
using PwdCrypter.Logger;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace PwdCrypter
{
    /// <summary>
    /// Tipologie di azioni da toast notificaiton
    /// </summary>
    public enum AppToastAction
    {
        CheckPassword,
        Backup
    };

    public partial class App : Application
    {
        private const string FakeDeviceId = "YOUR_FAKE_DEVICE_ID";
        private const string SaltDevicePwd = "YOUR_DEVICE_SALT";

        #region BackgroundTask
        public const string CheckPasswordBackgroundTaskName = "CheckPasswordBackgroundTask";
        public const string CheckPasswordBackgroundTaskEntryPoint = "PwdCrypterBackground.CheckPasswordTask";
        public const string BackupBackgroundTaskName = "BackupBackgroundTask";
        public const string BackupBackgroundTaskEntryPoint = "PwdCrypterBackground.BackupTask";
        #endregion

        private static PasswordManager AppPwdManager = null;
        private static StatisticInformer AppStatistic = null;
        private static AppSettings ApplicationSettings = null;
        private static BillingManager AppBillingManager = null;
        private static PwdSecurityManager AppPwdSecurity = null;
        private static CacheManager AppCacheManager = null;
        private static IPushNotificationManager NotificationManager = null;
        private static ILogger AppLogger = null;
        private static ILogger FakeAppLogger = null;

        public static PasswordManager PwdManager { get => GetPasswordManager(); }
        public static StatisticInformer Statistic { get => GetStatistic(); }
        public static AppSettings Settings { get => GetSettings(); }
        public static ICloudConnector CloudConnector { get; set; }
        public static BillingManager Billing { get => GetBillingManager(); }
        public static PwdSecurityManager PwdSecurity { get => GetPwdSecurity(); }
        public static CacheManager Cache { get => GetCacheManager(); }
        public static IPushNotificationManager PushNotificationManager { get => GetPushNotificationManager(); }
        public static ILogger Logger { get => GetLogger(); }


        public static App CurrentApp { get; private set; }
        public static double ViewStepMaxWidth { get => 640.0; }
        public static double ViewStepMaxHeight { get => 450.0; }
        public static double ViewStepSmallMaxHeight { get => 250.0; }
        public static bool AgreePrivacyPolicy { get; set; }
        public static bool IsLoggedIn { get; set; } = false;
        public static bool IsPLUSActive { get; private set; } = false;
        public static bool IsCloudAvailable { get; private set; } = false;
        public static bool IsAttachmentFeatureAvailable { get; private set; } = false;
        public static bool WaitingForCheckPurchasedItem = false;

        public static string Title => "PwdCrypter";
        public static string Version
        {
            get => DependencyService.Get<IAppInformant>().GetVersionNumber();
        }

        public static bool IsExtensionEnabled
        {
            get
            {
                if (Device.RuntimePlatform == Device.UWP)
                    return true;
                return false;
            }
        }

        /// <summary>
        /// Dati provenienti dalla push notification
        /// </summary>
        public static object PushNotificationData { get; set; } = null;

        /// <summary>
        /// Restituisce il nome della piattaforma cloud corrente
        /// </summary>
        /// <returns>Nome della piattaforma cloud corrente</returns>
        public static string GetCurrentCloudPlatformName()
        {
            switch (GetCurrentCloudPlatform())
            {
                case CloudPlatform.OneDrive:
                    return "OneDrive";
                case CloudPlatform.GoogleDrive:
                    return "GoogleDrive";
                default:
                case CloudPlatform.Unknown:
                    return "Unknown";
            }
        }

        /// <summary>
        /// Invia una notifica toast locale
        /// </summary>
        /// <param name="title">Titolo della notifica</param>
        /// <param name="message">Messaggio da visualizzare</param>
        public static void SendToastNotification(string title, string message)
        {
            if (!ApplicationSettings.LocalNotification)
                return;

            INotifier toastNotifier = DependencyService.Get<INotifier>();
            if (toastNotifier != null)
                toastNotifier.SendNotification(title, message);
        }

        public static void SendLigthNotification(Frame frameNotification, Label labelNotification, string message)
        {
            INotifier lightNotifier = DependencyService.Get<INotifier>();
            if (lightNotifier != null)
            {
                lightNotifier.SetView(frameNotification, labelNotification);
                lightNotifier.SendLightNotification(message);
            }
        }

        /// <summary>
        /// Restituisce l'identiticativo della piattaforma Cloud corrente
        /// </summary>
        /// <returns>Identificativo della piattaforma Cloud corrente</returns>
        public static CloudPlatform GetCurrentCloudPlatform()
        {
            if (CloudConnector == null)
                return CloudPlatform.Unknown;

            if (CloudConnector is OneDriveConnector)
                return CloudPlatform.OneDrive;
            if (CloudConnector is GoogleDriveConnector)
                return CloudPlatform.GoogleDrive;

            return CloudPlatform.Unknown;
        }

        public static bool IsNotificationOpen()
        {
            return (PushNotificationData != null);
        }
        public static void ResetPushNotificationData()
        {
            PushNotificationData = null;
        }

        public App()
        {
            InitializeComponent();

            CurrentApp = this;
            AgreePrivacyPolicy = !IsFirstLaunch();

            if (PwdManager.IsInitialized())
                MainPage = new NavigationPage(new LoginPage());            
            else
                MainPage = new NavigationPage(new MainPage());            

            if (Device.RuntimePlatform == Device.UWP)
                NavigationPage.SetHasNavigationBar(MainPage, false);
        }

        public static async Task<NewsInfo> GetNewsData(Uri newsUri)
        {
            System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
            try
            {
                System.Net.Http.HttpResponseMessage response = await httpClient.GetAsync(newsUri);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    JObject newsData = JObject.Parse(json);
                    if (newsData != null && newsData.ContainsKey("news"))
                    {
                        JObject newsDataByLang = newsData.GetValue("news").ToObject<JObject>().
                            GetValue(AppResources.langIDSuper).ToObject<JObject>();

                        return new NewsInfo
                        {
                            Title = newsDataByLang.Value<string>("title"),
                            Content = newsDataByLang.Value<string>("content"),
                            Date = newsData.Value<DateTime>("sent_date"),
                            IsModal = true
                        };
                    }
                }
            }
            finally
            {
                httpClient.Dispose();
            }

            return null;
        }

        /// <summary>
        /// Mostra la pagina con la notizia descritta dal parametro info
        /// </summary>
        /// <param name="info">Oggetto con i dati della news da visualizzare</param>
        public async Task ShowNews(NewsInfo info)
        {
            if (info.IsModal)
            {
                await MainPage.Navigation.PushModalAsync(new NewsPage
                {
                    BindingContext = info
                }, true);
            }
            else
            {
                await MainPage.Navigation.PushAsync(new NewsPage
                {
                    BindingContext = info
                }, true);
            }
        }

        public void DoAction(string action)
        {
            Logger.Debug(string.Format("DoAction [{0}]", action));
            try
            {
                if (string.Compare(action, nameof(AppToastAction.CheckPassword), true) == 0)                
                    MessagingCenter.Send(MainPage, "NavigateTo", typeof(CheckPasswordPage));
                else if (string.Compare(action, nameof(AppToastAction.Backup), true) == 0)
                    MessagingCenter.Send(MainPage, "NavigateTo", typeof(BackupPage));
                else
                    throw new Exception(string.Format("Action {0} not handled", action));
            }
            catch(Exception e)
            {
                Logger.Error(string.Format("DoAction error: {0}", e.Message));
                throw;
            }
        }

        protected override async void OnStart()
        {
            Settings.Init();

            MessagingCenter.Subscribe<NewsInfo>(this, "ShowNews", async (data) =>
            {
                await ShowNews(data);
            });
            MessagingCenter.Subscribe<string>(this, "DoAction", (data) =>
            {
                DoAction(data);
            });

            await PushNotificationManager?.Init(Settings.OneSignal.AppId,
                new Uri(Settings.OneSignal.Url),
                OnPushNotificationOpened);

            // Apre il canale se le notifiche push sono abilitate.
            // N.B.: per le UWP il canale lo apro sempre: le notifiche push si disabilitano dal pannello delle impostazioni
            // delle App
            if (Settings.PushNotification || Device.RuntimePlatform == Device.UWP)
                await PushNotificationManager?.Subscribe();

            CreateCloudConnector();
            await CheckPurchasedItem(true);
        }

        private async Task<bool> OnPushNotificationOpened(NotificationInfo arg)
        {
            Logger.Debug("OnPushNotificationOpened");

            if (Uri.IsWellFormedUriString(arg.RawPayload, UriKind.Absolute))
            {
                PushNotificationData = await GetNewsData(new Uri(arg.RawPayload));
                // Per Android devo aprire la pagina qui perché non viene eseguita una nuova
                // istanza di App e dato che la chiamata a OnPushNotificationOpened avviene in un altro
                // thread diverso da quello principale, potrebbe avvenire la chiamata dopo la chiamata della funzione
                // CheckRedirect.
                if (Device.RuntimePlatform == Device.Android)
                    MessagingCenter.Send(PushNotificationData as NewsInfo, "ShowNews");
            }
            else if (!string.IsNullOrEmpty(arg.RawPayload))
            {
                try
                {
                    JObject jObject = JObject.Parse(arg.RawPayload);
                    if (jObject != null && jObject.ContainsKey("action"))
                    {
                        PushNotificationData = jObject.GetValue("action").ToString();
                        if (Device.RuntimePlatform == Device.Android && IsLoggedIn)
                            MessagingCenter.Send(PushNotificationData.ToString(), "DoAction");
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Errore durante il parsing dei dati provenienti dalla push notification. Errore: " + e.Message);
                }                
            }
            return true;
        }

        /// <summary>
        /// Sincronizzazione con i prodotti acquistati dall'utente
        /// </summary>
        /// <param name="silent">Passare true per non visualizzare all'utente i messaggi di errore. Default false.</param>
        /// <returns></returns>
        public async Task CheckPurchasedItem(bool silent=false, Func<bool> onAfterCheck = null)
        {
            if (WaitingForCheckPurchasedItem)
            {
                onAfterCheck?.Invoke();
                return;
            }
            
            WaitingForCheckPurchasedItem = true;

            // Sincronizzazione con i prodotti acquistati
            // PLUS: cloud e allegati
            try
            {
                bool proFeatures = true;

                await Cache.ReadCache();

                // Verifica se ho un codice di sblocco
                /* Tutte le funzioni PLUS sono ora disponibili gratuitamente
                ProductCache product = Cache.GetProduct(BillingManager.PLUSProductID);
                if (product != null)
                {
                    proFeatures = Billing.IsProductUnlocked(product);
                    if (proFeatures)
                        await Cache.WriteCache();
                }

                if (!proFeatures)
                {
                    // Se l'applicazione è stata acquistata in precedenza, allora sblocco tutte le feature.
                    IAppInformant appInformant = DependencyService.Get<IAppInformant>();
                    if (appInformant != null && await appInformant.AppWasPurchased())
                        proFeatures = true;
                    else
                        proFeatures = await Billing.IsPurchase(BillingManager.PLUSProductID);
                    IsPLUSActive = proFeatures;

                    // Aggiorna la cache
                    await Cache.UpdateProduct(BillingManager.PLUSProductID, proFeatures);
                }*/

                IsCloudAvailable = proFeatures;
                IsAttachmentFeatureAvailable = proFeatures;
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("CheckPurchasedItem error: " + Ex.Message);
                if (!silent)
                    await MainPage.DisplayAlert(Title, Ex.Message, "Ok");

                // Verifica se ho i dati in cache
                /*bool proCacheFeatures = false;
                ProductCache product = Cache.GetProduct(BillingManager.PLUSProductID);
                if (product != null)
                {
                    IsPLUSActive = product.Purchased;
                    proCacheFeatures = IsPLUSActive || Billing.IsProductUnlocked(product);
                    await Cache.WriteCache();
                }*/
                // Tutte le funzioni PLUS sono ora disponibili gratuitamente
                bool proCacheFeatures = true;
                IsCloudAvailable = proCacheFeatures;
                IsAttachmentFeatureAvailable = proCacheFeatures;
            }
            finally
            {
                onAfterCheck?.Invoke();
                WaitingForCheckPurchasedItem = false;
            }
        }

        private void CreateCloudConnector()
        {
            if (CloudConnector != null)
                return;

            switch (Settings.CloudPlatform)
            {
                case CloudPlatform.OneDrive:
                    CloudConnector = new OneDriveConnector();
                    break;
                case CloudPlatform.GoogleDrive:
                    CloudConnector = new GoogleDriveConnector();
                    break;
                case CloudPlatform.Unknown:
                    break;
                default:
                    Debug.WriteLine(string.Format("CreateCloudConnector: platform {0} unknown", Settings.CloudPlatform.ToString()));
                    break;
            }
        }

        protected override void OnSleep()
        {
            // OnSleep
        }

        protected override void OnResume()
        {
            // OnResume
        }

        /// <summary>
        /// Restituisce l'oggetto del PasswordManager
        /// </summary>
        /// <returns>Oggetto del Password Manager</returns>
        private static PasswordManager GetPasswordManager()
        {
            if (AppPwdManager == null)
                AppPwdManager = new PasswordManager();
            return AppPwdManager;
        }

        /// <summary>
        /// Restituisce l'oggetto per gestire le statistiche di utilizzo
        /// dell'applicazione
        /// </summary>
        /// <returns>Oggetto gestore delle statistiche</returns>
        private static StatisticInformer GetStatistic()
        {
            if (AppStatistic == null)
                AppStatistic = new StatisticInformer();
            return AppStatistic;
        }

        /// <summary>
        /// Restituise l'oggetto per la gestione delle impostazioni dell'applicazione.
        /// </summary>
        /// <returns>Oggetto gestore delle impostazioni dell'applicazione</returns>
        private static AppSettings GetSettings()
        {
            if (ApplicationSettings == null)
                ApplicationSettings = new AppSettings();
            return ApplicationSettings;
        }

        /// <summary>
        /// Restituisce l'oggetto per la gestione degli acquisti In-App
        /// </summary>
        /// <returns>Oggetto gestore degli acquisti In-App</returns>
        private static BillingManager GetBillingManager()
        {
            if (AppBillingManager == null)
                AppBillingManager = new BillingManager();
            return AppBillingManager;
        }

        /// <summary>
        /// Restituice l'oggetto responsabile dei meccanismi di sicurezza delle password
        /// </summary>
        /// <returns>Oggetto gestore dei meccanismi di sicurezza delle password</returns>
        private static PwdSecurityManager GetPwdSecurity()
        {
            if (AppPwdSecurity == null)
                AppPwdSecurity = new PwdSecurityManager();
            return AppPwdSecurity;
        }

        /// <summary>
        /// Restituisce l'oggetto responsabile della cache dell'applicazione
        /// </summary>
        /// <returns>Oggetto gestore della cache dell'applicazione</returns>
        private static CacheManager GetCacheManager()
        {
            if (AppCacheManager == null)
                AppCacheManager = new CacheManager();
            return AppCacheManager;
        }

        /// <summary>
        /// Restituisce l'oggetto responsabile della gestione delle notifiche push.
        /// </summary>
        /// <returns>Oggetto gestore delle notifiche push</returns>
        private static IPushNotificationManager GetPushNotificationManager()
        {
            if (NotificationManager == null)
                NotificationManager = DependencyService.Get<IPushNotificationManager>();
            return NotificationManager;
        }

        /// <summary>
        /// Restituisce l'interfaccia per il log
        /// </summary>
        /// <returns>Interfaccia per il log</returns>
        private static ILogger GetLogger()
        {
            if (AppLogger == null)
                AppLogger = DependencyService.Get<ILogger>();
            if (AppLogger == null)
            {
                if (FakeAppLogger == null)
                    FakeAppLogger = new FileLoggerFake();
                return FakeAppLogger;
            }
            return AppLogger;
        }

        /// <summary>
        /// Restituisce true se  è la prima volta che viene eseguita l'applicazione
        /// </summary>
        /// <returns>True se è la prima volta che viene eseguita l'applicazione, false altrimenti.</returns>
        static public bool IsFirstLaunch()
        {
            return !PwdManager.IsInitialized();
        }

        /// <summary>
        /// Restituisce true se l'applicazione è stata aggiornata.
        /// </summary>
        /// <returns>True se l'applicazione è stata aggiornata, false altrimenti.</returns>
        static public bool IsUpdated()
        {
            string CurrentVersionStr = Version;
            string LastVersionStr = Settings.VersionSettings;

            if ((CurrentVersionStr.CompareTo(LastVersionStr) != 0) && (LastVersionStr != ""))
            {
                Settings.VersionSettings = Version;
                Settings.WriteSettings();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Restituisce true se la piattaforma Cloud è attiva.
        /// </summary>
        /// <returns>True se la piattaforma Cloud è attiva, false altrimenti</returns>
        static public bool IsCloudEnabled()
        {
            if (App.CloudConnector == null)
                return false;
            return App.CloudConnector.IsLoggedIn();
        }

        /// <summary>
        /// Bug di Xamaring su Android: OnAppearing viene chiamata due volte
        /// se si apre una pagina modale in OnAppearing.
        /// 30.06.2020: ora succede anche per le UWP
        /// </summary>
        /// <returns>True se è necessario gestire il bug, false altrimenti</returns>
        public static bool IsAppearingTwice()
        {
            if (Device.RuntimePlatform == Device.Android
                || Device.RuntimePlatform == Device.UWP)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Permette di cambiare la MainPage dell'applicazione
        /// </summary>
        /// <param name="page">Pagina che deve essere utilizzata come MainPage</param>
        public void ChangeMainPage(Page page)
        {
            if (page is MasterDetailPage)
                MainPage = page;
            else
                MainPage = new NavigationPage(page);
        }

        /// <summary>
        /// Funzione BeginInvokeOnMainThread asincrona
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func">Funzione</param>
        /// <returns></returns>
        public static Task<T> BeginInvokeOnMainThreadAsync<T>(Func<T> func)
        {
            var taskCompletation = new TaskCompletionSource<T>();
            Device.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var result = func();
                    taskCompletation.SetResult(result);
                }
                catch (Exception Ex)
                {
                    taskCompletation.TrySetException(Ex);
                }
            });

            return taskCompletation.Task;
        }

        /// <summary>
        /// Funzione BeginInvokeOnMainThread asincrona
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func">Funzione</param>
        /// <returns></returns>
        public static Task<T> BeginInvokeOnMainThreadAsyncEx<T>(Func<Task<T>> func)
        {
            var taskCompletation = new TaskCompletionSource<T>();
            Device.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var result = await func();
                    taskCompletation.SetResult(result);
                }
                catch (Exception Ex)
                {
                    taskCompletation.TrySetException(Ex);
                }
            });

            return taskCompletation.Task;
        }

        /// <summary>
        /// Invia i dati al PwdService per la trasmissione all'estensione del browser
        /// </summary>
        /// <param name="data">Dati da inviare al PwdService</param>
        public static void SendDataToPwdService(JObject data)
        {
            MessagingCenter.Send(CurrentApp, "AppServiceData", data);
        }

        /// <summary>
        /// Restituisce una password che dipende dal dispositivo
        /// </summary>
        /// <returns>Password</returns>
        public static byte[] GetDevicePwd()
        {
            string defDeviceId = FakeDeviceId;
            string pwd = SaltDevicePwd;

            try
            {
                IDeviceInfo deviceInfo = DependencyService.Get<IDeviceInfo>();
                pwd += deviceInfo.GetDeviceID();
            }
            catch (Exception ex)
            {
                // API non trovate o non funzionanti
                Debug.WriteLine("GetDevicePwd failed. Error: " + ex.Message);
                pwd += defDeviceId + "napi";
            }

            return EncDecHelper.SHA256Bytes(pwd);
        }

        /// <summary>
        /// Sincronizza le impostazioni per l'autenticazione a due fattori in base alle impostazioni
        /// salvate nel file delle password (correntemente aperto).
        /// </summary>
        public static void Sync2FASetting()
        {
            if (PwdManager.Is2FAAccessConfigured() && Settings.Access != SecurityAccess.TwoFactor)
            {
                Settings.Access = SecurityAccess.TwoFactor;
                Settings.WriteSettings();
            }
            else if (!PwdManager.Is2FAAccessConfigured() && Settings.Access == SecurityAccess.TwoFactor)
            {
                Settings.Access = SecurityAccess.MasterPassword;
                Settings.WriteSettings();
            }
        }
    }


    /// <summary>
    /// Converter per effettuare il binding di una property booleana con
    /// valore inverso.
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }
    }

    /// <summary>
    /// Converter per visualizza un DateTime a video
    /// </summary>
    public class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((DateTime)value).ToString("dd-MM-yyyy, HH:mm:ss");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DateTime.Parse((string)value);
        }
    }


    /// <summary>
    /// Classe che fornisce supporto per lo stile dei controlli
    /// </summary>
    public class AppStyle
    {
        public static Color ButtonBackgroundColor { get { return Color.FromHex("#FF9052"); } }
        public static Color ButtonTextColor { get { return Color.White; } }
        public static Color ButtonDisabledBackgroundColor { get { return Color.LightGray; } }
        public static Color ButtonDisabledTextColor { get { return Color.Black; } }
        public static Color ButtonAlternativeBackgroundColor { get { return Color.LightBlue; } }
        public static Color IconColor { get { return Color.FromHex("#FF9052"); } }
        public static Color ListRowColorEven { get { return Color.FromHex("222196F3"); } }
        public static Color ListRowColorOdd { get { return Color.FromHex("DDDDDDF3"); } }
    }
}
