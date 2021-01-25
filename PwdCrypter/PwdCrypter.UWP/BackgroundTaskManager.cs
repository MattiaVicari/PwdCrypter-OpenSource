using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Xamarin.Forms;

[assembly: Dependency(typeof(PwdCrypter.UWP.BackgroundTaskManager))]
namespace PwdCrypter.UWP
{
    class BackgroundTaskManager : IBackgroundTaskManager
    {        
        private const uint MinTimePeriod = 15;  // Attesa minima di 15 minuti

        public DateTime GetNextExecutionDate(string taskName)
        {
            DateTime nextDate = DateTime.MinValue;

            var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
            string nextRun = settings.Values[taskName + ".NextDate"]?.ToString();
            if (!string.IsNullOrEmpty(nextRun))
                nextDate = DateTime.ParseExact(nextRun, "dd-MM-yyyy, HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            
            return nextDate;
        }

        public bool IsRegistered(string taskName)
        {            
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == taskName)
                    return true;                                
            }
            return false;
        }

        public async Task Register(string taskName, string taskEntryPoint, DateTime startDateTime, TimeKind tKind, uint interval)
        {
            if (IsRegistered(taskName))
                return;

            try
            {
                if (startDateTime < DateTime.Now)
                    throw new Exception("Data di partenza anteriore alla data odierna");

                var settings = Windows.Storage.ApplicationData.Current.LocalSettings;

                BackgroundExecutionManager.RemoveAccess();
                await BackgroundExecutionManager.RequestAccessAsync();

                var builder = new BackgroundTaskBuilder
                {
                    Name = taskName,
                    TaskEntryPoint = taskEntryPoint
                };
                builder.SetTrigger(new TimeTrigger(MinTimePeriod, false)); // ATTENZIONE: meno di 15 minuti non si può fare

                // Calcola il primo intervallo per partire alla data indicata
                DateTime nowNoSeconds = DateTime.ParseExact(DateTime.Now.ToString("dd-MM-yyyy HH:mm:00"), "dd-MM-yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                uint timeIntervalFirst = (uint)Math.Round((startDateTime - nowNoSeconds).TotalMinutes, 0);
                
                BackgroundTaskRegistration task = builder.Register();
                settings.Values[taskName + ".Interval"] = timeIntervalFirst;
                settings.Values[taskName + ".TimeKind"] = (int)tKind;
                settings.Values[taskName + ".TimeQty"] = interval;
                settings.Values[taskName + ".NextDate"] = nowNoSeconds.AddMinutes(timeIntervalFirst).ToString("dd-MM-yyyy, HH:mm:ss");

                Debug.WriteLine(string.Format("Registrata l'attività {0} con intervallo {1} {2}, a partire dal {3}", 
                    taskName, interval, tKind, startDateTime.ToString("dd/MM/yyyy HH:mm")));
            }
            catch(Exception ex)
            {                
                Debug.WriteLine(string.Format("Errore durante la registrazione del task {0}: {1}", taskName, ex.Message));
                Unregister(taskName);
            }
        }

        private void ClearSettings(string taskName)
        {
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
            settings.Values[taskName + ".Interval"] = 0;
            settings.Values[taskName + ".TimeKind"] = null;
            settings.Values[taskName + ".TimeQty"] = null;
            settings.Values[taskName + ".NextDate"] = "";
        }

        public void Unregister(string taskName)
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == taskName)
                {
                    task.Value.Unregister(true);
                    ClearSettings(taskName);
                    Debug.WriteLine(string.Format("Cancellata registrazione dell'attività {0}", taskName));                    
                }
            }
        }
    }
}
