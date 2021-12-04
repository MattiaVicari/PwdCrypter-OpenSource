using Android.App;
using Android.Content;
using Android.OS;
using System;

namespace PwdCrypter.Droid
{
    [Activity(Label = "AuthGoogleCallbackActivity", NoHistory = true, LaunchMode = Android.Content.PM.LaunchMode.SingleTop)]
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "com.googleusercontent.apps.YOUR_CALLBACK_URL",
        DataPath = "/oauth2redirect"
    )]
    public class AuthGoogleCallbackActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            global::Android.Net.Uri uri_android = Intent.Data;
            // Convert Android.Net.Url to Uri
            var uri = new Uri(uri_android.ToString());

            // Close browser 
            var intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);

            // Load redirectUrl page
            if (OAuth2Authenticator.AuthenticationState != null)
                OAuth2Authenticator.AuthenticationState.OnPageLoading(uri);

            Finish();
        }
    }
}