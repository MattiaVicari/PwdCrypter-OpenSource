using Xamarin.Forms;

[assembly: Dependency(typeof(PwdCrypter.UWP.ApplicationCloser))]
namespace PwdCrypter.UWP
{
    class ApplicationCloser : IApplicationCloser
    {
        public void Close()
        {
            App.Current.Exit();
        }
    }
}
