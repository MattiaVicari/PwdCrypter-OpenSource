using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FingerprintPage : ContentPage
    {
        const int MESSAGE_DELAY = 1000;

        public static Dictionary<string, string> ImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#000000" }, { "currentOpacity", "1.0" } };
        }

        private readonly IFingerprintAuth Fingerprint = null;
        private bool IsFingerprintSetup = false;

        public FingerprintPage()
        {
            InitializeComponent();
            Fingerprint = DependencyService.Get<IFingerprintAuth>();
        }

        private void BtnCancel_OnClicked(object sender, EventArgs e)
        {
            Fingerprint.Cancel();
        }

        private void DoAuthenticate()
        {
            lblMessage.Text = AppResources.txtFingerprintReady;
            try
            {
                if (Fingerprint == null)
                    throw new Exception("Service not available");

                byte[] secret = Encoding.UTF8.GetBytes(App.PwdManager.Password);
                Fingerprint.Init();
                Fingerprint.Authenticate(secret, FingerprintSignOp.Crypt);
            }
            catch (Exception ex)
            {
                DisplayAlert(App.Title, string.Format(AppResources.errConfigFingerprint, ex.Message), "Ok");
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (Fingerprint != null && !IsFingerprintSetup)
            {
                Fingerprint.OnAuthenticationSucceeded += Fingerprint_OnAuthenticationSucceeded;
                Fingerprint.OnAuthenticationCanceled += Fingerprint_OnAuthenticationCanceled;
                Fingerprint.OnAuthenticationError += Fingerprint_OnAuthenticationError;
                Fingerprint.OnAuthenticationFailed += Fingerprint_OnAuthenticationFailed;
                Fingerprint.OnAuthenticationHelp += Fingerprint_OnAuthenticationHelp;
                IsFingerprintSetup = true;
            }
            DoAuthenticate();
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

        private async void Fingerprint_OnAuthenticationSucceeded(byte[] result)
        {
            lblMessage.Text = AppResources.txtFingerprintSucceeded;
            Debug.WriteLine("Impronta: autenticazione riuscita");

            try
            {
                // Genera il file che identifica l'accesso
                App.PwdManager.SaveFingerprintAccessFile(result);
            }
            catch (Exception e)
            {
                await DisplayAlert(App.Title, string.Format(AppResources.errConfigFingerprint, e.Message), "Ok");
            }
            finally
            {
                await Task.Delay(MESSAGE_DELAY);
                await Navigation.PopModalAsync(true);
            }
        }
        private void Fingerprint_OnAuthenticationHelp(int helpMsgId, string helpString)
        {
            lblMessage.Text = string.Format(AppResources.txtFingerprintHelp, helpString);
            Debug.WriteLine(string.Format("Impronta: richiesta di aiuto con codice {0}: {1}", helpMsgId, helpString));
        }

        private void Fingerprint_OnAuthenticationFailed()
        {
            lblMessage.Text = AppResources.txtFingerprintFailed;
            Debug.WriteLine("Impronta: autenticazione fallita");
        }

        private async void Fingerprint_OnAuthenticationError(int errorMsgId, string errorMsg)
        {
            lblMessage.Text = string.Format(AppResources.txtFingerprintError, errorMsg);
            Debug.WriteLine(string.Format("Impronta: errore durante la fase di autenticazione, codice {0}: {1}", errorMsgId, errorMsg));
            if (errorMsgId == 7)  // Troppi tentativi. Riprovare più tardi
            {
                await Task.Delay(MESSAGE_DELAY);
                await Navigation.PopModalAsync(true);
            }
        }

        private async void Fingerprint_OnAuthenticationCanceled()
        {
            lblMessage.Text = AppResources.txtFingerprintCanceled;
            Debug.WriteLine("Impronta: autenticazione cancellata dall'utente");

            await Task.Delay(MESSAGE_DELAY);
            await Navigation.PopModalAsync(true);
        }
    }
}