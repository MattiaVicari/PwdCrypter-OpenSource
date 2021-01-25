using System;
using Xamarin.Forms;

namespace PwdCrypter
{
    public class Alert
    {
        public double Timeout { get; set; } = 0.0;
        public event EventHandler OnTimeout = null;

        public void StartTimer()
        {
            if (Timeout <= 0.0)
                throw new Exception(string.Format("Timeout {0} is invalid", Timeout));

            Device.StartTimer(TimeSpan.FromSeconds(Timeout), () =>
            {
                OnTimeout?.Invoke(this, new EventArgs());
                return false;
            });
        }
    }
}
