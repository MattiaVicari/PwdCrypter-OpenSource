using System;
using System.Linq;
using Android.Content;
using Android.Util;
using AndroidX.Work;

namespace PwdCrypter.Droid.Service
{
    public class CheckPasswordWorker : Worker
    {
        private const string Tag = "CheckPasswordWorker";
        private readonly Context _context;

        public CheckPasswordWorker(Context context, WorkerParameters workerParameters)
            : base(context, workerParameters)
        {
            _context = context;
        }

        public override Result DoWork()
        {
            try
            {
                Notifier notifier = new Notifier
                {
                    Context = _context,
                    NotificationIntent = new Intent(_context, typeof(CheckPasswordActivity))
                        .SetFlags(ActivityFlags.NewTask)
                        .SetFlags(ActivityFlags.SingleTop)
                };
                notifier.SendNotification("PwdCrypter",
                    _context.Resources.GetString(Resource.String.msgCheckPassword),
                    "{\"action\": \"checkpassword\"}");
                Log.Info(Tag, "Local notification sent with success");

                // Restituisce la data prevista per la prossima esecuzione                
                long interval = InputData.GetLong("interval", 0);                
                DateTime nextRun = DateTime.MinValue;
                if (interval > 0)
                    nextRun = DateTime.Now.AddMinutes(interval);                
                Data data = new Data.Builder()
                    .PutString("NextDate", nextRun.ToString("dd-MM-yyyy, HH:mm:ss"))
                    .Build();

                return Result.InvokeSuccess(data);
            }
            catch(Exception e)
            {
                Log.Error(Tag, "Operation failed. Error: {0}", e.Message);
                return Result.InvokeFailure();
            }
        }
    }
}