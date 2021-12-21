using Newtonsoft.Json.Linq;
using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class LoginPage : ContentPage
	{
        private Utility.RedirectData RedirectInfo = null;
        private readonly IFingerprintAuth Fingerprint = null;
        private bool IsFingerprintSetup = false;
        private bool FingerprintWaiting = false;

        private bool pageIsBusy = false;
        public bool LoginIsBusy
        {
            get { return pageIsBusy; }
            set => SetLoginIsBusy(value);
        }

        private void SetLoginIsBusy(bool busy)
        {
            if (pageIsBusy == busy)
                return;

            pageIsBusy = busy;

            spinnerLogin.IsRunning = busy;
            spinnerLogin.IsEnabled = busy;
            spinnerLogin.IsVisible = busy;

            btnLogin.IsEnabled = !busy;
            btnLogin.BackgroundColor = busy ? AppStyle.ButtonDisabledBackgroundColor : AppStyle.ButtonBackgroundColor;
            btnLogin.TextColor = busy ? AppStyle.ButtonDisabledTextColor : AppStyle.ButtonTextColor;
        }

        public LoginPage ()
		{
			InitializeComponent ();

            Fingerprint = DependencyService.Get<IFingerprintAuth>();

            if (Device.RuntimePlatform == Device.UWP)
                NavigationPage.SetHasNavigationBar(this, false);
        }

        /// <summary>
        /// Mostra la pagina della privacy policy
        /// </summary>
        /// <param name="toBeAgree">Passare true per indicare che è richiesta l'accettazione
        /// della privacy policy</param>
        /// <returns></returns>
        async private Task ShowPrivacyPolicy(bool toBeAgree)
        {
            var data = new PrivacyPolicyData
            {
                ToBeAgree = toBeAgree,
                ShowNavigationBar = !toBeAgree
            };

            await Navigation.PushModalAsync(new PrivacyPolicy
            {
                BindingContext = data
            }, true);
        }

        /// <summary>
        /// Verifica le condizioni di installazione per sapere se è necessario effettuare
        /// il redirect su un altra pagina
        /// </summary>
        /// <returns></returns>
        async private Task CheckRedirect()
        {
            App.Logger.Debug("CheckRedirect...");
            LoginIsBusy = true;
            try
            {
                if (App.IsNotificationOpen() && App.PushNotificationData is NewsInfo newsData)
                {
                    App.Logger.Debug("Push notification is open for news");
                    await App.CurrentApp.ShowNews(newsData);
                }
                else if ((!App.PwdManager.IsInitialized() && !App.AgreePrivacyPolicy) || App.IsUpdated())
                {
                    App.Logger.Debug("Show privacy policy");
                    await ShowPrivacyPolicy(true);
                }
                else if (App.IsLoggedIn)
                {
                    App.Logger.Debug("Got to hamburger menu");
                    App.CurrentApp.ChangeMainPage(new HamburgerMenu());
                }
                else
                    App.Logger.Debug("CheckRedirect: no redirection");
            }
            catch(Exception e)
            {
                App.Logger.Error(string.Format("CheckRedirect error: {0}", e.Message));
                throw;
            }
            finally
            {
                LoginIsBusy = false;
            }
        }

        async protected override void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext != null && BindingContext is Utility.RedirectData data)
            {
                RedirectInfo = data;
                lblMessage.Text = data?.MessageText;
                lblMessage.IsVisible = !string.IsNullOrEmpty(lblMessage.Text);
                NavigationPage.SetHasNavigationBar(this, false);
            }
            else
            {
                if (App.IsAppearingTwice())
                {
                    // Workaround
                    // Se si aspetta anche solo un secondo, la OnAppearing non verrà chiamata
                    // una seconda volta.
                    await Task.Delay(1000);
                    await CheckRedirect();
                }
                else
                    await CheckRedirect();
            }

            CheckFingerprintAccess();
        }

        protected override void OnDisappearing()
        {
            if (Fingerprint != null && IsFingerprintSetup)
            {
                Fingerprint.OnAuthenticationSucceeded -= Fingerprint_OnAuthenticationSucceeded;
                Fingerprint.OnAuthenticationCanceled -= Fingerprint_OnAuthenticationCanceled;
                Fingerprint.OnAuthenticationError -= Fingerprint_OnAuthenticationError;
                Fingerprint.OnAuthenticationFailed -= Fingerprint_OnAuthenticationFailed;
                Fingerprint.OnAuthenticationHelp -= Fingerprint_OnAuthenticationHelp;
                IsFingerprintSetup = false;
            }
            base.OnDisappearing();
        }

        private void CheckFingerprintAccess()
        {
            if (Fingerprint != null && !IsFingerprintSetup)
            {
                Fingerprint.OnAuthenticationSucceeded += Fingerprint_OnAuthenticationSucceeded;
                Fingerprint.OnAuthenticationCanceled += Fingerprint_OnAuthenticationCanceled;
                Fingerprint.OnAuthenticationError += Fingerprint_OnAuthenticationError;
                Fingerprint.OnAuthenticationFailed += Fingerprint_OnAuthenticationFailed;
                Fingerprint.OnAuthenticationHelp += Fingerprint_OnAuthenticationHelp;
                IsFingerprintSetup = true;
            }

            stackFingerprint.IsVisible = false;
            try
            {
                if (App.Settings.Access == SecurityAccess.Fingerprint)
                {
                    if (App.PwdManager.IsFingerprintAccessConfigured())
                    {
                        stackFingerprint.IsVisible = true;
                        if (Fingerprint != null)
                        {
                            Fingerprint.Init();
                            byte[] data = App.PwdManager.ReadFingerprintAccessFile();
                            Fingerprint.Authenticate(data, FingerprintSignOp.Decrypt);
                            FingerprintWaiting = true;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                stackFingerprint.IsVisible = false;
                FingerprintWaiting = false;
                Debug.WriteLine("Accesso allo scanner per l'impronta digitale fallito. Errore: " + ex.Message);
            }
        }

        private void Fingerprint_OnAuthenticationHelp(int helpMsgId, string helpString)
        {
            lblFingerprintMsg.Text = string.Format(AppResources.txtFingerprintHelp, helpString);
            Debug.WriteLine(string.Format("Impronta: richiesta di aiuto con codice {0}: {1}", helpMsgId, helpString));
        }

        private void Fingerprint_OnAuthenticationFailed()
        {
            lblFingerprintMsg.Text = AppResources.txtFingerprintFailed;
            Debug.WriteLine("Impronta: autenticazione fallita");
        }

        private void Fingerprint_OnAuthenticationError(int errorMsgId, string errorMsg)
        {
            lblFingerprintMsg.Text = string.Format(AppResources.txtFingerprintError, errorMsg);
            Debug.WriteLine(string.Format("Impronta: errore durante la fase di autenticazione, codice {0}: {1}", errorMsgId, errorMsg));
        }

        private void Fingerprint_OnAuthenticationCanceled()
        {
            lblFingerprintMsg.Text = AppResources.txtFingerprintCanceled;
            Debug.WriteLine("Impronta: autenticazione cancellata dall'utente");
            FingerprintWaiting = false;
        }

        private async void Fingerprint_OnAuthenticationSucceeded(byte[] result)
        {
            lblFingerprintMsg.Text = AppResources.txtFingerprintSucceeded;
            Debug.WriteLine("Impronta: autenticazione riuscita");
            App.PwdManager.Password = Encoding.UTF8.GetString(result);
            await OpenPasswordListFile();
        }

        protected override bool OnBackButtonPressed()
        {
            if (RedirectInfo != null)
                return true;
            return base.OnBackButtonPressed();
        }

        private void ContentPage_SizeChanged(object sender, EventArgs e)
        {
            const double AutoWidth = -1;

            if (Width > App.ViewStepMaxWidth)
            {
                double fixedWidth = App.ViewStepMaxWidth - 140;

                entryPassword.HorizontalOptions = LayoutOptions.CenterAndExpand;
                entryPassword.WidthRequest = fixedWidth;

                if (Height < App.ViewStepMaxHeight)
                {
                    btnLogin.HorizontalOptions = LayoutOptions.FillAndExpand;
                    btnLogin.WidthRequest = AutoWidth;
                }
                else
                {
                    btnLogin.HorizontalOptions = LayoutOptions.CenterAndExpand;
                    btnLogin.WidthRequest = fixedWidth;
                }
            }
            else
            {
                entryPassword.HorizontalOptions = LayoutOptions.FillAndExpand;
                entryPassword.WidthRequest = AutoWidth;

                btnLogin.HorizontalOptions = LayoutOptions.FillAndExpand;
                btnLogin.WidthRequest = AutoWidth;
            }

            if (Height < App.ViewStepMaxHeight)
            {
                lblTitle.FontSize = Device.GetNamedSize(NamedSize.Medium, lblTitle.GetType());
                if (Width <= App.ViewStepMaxWidth && Height > App.ViewStepSmallMaxHeight)
                    entryPassword.Orientation = StackOrientation.Vertical;
                else
                    entryPassword.Orientation = StackOrientation.Horizontal;

                if (Device.RuntimePlatform == Device.Android)
                    entryPassword.FontSize = 14;
            }
            else
            {
                lblTitle.FontSize = Device.GetNamedSize(NamedSize.Large, lblTitle.GetType());
                entryPassword.Orientation = StackOrientation.Vertical;

                if (Device.RuntimePlatform == Device.Android)
                    entryPassword.FontSize = Device.GetNamedSize(NamedSize.Default, typeof(Entry));
            }
        }

        private async Task DoLogin2FA()
        {
            // Richiede il codice OTP
            TOTPPage page = new TOTPPage();
            page.OnSuccess += TOTP_OnSuccess;
            page.OnCancel += TOTP_OnCancel;
            await Navigation.PushModalAsync(page, true);                    
        }

        private async void TOTP_OnCancel(object sender, EventArgs e)
        {
            App.IsLoggedIn = false;
            entryPassword.Text = "";
            await DisplayAlert(App.Title, AppResources.msgTOTPCancelled, "Ok");
        }

        private async void TOTP_OnSuccess(object sender, EventArgs e)
        {
            App.IsLoggedIn = true;
            await CompletLogin();
            
            try
            {
                App.Sync2FASetting();
            }
            catch(Exception ex)
            {
                await DisplayAlert(App.Title, AppResources.errSettingsUpdate + " " + ex.Message, "OK");
            }            
        }

        private async Task DoLogin()
        {
            App.IsLoggedIn = true;
            if (Fingerprint != null && FingerprintWaiting)
                Fingerprint.Cancel();
            await CompletLogin();
        }

        private async Task CompletLogin()
        {
            await App.Statistic.RegisterLogin();
            // Mantiene aggiornati i file locali con la lista e le statistiche delle password su Cloud
            if (App.IsCloudEnabled() && RedirectInfo == null)
            {
                App.PwdManager.Cloud = true;
                try
                {
                    string pwdfileNameInCloud = App.PwdManager.PwdFileNameInCloud;
                    await App.CloudConnector.GetFile(pwdfileNameInCloud, App.PwdManager.GetPasswordFilePath());
                    await App.PwdManager.DownloadAttachmentFromCloud();
                    await App.CloudConnector.GetFile("appcloud.stat", App.Statistic.StatisticsFilePath);
                }
                catch (Exception Ex)
                {
                    Debug.WriteLine("Sync with Cloud error: " + Ex.Message);
                    await DisplayAlert(App.Title, AppResources.errCloudDownload + " " + Ex.Message, "Ok");
                }
                finally
                {
                    App.PwdManager.Cloud = false;
                }
            }

            if (RedirectInfo != null)
            {
                if (RedirectInfo.Modal)
                    await Navigation.PopModalAsync(true);
                else
                    await Navigation.PopAsync(true);
            }
            else
                App.CurrentApp.ChangeMainPage(new HamburgerMenu());
        }

        async private Task OpenPasswordListFile()
        {
            LoginIsBusy = true;
            try
            {
                ReadResult result = await App.PwdManager.OpenPasswordFile();
                if (result == ReadResult.Success || result == ReadResult.SuccessWithWarning)
                {
                    if (result == ReadResult.SuccessWithWarning)
                        await DisplayAlert(App.Title, AppResources.warnFileCorruptedButRecover, "Ok");

                    if (App.PwdManager.Is2FAAccessConfigured())
                        await DoLogin2FA();
                    else
                        await DoLogin();
                }
                else
                {
                    App.IsLoggedIn = false;
                    await DisplayAlert(App.Title, AppResources.errWrongPassword, "Ok");
                }
            }
            finally
            {
                LoginIsBusy = false;
            }
        }

        async private void BtnLogin_Clicked(object sender, EventArgs e)
        {
            if (entryPassword.Text == null || entryPassword.Text.Trim().Length == 0)
            {
                await DisplayAlert(App.Title, AppResources.errInvalidPassword, "Ok");
                return;
            }

            App.PwdManager.Password = entryPassword.Text;
            await OpenPasswordListFile();
        }
    }
}