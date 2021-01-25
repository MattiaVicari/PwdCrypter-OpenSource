using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;
using Com.OneSignal;
using Com.OneSignal.Abstractions;
using Android.Util;

[assembly: Dependency(typeof(PwdCrypter.Droid.PushNotificationManager))]
namespace PwdCrypter.Droid
{
    class PushNotificationManager : IPushNotificationManager
    {
        private string PushNotificationToken = "";
        private string PushNotificationDeviceID = "";

        public bool CanReceiveNotification()
        {
            return true;
        }

        public async Task Init(string AppId, Uri url, Func<NotificationInfo, Task<bool>> pushNotificationOpened)
        {
            await Task.Run(() =>
            {
                OneSignal.Current.SetLogLevel(LOG_LEVEL.VERBOSE, LOG_LEVEL.NONE);
                OneSignal.Current.StartInit(AppId).Settings(new Dictionary<string, bool>
                {
                    { IOSSettings.kOSSettingsKeyAutoPrompt, false },
                    { IOSSettings.kOSSettingsKeyInAppLaunchURL, true }
                })
                .InFocusDisplaying(OSInFocusDisplayOption.Notification)
                .UnsubscribeWhenNotificationsAreDisabled(true)
                .HandleNotificationOpened(async (result) =>
                {
                    NotificationInfo info = new NotificationInfo
                    {
                        Id = result.notification.payload.notificationID,
                        Title = result.notification.payload.title,
                        Body = result.notification.payload.body,
                        BigPicture = result.notification.payload.bigPicture,
                        LargeIcon = result.notification.payload.largeIcon,
                        SmallIcon = result.notification.payload.smallIcon,
                        LaunchUrl = result.notification.payload.launchURL,
                        RawPayload = result.notification.payload.body,
                        Date = DateTime.Now
                    };

                    if (result.notification.payload.additionalData.Count > 0)
                    {
                        var context = Android.App.Application.Context;
                        var additionalData = result.notification.payload.additionalData;
                        if (additionalData.ContainsKey("news") && additionalData["news"] != null)
                            info.RawPayload = additionalData["news"].ToString();
                    }

                    await pushNotificationOpened?.Invoke(info);
                })
                .HandleNotificationReceived((result) =>
                {
                    Log.Debug("PwdCrypter", "Push notification received");
                })
                .EndInit();

                OneSignal.Current.IdsAvailable((playerID, pushToken) =>
                {
                    PushNotificationToken = pushToken;
                    PushNotificationDeviceID = playerID;

                    Log.Debug("PwdCrypter", "OneSignal DeviceID: " + playerID);
                    Log.Debug("PwdCrypter", "OneSignal Token: " + pushToken);
                });

                OneSignal.Current.RegisterForPushNotifications();
            });
        }

        public bool IsActive()
        {
            return !string.IsNullOrEmpty(PushNotificationToken) && !string.IsNullOrEmpty(PushNotificationDeviceID);
        }

        public async Task Subscribe()
        {
            await Task.Run(() =>
                OneSignal.Current.SetSubscription(true)
            );
        }

        public async Task Unsubscribe()
        {
            await Task.Run(() =>
                OneSignal.Current.SetSubscription(false)
            );
        }
    }
}