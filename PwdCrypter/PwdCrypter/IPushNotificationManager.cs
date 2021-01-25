using System;
using System.Threading.Tasks;

namespace PwdCrypter
{
    public interface IPushNotificationManager
    {
        Task Init(string AppId, Uri url, Func<NotificationInfo, Task<bool>> pushNotificationOpened);
        Task Subscribe();
        Task Unsubscribe();
        bool IsActive();
        bool CanReceiveNotification();
    }

    /// <summary>
    /// Oggetto che rappresenta le informazioni su una notifica push ricevuta
    /// dall'App.
    /// </summary>
    public class NotificationInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string SmallIcon { get; set; }
        public string LargeIcon { get; set; }
        public string BigPicture { get; set; }
        public string LaunchUrl { get; set; }
        public string RawPayload { get; set; }
        public DateTime Date { get; set; }
    }
}
