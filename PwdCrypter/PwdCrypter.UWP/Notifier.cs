using System;
using System.Diagnostics;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Xamarin.Forms;

[assembly: Dependency(typeof(PwdCrypter.UWP.Notifier))]
namespace PwdCrypter.UWP
{
    public class Notifier : INotifier
    {
        private ContentView ContentViewLightNotification = null;
        private Label MessageViewLightNotification = null;

        /// <summary>
        /// Notifica Toast di Windows 10
        /// </summary>
        /// <param name="title">Titolo del toast</param>
        /// <param name="message">Messaggio del toast</param>
        /// <param name="launchArg">Argomento da passare all'applicazione al click della notifica toast</param>
        public void SendNotification(string title, string message, string launchArg = null)
        {
            const string logo = "ms-appx:///Assets/AboutLogo.png";

            try
            {
                XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);

                XmlNodeList toastNodeList = toastXml.GetElementsByTagName("text");
                toastNodeList.Item(0).AppendChild(toastXml.CreateTextNode(title));
                toastNodeList.Item(1).AppendChild(toastXml.CreateTextNode(message));

                XmlNodeList toastImageNodeList = toastXml.GetElementsByTagName("image");
                toastImageNodeList.Item(0).Attributes.GetNamedItem("id").NodeValue = "1";
                toastImageNodeList.Item(0).Attributes.GetNamedItem("src").NodeValue = logo;

                if (!string.IsNullOrEmpty(launchArg))
                {
                    XmlAttribute launchAttr = toastXml.CreateAttribute("launch");
                    launchAttr.NodeValue = launchArg;
                    toastXml.GetElementsByTagName("toast").Item(0).Attributes.SetNamedItem(launchAttr);
                }

                ToastNotification toast = new ToastNotification(toastXml);
                ToastNotificationManager.CreateToastNotifier().Show(toast);
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("Unable to create toast notification. Error: " + Ex.Message);
            }
        }

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
