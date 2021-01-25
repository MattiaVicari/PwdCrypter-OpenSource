//#define TEST_BACKGROUND_TASK

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Storage;

namespace PwdCrypterBackground
{
    public sealed class CheckPasswordTask : IBackgroundTask
    {
        const string TaskName = "CheckPasswordBackgroundTask";

        private BackgroundTaskDeferral _deferral;
        private readonly Logger _workLogger = new Logger(TaskName);
        private DateTime _executionMinutes;

        private void DoWork()
        {
            Resources resources = new Resources();
            Notification.SendNotification("PwdCrypter",
                resources.GetString("msgCheckPassword"),
                "{\"action\": \"checkpassword\"}",
                "ms-appx:///Assets/checkpassword_hero.png");
        }

        private DateTime GetNowMinutes()
        {
            return DateTime.ParseExact(DateTime.Now.ToString("dd-MM-yyyy HH:mm:00"), "dd-MM-yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        }

#if DEBUG
        private IAsyncAction PrintLocalSetting()
        {
            return Task.Run(async () =>
            {
                ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
                string[] keys = new string[]
                {
                    ".Interval",
                    ".TimeKind",
                    ".TimeQty",
                    ".NextDate"
                };
                foreach (string key in keys)
                {
                    if (settings.Values[TaskName + key] != null)
                        await _workLogger.Debug(string.Format(TaskName + key + " = {0}", settings.Values[TaskName + key]));
                }
            }).AsAsyncAction();
        }
#endif

        private async Task RunWork()
        {
            Debug.WriteLine("Esecuzione attività...", TaskName);
            await _workLogger.Debug(string.Format("Esecuzione attività, {0}...", _executionMinutes.ToString("dd-MM-yyyy HH:mm:ss")));
#if DEBUG
            await PrintLocalSetting();
#endif

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
#if !TEST_BACKGROUND_TASK
                    DoWork();
#endif
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
#if TEST_BACKGROUND_TASK            
            for (uint test = 1; test <= NumberOfTest; test++)
            {
                try
                {
                    await SetTestInput(test);
                    await RunWork();
                    await CheckTestOutput(test);
                }
                catch(Exception e)
                {
                    await _workLogger.Error(e.Message);
                }
            }
#else
            await RunWork();            
#endif
            _deferral.Complete();
        }

        #region BACKGROUND_TASK_TEST
#if TEST_BACKGROUND_TASK
        const uint NumberOfTest = 3;

        private async Task SetTestInput(uint test)
        {
            uint timeQty;
            int timeKind;
            DateTime startDateTime, scheduleDateTime, nextDateTime;
            ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;

            if (test == 1)
            {
                // Regolare
                // Dalle 02-08-2020 16:00 ogni settimana
                // Schedulazione alle 02-08-2020 15:00
                // Esecuzione alle 02-08-2020 16:01
                scheduleDateTime = DateTime.ParseExact("02-08-2020 15:00", "dd-MM-yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                _executionMinutes = DateTime.ParseExact("02-08-2020 16:01", "dd-MM-yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                startDateTime = DateTime.ParseExact("02-08-2020 16:00", "dd-MM-yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);                
                timeKind = 2; // Giorni
                timeQty = 7;                
            }
            else if (test == 2)
            {
                // Regolare più periodi per intervallo successivo.
                // Dalle 02-08-2020 16:00 ogni 6 mesi
                // Schedulazione alle 01-08-2020 16:00
                // Esecuzione alle 02-08-2020 16:01
                scheduleDateTime = DateTime.ParseExact("01-08-2020 16:00", "dd-MM-yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                _executionMinutes = DateTime.ParseExact("02-08-2020 16:01", "dd-MM-yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                startDateTime = DateTime.ParseExact("02-08-2020 16:00", "dd-MM-yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                timeKind = 3; // Mesi
                timeQty = 6;                          
            }
            else if (test == 3)
            {
                // Regolare più periodi del primo intervallo
                // Dalle 01-01-2021 16:00 ogni settimana
                // Schedulazione alle 02-08-2020 15:00
                // Esecuzione alle 02-09-2020 16:05                
                scheduleDateTime = DateTime.ParseExact("02-08-2020 15:00", "dd-MM-yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                _executionMinutes = DateTime.ParseExact("02-09-2020 16:05", "dd-MM-yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                startDateTime = DateTime.ParseExact("01-01-2021 16:00", "dd-MM-yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                timeKind = 2;
                timeQty = 7;
            }            
            else
                throw new Exception("Test non riconosciuto");

            uint interval = (uint)Math.Round((startDateTime - scheduleDateTime).TotalMinutes, 0);            

            await _workLogger.Debug("=======================================");
            await _workLogger.Debug(string.Format("TEST {0}", test));
            await _workLogger.Debug("=======================================");
            settings.Values[TaskName + ".Interval"] = interval;
            settings.Values[TaskName + ".TimeKind"] = timeKind;
            settings.Values[TaskName + ".TimeQty"] = timeQty;
            settings.Values[TaskName + ".NextDate"] = scheduleDateTime.AddMinutes(interval).ToString("dd-MM-yyyy, HH:mm:ss");            
        }

        private async Task CheckTestOutput(uint test)
        {
            uint interval, value;
            uint timeQty, timeKind;
            DateTime nextDate, valueDT;
            ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;

            if (test == 1)
            {
                interval = 7 * 24 * 60 - 1;
                timeKind = 2;
                timeQty = 7;
                nextDate = DateTime.ParseExact("09-08-2020 16:00", "dd-MM-yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (test == 2)
            {
                nextDate = DateTime.ParseExact("02-02-2021 16:00", "dd-MM-yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                DateTime start = DateTime.ParseExact("02-08-2020 16:01", "dd-MM-yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                interval = (uint)Math.Round((nextDate - start).TotalMinutes, 0);
                timeKind = 3;
                timeQty = 6;
            }
            else if (test == 3)
            {
                nextDate = DateTime.ParseExact("01-01-2021 16:00", "dd-MM-yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                DateTime start = DateTime.ParseExact("02-08-2020 15:00", "dd-MM-yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                interval = (uint)Math.Round((nextDate - start).TotalMinutes, 0);
                timeKind = 2;
                timeQty = 7;
            }            
            else
                throw new Exception("Test non riconosciuto");

            await _workLogger.Debug("=======================================");
            await _workLogger.Debug(string.Format("RISULTATI TEST {0}", test));
            await _workLogger.Debug("=======================================");
            if ((value = uint.Parse(settings.Values[TaskName + ".Interval"].ToString())) != interval)
                await _workLogger.Debug(string.Format("Interval FALLITO. Trovato {0}, Atteso {1}", value, interval));
            else
                await _workLogger.Debug(string.Format("Interval PASSATO. Trovato {0}", value));            
            if ((value = uint.Parse(settings.Values[TaskName + ".TimeKind"].ToString())) == timeKind)
                await _workLogger.Debug(string.Format("TimeKind PASSATO. Trovato {0}", value));
            else
                await _workLogger.Debug(string.Format("TimeKind FALLITO. Trovato {0}, Atteso {1}", value, timeKind));
            if ((value = uint.Parse(settings.Values[TaskName + ".TimeQty"].ToString())) == timeQty)
                await _workLogger.Debug(string.Format("TimeQty PASSATO. Trovato {0}", value));
            else
                await _workLogger.Debug(string.Format("TimeQty FALLITO. Trovato {0}, Atteso {1}", value, timeQty));
            if ((valueDT = DateTime.ParseExact(settings.Values[TaskName + ".NextDate"].ToString(), "dd-MM-yyyy, HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)) != nextDate)
                await _workLogger.Debug(string.Format("NextDate FALLITO. Trovato {0}, Atteso {1}", valueDT.ToString("dd-MM-yyyy, HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture), nextDate.ToString("dd-MM-yyyy, HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)));
            else
                await _workLogger.Debug(string.Format("NextDate PASSATO. Trovato {0}", valueDT.ToString("dd-MM-yyyy, HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)));            
            await _workLogger.Debug("=======================================");
        }
#endif
#endregion
    }
}
