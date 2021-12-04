using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;

namespace PwdCrypter.Droid
{
    [Activity(Label = "PwdCrypter", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = false, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        internal static MainActivity Instance { get; private set; } = null;

        const string NotificationChannelID = "YOUR_CHANNEL_ID";
        const string NotificationChannelName = "PwdCrypterNChannel";
        const string NotificationChannelDes = "PwdCrypter Local Notification Channel";

        public string ChannelID { get => NotificationChannelID; }

        protected override void OnCreate(Bundle bundle)
        {
            Instance = this;
            Xamarin.Essentials.Platform.Init(this, bundle);

            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            // Inizializzazione libreria er il supporto degli SVG
            FFImageLoading.Forms.Platform.CachedImageRenderer.Init(true);

            // Crea il canale per le notifiche
            CreateNotificationChannel();

            global::Xamarin.Forms.Forms.Init(this, bundle);
            global::Xamarin.Auth.Presenters.XamarinAndroid.AuthenticationConfiguration.Init(this, bundle);

            global::Xamarin.Auth.CustomTabsConfiguration.CustomTabsClosingMessage = null;
            LoadApplication(new App());
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                // Notification channels are new in API 26 (and not a part of the
                // support library). There is no need to create a notification
                // channel on older versions of Android.
                return;
            }

            var channel = new NotificationChannel(NotificationChannelID, NotificationChannelName, NotificationImportance.Default)
            {
                Description = NotificationChannelDes
            };

            var notificationManager = (NotificationManager)GetSystemService(NotificationService);
            notificationManager.CreateNotificationChannel(channel);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }        
    }
}