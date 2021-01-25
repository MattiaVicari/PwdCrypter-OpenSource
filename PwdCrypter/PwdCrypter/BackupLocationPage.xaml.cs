using System;
using System.Collections.Generic;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
    /// <summary>
    /// Locazione del backup
    /// </summary>
    public enum BackupLocation
    {
        /// <summary>
        /// Backup in locale
        /// </summary>
        Local,
        /// <summary>
        /// Backup su Cloud
        /// </summary>
        Cloud
    };

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BackupLocationPage : ContentPage
    {
        public static Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }
        public static Dictionary<string, string> ButtonImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#304ba6" }, { "currentOpacity", "1.0" } };
        }

        public delegate void EventHandlerSelection(object sender, BackupLocation location);
        public event EventHandlerSelection OnSelection = null;

        public static double LogoLocationWidth
        {
            get
            {
                if (Device.RuntimePlatform == Device.UWP)
                    return 200;
                return 150;
            }
        }

        public BackupLocationPage()
        {
            InitializeComponent();
        }

        private void ContentPage_SizeChanged(object sender, EventArgs e)
        {
            const double AutoWidth = -1;

            if (Width > App.ViewStepMaxWidth)
            {
                if (Height < App.ViewStepMaxHeight)
                {
                    btnCancel.HorizontalOptions = LayoutOptions.FillAndExpand;
                    btnCancel.WidthRequest = AutoWidth;
                }
                else
                {
                    double fixedWidth = App.ViewStepMaxWidth - 140;
                    btnCancel.HorizontalOptions = LayoutOptions.CenterAndExpand;
                    btnCancel.WidthRequest = fixedWidth;
                }
            }
            else
            {
                btnCancel.HorizontalOptions = LayoutOptions.FillAndExpand;
                btnCancel.WidthRequest = AutoWidth;
            }
        }

        private async void BtnLocal_OnClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync(true);
            OnSelection?.Invoke(sender, BackupLocation.Local);
        }

        private async void BtnCloud_OnClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync(true);
            OnSelection?.Invoke(sender, BackupLocation.Cloud);
        }

        private async void BtnCancel_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync(true);
        }
    }
}