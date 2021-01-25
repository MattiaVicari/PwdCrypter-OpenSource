using Xamarin.Forms;

namespace PwdCrypter
{
    public interface INotifier
    {
        void SetView(ContentView contentView, Label messageView);
        void SendNotification(string title, string message, string launchArg = null);
        void SendLightNotification(string message);
    }
}
