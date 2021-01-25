using Android.App;
using Android.Content;
using Android.Support.V4.App;
using System.Diagnostics;
using Xamarin.Forms;

[assembly: Dependency(typeof(PwdCrypter.Droid.Notifier))]
namespace PwdCrypter.Droid
{
    public class Notifier : INotifier
    {
        private ContentView ContentViewLightNotification = null;
        private Label MessageViewLightNotification = null;

        public Context Context { get; set; } = null;
        public Intent NotificationIntent { get; set; } = null;

        /// <summary>
        /// Notifica leggere con pannello con animazione fade in/out
        /// </summary>
        /// <param name="message">Messaggio della notifica</param>
        public void SendLightNotification(string message)
        {
            if (ContentViewLightNotification == null || MessageViewLightNotification == null)
            {
                Debug.WriteLine("Unable to show the ligth notification. Xamarin controls are not set");
                return;
            }

            MessageViewLightNotification.Text = message;
            Device.BeginInvokeOnMainThread(async () =>
            {
                ContentViewLightNotification.IsVisible = true;
                await ContentViewLightNotification.FadeTo(1.0, 500);
                await System.Threading.Tasks.Task.Delay(1000);
                await ContentViewLightNotification.FadeTo(0.0, 500);
                ContentViewLightNotification.IsVisible = false;
            });
        }

        /// <summary>
        /// Notifica Android
        /// </summary>
        /// <param name="title">Titolo</param>
        /// <param name="message">Messaggio</param>
        /// <param name="launchArg">Argomento passato all'applicazione al click della notifica</param>
        public void SendNotification(string title, string message, string launchArg = null)
        {            
            const int notificationId = 0;
            Context context = (Context ?? Android.App.Application.Context);

            NotificationCompat.BigTextStyle textStyle = new NotificationCompat.BigTextStyle();
            textStyle.BigText(message);

            NotificationCompat.Builder builder = new NotificationCompat.Builder(context, MainActivity.Instance.ChannelID)
                .SetContentTitle(title)
                .SetContentText(message)
                .SetStyle(textStyle);

            if (!string.IsNullOrEmpty(launchArg))
            {
                var bundle = new Android.OS.Bundle();
                bundle.PutString("launch", launchArg);
                if (NotificationIntent != null)
                {
                    NotificationIntent.PutExtra("launch", launchArg);
                    PendingIntent pendingIntent = PendingIntent.GetActivity(context, 0, NotificationIntent, PendingIntentFlags.OneShot);
                    builder.SetContentIntent(pendingIntent);
                }
                else
                    builder.SetExtras(bundle);
            }

            if (Android.OS.Build.VERSION.SdkInt > Android.OS.BuildVersionCodes.LollipopMr1)
            {
                builder.SetSmallIcon(Resource.Drawable.ic_notification);
                builder.SetVibrate(new long[] { 100, 200, 300 });
            }
            else
                builder.SetSmallIcon(Resource.Drawable.ic_notification_white);

            // Build the notification:
            Notification notification = builder.Build();            

            // Get the notification manager:
            NotificationManager notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;

            // Publish the notification:
            notificationManager.Notify(notificationId, notification);
        }

        /// <summary>
        /// Impostazione dei conntrolli da utilizzare per le notifiche light
        /// </summary>
        /// <param name="contentView">Contenitore della notifica</param>
        /// <param name="messageView">Label in cui visualizzare il messaggio</param>
        public void SetView(ContentView contentView, Label messageView)
        {
            ContentViewLightNotification = contentView;
            MessageViewLightNotification = messageView;
        }
    }
}