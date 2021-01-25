using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace PwdCrypterBackground
{
    public sealed class Logger
    {
        const string BaseLogFile = "PwdCrypterBackgroundTask_";
        const string LogMessagePattern = "[{0}] [{1}] [{2}] - {3}";
        const string LogName = "PwdCrypterBackgroundTask";

        private readonly StorageFolder AppFolder = null;
        private readonly string LogFileName;

        public Logger(string suffix)
        {
            AppFolder = ApplicationData.Current.LocalFolder;
            LogFileName = BaseLogFile + suffix;
        }

        private string GetFormattedLogMessage(string logKind, string message)
        {
            return string.Format(LogMessagePattern, DateTime.Now, logKind, LogName, message) + "\n";
        }

        public IAsyncAction Info(string message)
        {
            try
            {
                return Task.Run(async () =>
                {
                    StorageFile log = await AppFolder.CreateFileAsync(LogFileName, CreationCollisionOption.OpenIfExists);
                    await File.AppendAllTextAsync(log.Path, GetFormattedLogMessage("INFO", message));
                }).AsAsyncAction();                
            }
            catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                return Task.CompletedTask.AsAsyncAction();
            }
        }

        public IAsyncAction Error(string message)
        {
            try
            {
                return Task.Run(async () =>
                {
                    StorageFile log = await AppFolder.CreateFileAsync(LogFileName, CreationCollisionOption.OpenIfExists);
                    await File.AppendAllTextAsync(log.Path, GetFormattedLogMessage("ERROR", message + "\n" + Environment.StackTrace));
                }).AsAsyncAction();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                return Task.CompletedTask.AsAsyncAction();
            }
        }

        public IAsyncAction Warning(string message)
        {
            try
            {
                return Task.Run(async () =>
                {
                    StorageFile log = await AppFolder.CreateFileAsync(LogFileName, CreationCollisionOption.OpenIfExists);
                    await File.AppendAllTextAsync(log.Path, GetFormattedLogMessage("WARN", message));
                }).AsAsyncAction();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                return Task.CompletedTask.AsAsyncAction();
            }
        }

        public IAsyncAction Debug(string message)
        {
#if DEBUG
            try
            {
                return Task.Run(async () =>
                {
                    StorageFile log = await AppFolder.CreateFileAsync(LogFileName, CreationCollisionOption.OpenIfExists);
                    await File.AppendAllTextAsync(log.Path, GetFormattedLogMessage("DEBUG", message));
                }).AsAsyncAction();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                return Task.CompletedTask.AsAsyncAction();
            }
#else
            return Task.CompletedTask.AsAsyncAction();
#endif
        }
    }
}
