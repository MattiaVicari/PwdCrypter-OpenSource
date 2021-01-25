using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TOTPPage : ContentPage
    {
        private TwoFAInfo Info = null;
        private bool _enableRecoveryMode = true;
        private bool _recoveryMode = false;

        public event EventHandler OnSuccess;
        public event EventHandler OnFail;
        public event EventHandler OnCancel;

        public bool RecoveryMode 
        {
            get => _recoveryMode;
            set
            {
                _recoveryMode = value;
                EnableRecoveryMode = !value;
            }
        }
        public bool EnableRecoveryMode 
        { 
            get => _enableRecoveryMode;            
            set
            {
                _enableRecoveryMode = value;
                linkRecoveryCode.IsVisible = value;
            }
        }

        public TOTPPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext != null && (BindingContext is TwoFAInfo data))
                Info = data;
            else
                Info = App.PwdManager.Access2FA;
            UpdateContent();
        }

        private void UpdateContent()
        {
            Title = RecoveryMode ? AppResources.titleRecoveryCode : AppResources.titleTOTP;
            lblTitle.Text = Title;
            lblMessage.Text = RecoveryMode ? AppResources.txtTOTPRecoveryCode : AppResources.txtTOTP;
        }

        private async void BtnConfirm_OnClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(entryTOTP.Text))
                return;

            // Verifica se il codice OTP inserito è corretto
            ITwoFactorAuth twoFactorAuth = DependencyService.Get<ITwoFactorAuth>();
            if (twoFactorAuth == null)
            {
                await DisplayAlert(App.Title, AppResources.errTwoFactorNotAvailable, "OK");
                return;
            }
            if (!Info.IsAvailable)
            {
                await DisplayAlert(App.Title, AppResources.errTwoFactorConfig2, "OK");
                return;
            }
            
            string code = twoFactorAuth.GetTOTP(Info.Code);
#if DEBUG
            System.Diagnostics.Debug.WriteLine("TOTP: " + code);
#endif
            if ((entryTOTP.Text.CompareTo(code) == 0 && !RecoveryMode) 
                || (entryTOTP.Text.CompareTo(Info.BackupCode) == 0 && RecoveryMode))
            {
                if (RecoveryMode)
                {
                    // Genera un nuovo codice di ripristino  
                    Info.BackupCode = twoFactorAuth.GetBackupCode();
                    App.PwdManager.Save2FAData(Info);
                    // Visualizza il nuovo codice di backup
                    RecoveryCodePage codePage = new RecoveryCodePage
                    {
                        BindingContext = new
                        {
                            Code = Info.BackupCode
                        }
                    };
                    codePage.OnClose += RecoveryCodePage_OnClose;
                    await Navigation.PushModalAsync(codePage, true);
                }
                else
                {
                    await Navigation.PopModalAsync(true);
                    OnSuccess?.Invoke(this, new EventArgs());
                }
            }
            else
            {                
                await DisplayAlert(App.Title, RecoveryMode ? AppResources.errWrongRecoveryCode : AppResources.errTOTPWrongCode, "OK");
                OnFail?.Invoke(this, new EventArgs());
            }
        }

        private async void RecoveryCodePage_OnClose(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync(true);
            OnSuccess?.Invoke(this, new EventArgs());
        }

        private void BtnCancel_OnClicked(object sender, EventArgs e)
        {
            Navigation.PopModalAsync(true);
            OnCancel?.Invoke(this, new EventArgs());            
        }

        private void ContentPage_SizeChanged(object sender, EventArgs e)
        {
            const double AutoWidth = -1;
            if (Width > App.ViewStepMaxWidth)
            {
                double fixedWidth = App.ViewStepMaxWidth - 140;
                entryTOTP.HorizontalOptions = LayoutOptions.CenterAndExpand;
                entryTOTP.WidthRequest = fixedWidth;
            }
            else
            {
                entryTOTP.HorizontalOptions = LayoutOptions.FillAndExpand;
                entryTOTP.WidthRequest = AutoWidth;
            }
        }

        private void RecoveryCode_OnClicked(object sender, EventArgs e)
        {
            entryTOTP.Text = "";
            RecoveryMode = true;
            UpdateContent();
        }
    }
}