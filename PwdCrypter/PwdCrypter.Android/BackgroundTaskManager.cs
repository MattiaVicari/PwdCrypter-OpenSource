using System;
using System.Threading.Tasks;
using Android.Runtime;
using Android.Util;
using AndroidX.Work;
using PwdCrypter.Droid.Service;
using Xamarin.Forms;

[assembly: Dependency(typeof(PwdCrypter.Droid.BackgroundTaskManager))]
namespace PwdCrypter.Droid
{
    public class BackgroundTaskManager : IBackgroundTaskManager
    {
        const string Tag = "BackgroundTaskManager";

        private readonly WorkManager AppWorkManager = null;
        
        public BackgroundTaskManager()
        {            
            var context = Android.App.Application.Context;
            AppWorkManager = WorkManager.GetInstance(context);
        }

        public DateTime GetNextExecutionDate(string taskName)
        {
            var infos = AppWorkManager.GetWorkInfosByTag(taskName).Get();
            Java.Util.ArrayList list = infos.JavaCast<Java.Util.ArrayList>();
            if (list.IsEmpty)
                return DateTime.MinValue;

            WorkInfo info = (WorkInfo)list.Get(0);
            /* Non avrò mai un valore in quanto OutputData non è disponibile per i PeriodicWorkRequest perché
             * non vanno mai nello stato di SUCCEEDED. 
             * https://stackoverflow.com/questions/57845459/android-workmanager-cannot-get-output-data-from-periodicworkrequest 
             * https://developer.android.com/reference/androidx/work/WorkInfo.html */
            string nextRun = info.OutputData.GetString("NextDate");
            if (string.IsNullOrEmpty(nextRun))
                return DateTime.MinValue;
            return DateTime.ParseExact(nextRun, "dd-MM-yyyy, HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        }

        public bool IsRegistered(string taskName)
        {
            var infos = AppWorkManager.GetWorkInfosByTag(taskName).Get();
            Java.Util.ArrayList list = infos.JavaCast<Java.Util.ArrayList>();
            if (list.IsEmpty)
                return false;

            WorkInfo info = (WorkInfo)list.Get(0);
            Log.Info(Tag, "Work {0} is {1}", taskName, info.GetState().ToString());
#if DEBUG
            for (int i=0; i < list.Size(); i++)
            {
                info = (WorkInfo)list.Get(i);
                Log.Debug(Tag, "Work state: {0}", info.GetState().ToString());
            }
#endif
            return true;
        }       

        public Task Register(string taskName, string taskEntryPoint, DateTime startDateTime, TimeKind tKind, uint interval)
        {
            if (IsRegistered(taskName))
                return Task.CompletedTask;            

            try
            {
                DateTime nowNoSecond = DateTime.ParseExact(DateTime.Now.ToString("dd-MM-yyyy HH:mm:00"), "dd-MM-yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                uint initialDelay = (uint)Math.Round((startDateTime - nowNoSecond).TotalMinutes, 0);

                uint timeInterval = interval;
                Java.Util.Concurrent.TimeUnit unit = Java.Util.Concurrent.TimeUnit.Minutes;
                switch (tKind)
                {
                    case TimeKind.Minute:
                        unit = Java.Util.Concurrent.TimeUnit.Minutes;
                        break;
                    case TimeKind.Hour:
                        unit = Java.Util.Concurrent.TimeUnit.Hours;
                        break;
                    case TimeKind.Day:
                        unit = Java.Util.Concurrent.TimeUnit.Days;
                        break;
                    case TimeKind.Month:
                        unit = Java.Util.Concurrent.TimeUnit.Days;
                        timeInterval *= 30;
                        break;
                    case TimeKind.Year:
                        unit = Java.Util.Concurrent.TimeUnit.Days;
                        timeInterval *= 365;
                        break;
                    default:
                        throw new Exception(string.Format("Categoria di tempo {0} non prevista", tKind));
                }

                PeriodicWorkRequest.Builder workRequestedBuilder;
                if (taskName.CompareTo(App.CheckPasswordBackgroundTaskName) == 0)
                    workRequestedBuilder = PeriodicWorkRequest.Builder.From<CheckPasswordWorker>(timeInterval, unit);
                else if (taskName.CompareTo(App.BackupBackgroundTaskName) == 0)
                    workRequestedBuilder = PeriodicWorkRequest.Builder.From<BackupWorker>(timeInterval, unit);
                else
                    throw new Exception("Task not found");
                
                Data data = new Data.Builder()
                    .PutLong("interval", unit.ToMinutes(timeInterval))
                    .Build();
                PeriodicWorkRequest workRequested = (PeriodicWorkRequest)workRequestedBuilder
                    .AddTag(taskName)
                    .SetInitialDelay(initialDelay, Java.Util.Concurrent.TimeUnit.Minutes)
                    .SetInputData(data)
                    .Build();
                AppWorkManager.EnqueueUniquePeriodicWork(taskName, ExistingPeriodicWorkPolicy.Replace, workRequested);                    
            }
            catch(Exception e)
            {           
                Log.Error(Tag, string.Format("Error occurred during scheduling the work with name \"{0}\": {1}", taskName, e.Message));
            }
            return Task.CompletedTask;
        }

        public void Unregister(string taskName)
        {
            AppWorkManager.CancelAllWorkByTag(taskName);
            AppWorkManager.PruneWork();
            Log.Info(Tag, "Work {0} cancelled", taskName);
        }
    }
}