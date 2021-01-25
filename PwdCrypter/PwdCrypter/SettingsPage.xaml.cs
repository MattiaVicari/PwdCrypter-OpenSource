using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingsPage : ContentPage
    {
        public static Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }
        public static bool CanReceiveNotification { get => App.PushNotificationManager.CanReceiveNotification(); }      

        private Dictionary<string, int> TimeoutOptions;
        private bool IsLoading;

        public SettingsPage()
        {
            InitializeComponent();

            LoadSettings();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            App.PwdSecurity.BeginOperation();
        }

        private void LoadSettings()
        {
            const int DefaultOption = 4;    // 5 minuti

            IsLoading = true;

            TimeoutOptions = new Dictionary<string, int>
            {
                { AppResources.txtNever,            -1 },
                { "30 " + AppResources.txtSeconds,  30 },
                { "1 " + AppResources.txtMinute,    60 },
                { "2 " + AppResources.txtMinutes,  120 },
                { "5 " + AppResources.txtMinutes,  300 },
                { "10 " + AppResources.txtMinutes, 600 },
                { "15 " + AppResources.txtMinutes, 900 }
            };

            int index = 0;
            foreach (KeyValuePair<string, int> item in TimeoutOptions)
            {
                pickerSessionTimeout.Items.Add(item.Key);
                if (App.Settings.SessionTimeout == item.Value)
                    pickerSessionTimeout.SelectedIndex = index;
                index++;
            }

            if (pickerSessionTimeout.SelectedIndex < 0)
                pickerSessionTimeout.SelectedIndex = DefaultOption;
            switchBrowserExtHelp.IsToggled = App.Settings.BrowserExtensionHelp;
            switchLocalNotification.IsToggled = App.Settings.LocalNotification;
            switchPushNotification.IsToggled = App.Settings.PushNotification;

            IsLoading = false;
        }

        private async Task SaveSettings()
        {
            try
            {
                App.Settings.SessionTimeout = TimeoutOptions.ElementAt(pickerSessionTimeout.SelectedIndex).Value;
                App.Settings.BrowserExtensionHelp = switchBrowserExtHelp.IsToggled;
                App.Settings.LocalNotification = switchLocalNotification.IsToggled;
                App.Settings.PushNotification = switchPushNotification.IsToggled;
                App.Settings.WriteSettings();
            }
            catch(Exception e)
            {
                await DisplayAlert(App.Title, AppResources.errSettingsUpdate + " " + e.Message, "OK");
            }

            // Per Android attiva o disabilita la sottoscrizione
            if (Device.RuntimePlatform == Device.Android)
            {
                if (!App.Settings.PushNotification)
                    await App.PushNotificationManager.Unsubscribe();
                else
                    await App.PushNotificationManager.Subscribe();
            }
        }

        private void ContentPage_SizeChanged(object sender, EventArgs e)
        {
            const double AutoWidth = -1;

            if (Width > App.ViewStepMaxWidth)
            {
                double fixedWidth = App.ViewStepMaxWidth - 140;

                gridMain.HorizontalOptions = LayoutOptions.CenterAndExpand;
                gridMain.WidthRequest = fixedWidth;
            }
            else
            {
                gridMain.HorizontalOptions = LayoutOptions.FillAndExpand;
                gridMain.WidthRequest = AutoWidth;
            }
        }

        protected override bool OnBackButtonPressed()
        {
            return true;
        }

        private async void PickerSessionTimeout_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!IsLoading)
                await SaveSettings();
        }

        private async void SwitchToggleChanged_Toggled(object sender, ToggledEventArgs e)
        {
            if (!IsLoading)
                await SaveSettings();
        }

        private void OpenPushNotificationSettings_Clicked(object sender, EventArgs e)
        {
            // Per Windows 10 permette di abilitare o disabilitare la ricezione
            // delle notifiche push, a prescindere che sia aperto o no il canale.
            if (Device.RuntimePlatform != Device.UWP)
            {
                Debug.WriteLine("WARNING: this feature should not be available for this platform!");
                return;
            }
            App.PushNotificationManager.Unsubscribe();
        }

        private async void SecurityAccess_OnClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SecurityAccessPage(), true);
        }
    }
}