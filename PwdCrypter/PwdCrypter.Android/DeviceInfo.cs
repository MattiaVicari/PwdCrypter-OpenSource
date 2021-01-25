using Xamarin.Forms;

[assembly: Dependency(typeof(PwdCrypter.Droid.DeviceInfo))]
namespace PwdCrypter.Droid
{
    class DeviceInfo : IDeviceInfo
    {
        public string GetDeviceID()
        {
            return Android.Provider.Settings.Secure.GetString(Android.App.Application.Context.ContentResolver, Android.Provider.Settings.Secure.AndroidId);
        }
    }
}