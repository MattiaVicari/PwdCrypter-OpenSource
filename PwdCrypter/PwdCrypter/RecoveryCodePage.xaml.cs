using PwdCrypter.Controls;
using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RecoveryCodePage : ContentPage
    {
        public event EventHandler OnClose;

        public RecoveryCodePage()
        {
            InitializeComponent();
            SetControls();
        }

        private void SetControls()
        {
            entryCode.AddFeature("clipboard-check", "clipboard-check", 
                (object sender, EventArgs e, FeatureInfo featureInfo) => 
                {
                    if (sender is FormEntry view)
                    {
                        view.OnFeature(featureInfo);
                        SendDataToClipboard(view.Text);
                    }
                });
        }

        private void SendDataToClipboard(string text)
        {
            Plugin.Clipboard.CrossClipboard.Current.SetText(text);
            App.SendToastNotification(App.Title, AppResources.msgDataCopyToClipboard);
            App.SendLigthNotification(frameNotification,
                                      lblNotification,
                                      AppResources.msgDataCopyToClipboard);
        }

        private async void BtnClose_OnClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync(true);
            OnClose?.Invoke(sender, e);
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
    }
}