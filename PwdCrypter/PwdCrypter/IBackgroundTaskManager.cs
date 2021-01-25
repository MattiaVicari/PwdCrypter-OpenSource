using System;
using System.Threading.Tasks;

namespace PwdCrypter
{
    public enum TimeKind { Minute, Hour, Day, Month, Year }

    public interface IBackgroundTaskManager
    {        
        Task Register(string taskName, string taskEntryPoint, DateTime startDateTime, TimeKind tKind, uint interval);
        void Unregister(string taskName);
        bool IsRegistered(string taskName);
        DateTime GetNextExecutionDate(string taskName);
    }
}
