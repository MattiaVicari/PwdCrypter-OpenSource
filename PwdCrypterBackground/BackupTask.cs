using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace PwdCrypterBackground
{
    public sealed class BackupTask : IBackgroundTask
    {
        const string TaskName = "BackupBackgroundTask";

        private BackgroundTaskDeferral _deferral;
        private readonly Logger _workLogger = new Logger(TaskName);
        private DateTime _executionMinutes;

        private DateTime GetNowMinutes()
        {
            return DateTime.ParseExact(DateTime.Now.ToString("dd-MM-yyyy HH:mm:00"), "dd-MM-yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void DoWork()
        {
            Resources resources = new Resources();
            Notification.SendNotification("PwdCrypter", 
                resources.GetString("msgBackup"), 
                "{\"action\": \"backup\"}",
                "ms-appx:///Assets/backup_hero.png");
        }

        private async Task RunWork()
        {            
            Debug.WriteLine("Esecuzione attività...", TaskName);
            await _workLogger.Debug(string.Format("Esecuzione attività, {0}...", _executionMinutes.ToString("dd-MM-yyyy HH:mm:ss")));

            try
            {
                // Oggetto con le impostazioni del work
                BackgroundTaskSetting setting = new BackgroundTaskSetting(TaskName, _executionMinutes)
                {
                    WorkLogger = _workLogger
                };
                await setting.Init();

                if (setting.Execute)
                {
                    DoWork();

                    ApplicationData.Current.LocalSettings.Values[TaskName + ".Status"] = "Ok";
                    ApplicationData.Current.LocalSettings.Values[TaskName + ".Interval"] = setting.Interval;
                    ApplicationData.Current.LocalSettings.Values[TaskName + ".NextDate"] = setting.NextDate.ToString("dd-MM-yyyy, HH:mm:ss");
                    await _workLogger.Debug("NextDate: " + ApplicationData.Current.LocalSettings.Values[TaskName + ".NextDate"]);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Errore durante l'esecuzione del task: {0}", new[] { e.Message });
                ApplicationData.Current.LocalSettings.Values[TaskName + ".Status"] = "Error ocurred: " + e.Message;
                await _workLogger.Error(e.Message);
            }

            Debug.WriteLine("Operazione completata", TaskName);
            await _workLogger.Debug("Operazione completata");
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _executionMinutes = GetNowMinutes();
            _deferral = taskInstance.GetDeferral();
            await RunWork();
            _deferral.Complete();
        }
    }
}
