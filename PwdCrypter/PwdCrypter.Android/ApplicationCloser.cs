using Xamarin.Forms;

[assembly: Dependency(typeof(PwdCrypter.Droid.ApplicationCloser))]
namespace PwdCrypter.Droid
{
    class ApplicationCloser : IApplicationCloser
    {
        public void Close()
        {
            Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
        }
    }
}