using PwdCrypter.Controls;
using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TwoFactorAuthPage : ContentPage
    {
        private readonly ITwoFactorAuth TwoFactorAuth = null;
        private string Secret = "";
        private string BackupCode = "";

        public TwoFactorAuthPage()
        {
            InitializeComponent();
            TwoFactorAuth = DependencyService.Get<ITwoFactorAuth>();
            SetControls();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();            
            if (string.IsNullOrEmpty(Secret))
                await CreateTwoFactorCode();
        }

        private void SetControls()
        {
            entrySecret.AddFeature("clipboard-check", "clipboard-check", OnCopyData);
            entryBackupCode.AddFeature("clipboard-check", "clipboard-check", OnCopyData);
        }

        private void OnCopyData(object sender, EventArgs e, FeatureInfo featureInfo)
        {
            if (sender is FormEntry view)
            {
                view.OnFeature(featureInfo);
                SendDataToClipboard(view.Text);
            }
        }

        private void SendDataToClipboard(string text)
        {
            Plugin.Clipboard.CrossClipboard.Current.SetText(text);
            App.SendToastNotification(App.Title, AppResources.msgDataCopyToClipboard);
            App.SendLigthNotification(frameNotification,
                                      lblNotification,
                                      AppResources.msgDataCopyToClipboard);
        }

        private async Task CreateTwoFactorCode()
        {
            try
            {
                if (TwoFactorAuth == null)
                    throw new Exception("Service not available");

                Secret = TwoFactorAuth.GetSecret();
                BackupCode = TwoFactorAuth.GetBackupCode();
                string qrcode = TwoFactorAuth.GetQRCodeURL(Secret, "PwdCrypter", "YOUR_NAME");
                BindingContext = new
                {
                    QRCodeUrl = qrcode,
                    Secret,
                    BackupCode
                };
            }
            catch(Exception ex)
            {
                await DisplayAlert(App.Title, string.Format(AppResources.errTwoFactorConfig, ex.Message), "Ok");
            }
        }

        private void BtnCancel_OnClicked(object sender, EventArgs e)
        {
            Navigation.PopModalAsync(true);
        }

        private async void BtnConfirm_OnClicked(object sender, EventArgs e)
        {
            try
            {
                // Effettua una verifica
                TOTPPage verifyPage = new TOTPPage
                {
                    BindingContext = new TwoFAInfo
                    {
                        BackupCode = BackupCode,
                        Code = Secret
                    },
                    EnableRecoveryMode = false
                };
                verifyPage.OnCancel += VerifyPage_OnCancel;
                verifyPage.OnSuccess += VerifyPage_OnSuccess;

                await Navigation.PushModalAsync(verifyPage, true);                
            }
            catch(Exception ex)
            {
                await DisplayAlert(App.Title, string.Format(AppResources.errTwoFactorConfig, ex.Message), "Ok");
            }            
        }

        private async void VerifyPage_OnSuccess(object sender, EventArgs e)
        {
            try
            {
                App.PwdManager.Save2FAData(Secret, BackupCode);
                await App.PwdManager.UpdateLocalPasswordFile();
                await Navigation.PopModalAsync(true);
            }
            catch (Exception ex)
            {
                await DisplayAlert(App.Title, string.Format(AppResources.errTwoFactorConfig, ex.Message), "Ok");
            }
        }

        private async void VerifyPage_OnCancel(object sender, EventArgs e)
        {
            await DisplayAlert(App.Title, AppResources.msgTOTPCancelled, "Ok");
        }

        private void ContentPage_SizeChanged(object sender, EventArgs e)
        {
            const double AutoWidth = -1;

            double contentWidth = Width;
            if (Width > App.ViewStepMaxWidth)
            {
                double fixedWidth = App.ViewStepMaxWidth - 140;
                gridMain.WidthRequest = fixedWidth;
                gridMain.HorizontalOptions = LayoutOptions.Center;
                contentWidth = fixedWidth;
            }
            else
            {
                gridMain.WidthRequest = AutoWidth;
                gridMain.HorizontalOptions = LayoutOptions.FillAndExpand;
            }

            qrcode.HeightRequest = Math.Min(400, contentWidth - 50);
            qrcode.WidthRequest = qrcode.HeightRequest;   
        }
    }
}