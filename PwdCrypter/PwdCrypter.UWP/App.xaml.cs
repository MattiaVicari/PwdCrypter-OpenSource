using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace PwdCrypter.UWP
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        #region BackgroundService
        private static readonly PwdService AppPwdService = new PwdService();

        private BackgroundTaskDeferral AppServiceDeferral = null;
        private AppServiceConnection Connection = null;
        private int CurrentConnectionIndex = 0;

        private static int ConnectionIndex = 0;
        private static readonly Dictionary<int, AppServiceConnection> Connections = new Dictionary<int, AppServiceConnection>();
        private static readonly Dictionary<int, BackgroundTaskDeferral> AppServiceDeferrals = new Dictionary<int, BackgroundTaskDeferral>();
        private static readonly object ThisLock = new object();
        #endregion

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();     
            this.Suspending += OnSuspending;

            Xamarin.Forms.MessagingCenter.Subscribe<PwdCrypter.App, JObject>(this, "AppServiceData", 
                (sender, json) =>
                {
                    AppPwdService.Data = json;
                });
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected async override void OnLaunched(LaunchActivatedEventArgs e)
        {            
            // Inizializzazione libreria er il supporto degli SVG
            //FFImageLoading.Forms.Platform.CachedImageRenderer.Init();

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            /*if (!(Window.Current.Content is Frame rootFrame))
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                Xamarin.Forms.Forms.Init(e);

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }*/

            await OnLaunchOrActivated(e, e.Arguments);
            PwdCrypter.App.Logger.Debug("OnLaunched");

            // Ensure the current window is active
            Window.Current.Activate();
        }        

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        #region BackgroundService
        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            base.OnBackgroundActivated(args);
            IBackgroundTaskInstance taskInstance = args.TaskInstance;

            if (taskInstance.TriggerDetails is AppServiceTriggerDetails)
            {
                AppServiceTriggerDetails appService = taskInstance.TriggerDetails as AppServiceTriggerDetails;

                AppServiceDeferral = taskInstance.GetDeferral();   // Ottiene un deferral così che il servizio non termina
                taskInstance.Canceled += OnAppServicesCanceled;    // Associa un gestore di cancellazione al task in background
                Connection = appService.AppServiceConnection;
                Connection.RequestReceived += OnAppServiceRequestReceived;
                Connection.ServiceClosed += AppServiceConnection_ServiceClosed;

                // Sezione critica
                lock (ThisLock)
                {
                    Connection.AppServiceName = ConnectionIndex.ToString();
                    Connections.Add(ConnectionIndex, Connection);
                    AppServiceDeferrals.Add(ConnectionIndex, AppServiceDeferral);
                    ConnectionIndex++;
                }
            }
        }

        private void AppServiceConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            try
            {
                CurrentConnectionIndex = int.Parse(sender.AppServiceName);
                AppServiceDeferral = AppServiceDeferrals[CurrentConnectionIndex];
                AppServiceDeferrals.Remove(CurrentConnectionIndex);

                if (AppServiceDeferral != null)
                {
                    AppServiceDeferral.Complete();
                    AppServiceDeferral = null;
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("AppServiceConnection_ServiceClosed error: " + Ex.Message);
            }
        }

        private async void OnAppServiceRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            AppServiceDeferral messageDeferral = args.GetDeferral();

            try
            {
                Dictionary<string, object> Request = new Dictionary<string, object>();
                Dictionary<string, object> Response =  new Dictionary<string, object>();
                foreach (KeyValuePair<string, object> keyValuePair in args.Request.Message)
                {
                    Request.Add(keyValuePair.Key, keyValuePair.Value);
                }

                AppPwdService.Consume(Request, out Response);

                foreach (KeyValuePair<string, object> keyValuePair in Response)
                {
                    ValueSet message = new ValueSet
                    {
                        { keyValuePair.Key, keyValuePair.Value }
                    };
                    await args.Request.SendResponseAsync(message);
                }
            }
            finally
            {
                messageDeferral.Complete();
            }
        }

        private void OnAppServicesCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            AppServiceTriggerDetails appService = sender.TriggerDetails as AppServiceTriggerDetails;

            try
            {
                CurrentConnectionIndex = int.Parse(appService.AppServiceConnection.AppServiceName);
                AppServiceDeferral = AppServiceDeferrals[CurrentConnectionIndex];
                AppServiceDeferrals.Remove(CurrentConnectionIndex);

                if (AppServiceDeferral != null)
                {
                    AppServiceDeferral.Complete();
                    AppServiceDeferral = null;
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("OnAppServicesCanceled error: " + Ex.Message);
            }
        }
        #endregion

        protected async override void OnActivated(IActivatedEventArgs args)
        {
            if (args.Kind == ActivationKind.ToastNotification)
            {                
                ToastNotificationActivatedEventArgs toastArgs = args as ToastNotificationActivatedEventArgs;
                string jsonNotificationData = toastArgs.Argument;

                await OnLaunchOrActivated(args, jsonNotificationData);
                PwdCrypter.App.Logger.Debug("OnActivated");

                // Ensure the current window is active
                Window.Current.Activate();
                return;
            }
            base.OnActivated(args);
        }

        private async Task OnLaunchOrActivated(IActivatedEventArgs e, string args)
        {
            // Esempio di arguments in caso di push notification:
            // News:
            // args = "{\"custom\":{\"a\":{\"news\":\"https://www.mydomain.com/api/news/?id=1&year=2019\"}}}";
            // Action:
            // args = "{\"action\":\"checkpassword\"}";

            if (!(Window.Current.Content is Frame rootFrame))
            {
                // Inizializzazione libreria er il supporto degli SVG
                FFImageLoading.Forms.Platform.CachedImageRenderer.Init();

                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                Xamarin.Forms.Forms.Init(e);

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            try
            {
                if (!string.IsNullOrEmpty(args))
                {
                    PwdCrypter.App.Logger.Debug(string.Format("Received arguments [{0}]", args));
                    await EvaluateArguments(rootFrame, args);
                }
                else
                {
                    PwdCrypter.App.Logger.Debug("No arguments");
                }
            }            
            catch (Exception ex)
            {
                Debug.WriteLine("Error occurred during the processing of the arguments for activation.\nError: " + ex.Message);
                PwdCrypter.App.Logger.Error("Error occurred during the processing of the arguments for activation.\nError: " + ex.Message);
            }
            finally
            {
                if (rootFrame.Content == null)
                {
                    PwdCrypter.App.Logger.Debug("Navigate to MainPage");
                    rootFrame.Navigate(typeof(MainPage), args);
                }
                else
                {
                    PwdCrypter.App.Logger.Debug("Root frame not null");
                }
            }
        }

        private async Task EvaluateArguments(Frame rootFrame, string args)
        {
            try
            {
                JObject jsonData = JObject.Parse(args);
                if (jsonData != null)
                {
                    if (jsonData.ContainsKey("custom"))
                    {
                        JObject jsonCustom = jsonData.GetValue("custom").ToObject<JObject>();
                        if (jsonCustom.ContainsKey("a"))
                        {
                            JObject jsonArg = jsonCustom.GetValue("a").ToObject<JObject>();
                            if (jsonArg.ContainsKey("news"))    // News
                            {
                                var newsUri = new Uri(jsonArg.Value<string>("news"));
                                object data = await PwdCrypter.App.GetNewsData(newsUri);
                                if (PwdCrypter.App.CurrentApp != null && rootFrame.Content != null)
                                    await PwdCrypter.App.CurrentApp.ShowNews(data as NewsInfo);
                                else
                                    PwdCrypter.App.PushNotificationData = data;
                            }
                        }
                    }
                    else if (jsonData.ContainsKey("action"))
                    {
                        string actionName = jsonData.GetValue("action").ToObject<string>();
                        if (PwdCrypter.App.CurrentApp != null && rootFrame.Content != null && PwdCrypter.App.IsLoggedIn)
                            PwdCrypter.App.CurrentApp.DoAction(actionName);
                        else
                            PwdCrypter.App.PushNotificationData = actionName;
                    }
                }
            }
            catch (Newtonsoft.Json.JsonReaderException ex)
            {
                Debug.WriteLine(string.Format("The argument {0} is not a valid JSON. Error: {1}", args, ex.Message));
                PwdCrypter.App.Logger.Error(string.Format("The argument {0} is not a valid JSON. Error: {1}", args, ex.Message));
            }
        }
    }
}
