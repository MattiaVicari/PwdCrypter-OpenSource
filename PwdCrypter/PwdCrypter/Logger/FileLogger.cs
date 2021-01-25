using System;
using System.IO;
using System.Text;
using Xamarin.Forms;

[assembly: Dependency(typeof(PwdCrypter.Logger.FileLogger))]
namespace PwdCrypter.Logger
{
    public class FileLoggerFake : ILogger
    {
        public void Debug(string logmessage)
        {
            // Nulla
        }

        public void Error(string logmessage)
        {
            // Nulla
        }

        public void Inform(string logmessage)
        {
            // Nulla
        }

        public void Warning(string logmessage)
        {
            // Nulla
        }
    }

    public class FileLogger : ILogger
    {
        const string LogFileName = "pwdcrypter.log";
        const string LogMessagePattern = "[{0}] [{1}] [{2}] - {3}";

        private string _fileFolder;
        private string FilePath => GetFilePath();
        private string LogName => App.Title;

        private string GetFilePath()
        {
            try
            {
                if (string.IsNullOrEmpty(_fileFolder))
                    _fileFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(_fileFolder, LogFileName);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                return LogFileName;
            }
        }

        private string GetFormattedLogMessage(string logKind, string message)
        {
            return string.Format(LogMessagePattern, DateTime.Now, logKind, LogName, message) + "\n";
        }

        public void Debug(string logmessage)
        {
#if DEBUG
            try
            {
                File.AppendAllText(FilePath, GetFormattedLogMessage("DEBUG", logmessage), Encoding.UTF8);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
#endif
        }

        public void Error(string logmessage)
        {
            try
            {
                File.AppendAllText(FilePath, GetFormattedLogMessage("ERROR", logmessage + "\n" + Environment.StackTrace), Encoding.UTF8);
            }
            catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }

        public void Inform(string logmessage)
        {
            try
            {
                File.AppendAllText(FilePath, GetFormattedLogMessage("INFO", logmessage), Encoding.UTF8);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }

        public void Warning(string logmessage)
        {
            try
            {
                File.AppendAllText(FilePath, GetFormattedLogMessage("WARN", logmessage), Encoding.UTF8);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }
    }
}
