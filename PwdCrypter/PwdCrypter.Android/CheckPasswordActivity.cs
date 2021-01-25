using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Newtonsoft.Json.Linq;

namespace PwdCrypter.Droid
{
    [Activity(
        Label = "CheckPasswordActivity", 
        Icon = "@mipmap/icon", 
        Theme = "@style/MainTheme", 
        MainLauncher = false, 
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class CheckPasswordActivity : Activity
    {
        const string Tag = "CheckPasswordActivity";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (MainActivity.Instance == null)
            {
                var intent = new Intent(this, typeof(MainActivity));
                StartActivity(intent);
            }

            string launch = this.Intent.GetStringExtra("launch");
            if (!string.IsNullOrEmpty(launch))
                EvaluateArguments(launch);
            Finish();
        }

        private void EvaluateArguments(string args)
        {
            Log.Info(Tag, "Arguments received {0}", args);

            try
            {
                JObject jsonData = JObject.Parse(args);
                if (jsonData != null)
                {
                    if (jsonData.ContainsKey("action"))
                    {
                        string actionName = jsonData.GetValue("action").ToObject<string>();
                        if (App.CurrentApp != null && App.IsLoggedIn)
                            App.CurrentApp.DoAction(actionName);
                        else
                            App.PushNotificationData = actionName;
                    }
                }
            }
            catch (Newtonsoft.Json.JsonReaderException ex)
            {
                Log.Debug(Tag, "The argument is not a valid JSON. Error: {0}", ex.Message);
                App.Logger.Error(string.Format("The argument {0} is not a valid JSON. Error: {1}", args, ex.Message));
            }
            catch(Exception ex)
            {
                Log.Debug(Tag, "Error: ", ex.Message);
                App.Logger.Error(string.Format("Error occurred during the parsing of the argument {0}. Error: {1}", args, ex.Message));
            }
        }
    }
}