using System;
using System.Diagnostics;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace PwdCrypterBackground
{
    public static class Notification
    {
        const string ToastHeroImageTemplate =
            "<toast launch =\"{0}\">" +
            "	<visual>" +
            "		<binding template=\"ToastGeneric\">" +
            "			<image id=\"2\" placement=\"appLogoOverride\" src=\"{1}\"/>" +
            "			<text id=\"1\">{2}</text>" +
            "			<text id=\"2\">{3}</text>" +
            "			<image placement=\"hero\" id=\"1\" src=\"{4}\"/>" +
            "		</binding>" +
            "	</visual>" +
            "</toast>";

        /// <summary>
        /// Notifica Toast di Windows 10
        /// https://docs.microsoft.com/en-us/windows/uwp/design/shell/tiles-and-notifications/adaptive-interactive-toasts
        /// </summary>
        /// <param name="title">Titolo del toast</param>
        /// <param name="message">Messaggio del toast</param>
        /// <param name="launchArgs">Argomento che verrà passato all'App all'apertura della notifica</param>
        /// <param name="heroImage">Immagino in primo piano (hero image 364x180 pixel)</param>
        public static void SendNotification(string title, string message, string launchArgs, string heroImage)
        {
            const string logo = "ms-appx:///Assets/AboutLogo.png";

            try
            {
                XmlDocument toastXml;
                // Disabilitata immagine hero:
                // https://forums.xamarin.com/discussion/182587/uwp-app-freeze-if-it-is-launched-by-the-toast-notification
                if (string.IsNullOrEmpty(heroImage))
                {
                    toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);

                    XmlNodeList toastNodeList = toastXml.GetElementsByTagName("text");
                    toastNodeList.Item(0).AppendChild(toastXml.CreateTextNode(title));
                    toastNodeList.Item(1).AppendChild(toastXml.CreateTextNode(message));

                    XmlNodeList toastImageNodeList = toastXml.GetElementsByTagName("image");
                    toastImageNodeList.Item(0).Attributes.GetNamedItem("id").NodeValue = "1";
                    toastImageNodeList.Item(0).Attributes.GetNamedItem("src").NodeValue = logo;

                    if (!string.IsNullOrEmpty(launchArgs))
                        ((XmlElement)toastXml.SelectSingleNode("/toast")).SetAttribute("launch", launchArgs);
                }
                else
                {
                    toastXml = new XmlDocument();
                    toastXml.LoadXml(string.Format(ToastHeroImageTemplate,
                        launchArgs.Replace("\"", "&quot;"),
                        logo,
                        title, message,
                        heroImage));
                }

                ToastNotification toast = new ToastNotification(toastXml);
                ToastNotificationManager.CreateToastNotifier().Show(toast);
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("Unable to create toast notification. Error: " + Ex.Message);
            }
        }
    }
}
