using System;
using System.Collections.Generic;
using System.Text;

namespace PwdCrypter.Logger
{
    public interface ILogger
    {
        void Debug(string logmessage);
        void Inform(string logmessage);
        void Error(string logmessage);
        void Warning(string logmessage);
    }
}
