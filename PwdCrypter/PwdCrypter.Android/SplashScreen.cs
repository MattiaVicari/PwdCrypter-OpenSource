using Android.App;
using Android.Content.PM;
using Android.Content;
using Android.OS;

namespace PwdCrypter.Droid
{
    [Activity(Label = "PwdCrypter", MainLauncher = true, NoHistory = true, Theme = "@style/MainTheme.Splash", 
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class SplashScreen : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);
            Finish();
        }
    }
}