using PwdCrypter.Controls;
using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChangeLoginPage : ContentPage
    {
        static public Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }

        public ChangeLoginPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            App.PwdSecurity.BeginOperation();
        }

        private void ContentPage_SizeChanged(object sender, EventArgs e)
        {
            const double AutoWidth = -1;

            View []views = {
                entryOldPassword,
                entryNewPassword,
                entryRepeatPassword,
                btnConfirm
            };

            LayoutOptions layoutOptions;
            LayoutOptions btnLayoutOptions;
            double width;
            double btnWidth;
            if (Width > App.ViewStepMaxWidth)
            {
                double fixedWidth = App.ViewStepMaxWidth - 140;

                layoutOptions = LayoutOptions.CenterAndExpand;
                width = fixedWidth;

                if (Height < App.ViewStepMaxHeight)
                {
                    btnLayoutOptions = LayoutOptions.FillAndExpand;
                    btnWidth = AutoWidth;
                }
                else
                {
                    btnLayoutOptions = LayoutOptions.CenterAndExpand;
                    btnWidth = fixedWidth;
                }
            }
            else
            {
                layoutOptions = LayoutOptions.FillAndExpand;
                width = AutoWidth;

                btnLayoutOptions = LayoutOptions.FillAndExpand;
                btnWidth = AutoWidth;
            }

            StackOrientation stackOrientation;
            double AndroidEntryFontSize;
            GridLength heightMainGrid;
            GridLength heightPwdGrid;
            GridLength heightNewPwdGrid;
            if (Height < App.ViewStepMaxHeight)
            {
                lblTitle.FontSize = Device.GetNamedSize(NamedSize.Medium, lblTitle.GetType());
                if (Width <= App.ViewStepMaxWidth && Height > App.ViewStepSmallMaxHeight)
                {
                    stackOrientation = StackOrientation.Vertical;
                }
                else
                {
                    stackOrientation = StackOrientation.Horizontal;
                }

                heightMainGrid = GridLength.Auto;
                if (Width <= App.ViewStepMaxWidth && Height > App.ViewStepSmallMaxHeight)
                {
                    heightPwdGrid = GridLength.Star;
                    heightNewPwdGrid = GridLength.Auto;
                }
                else
                {
                    heightPwdGrid = new GridLength(40, GridUnitType.Absolute);
                    heightNewPwdGrid = new GridLength(110, GridUnitType.Absolute);
                }

                AndroidEntryFontSize = 14;
            }
            else
            {
                lblTitle.FontSize = Device.GetNamedSize(NamedSize.Large, lblTitle.GetType());
                stackOrientation = StackOrientation.Vertical;

                heightMainGrid = GridLength.Star;
                heightPwdGrid = GridLength.Star;
                heightNewPwdGrid = GridLength.Auto;

                AndroidEntryFontSize = Device.GetNamedSize(NamedSize.Default, typeof(Entry));
            }

            // View
            foreach (View view in views)
            {
                if (view is FormEntry entry)
                {
                    entry.HorizontalOptions = layoutOptions;
                    entry.WidthRequest = width;
                    entry.Orientation = stackOrientation;
                    if (Device.RuntimePlatform == Device.Android)
                        entry.FontSize = AndroidEntryFontSize;
                }
                else if (view is Button button)
                {
                    button.HorizontalOptions = btnLayoutOptions;
                    button.WidthRequest = btnWidth;
                }
            }

            // Griglie
            gridMain.RowDefinitions[2].Height = heightMainGrid;
            for (int row = 0; row < gridPassword.RowDefinitions.Count; row++)
            {
                if (row == 1)
                    gridPassword.RowDefinitions[row].Height = heightNewPwdGrid;
                else
                    gridPassword.RowDefinitions[row].Height = heightPwdGrid;
            }
        }

        private bool CheckExpireUnlockCode()
        {
            try
            {
                ProductCache product = App.Cache.GetProduct(BillingManager.PLUSProductID);
                if (product != null)
                {
                    if (!product.Purchased && product.Unlocked && product.UnlockCodeExpire
                        && !string.IsNullOrEmpty(product.UnlockCode))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch(Exception ex)
            {
                Debug.WriteLine("CheckExpireUnlockCode error: " + ex.Message);
            }

            return false;
        }

        private async Task<bool> CheckPersistentUnlockCode(string password)
        {
            try
            {
                ProductCache product = App.Cache.GetProduct(BillingManager.PLUSProductID);
                if (product != null)
                {
                    if (!product.Purchased && product.Unlocked && !product.UnlockCodeExpire
                        && !string.IsNullOrEmpty(product.UnlockCode))
                    {
                        // Genera il nuovo codice e aggiorna il file di cache
                        product.UnlockCode = App.Billing.CreatePersistentUnlockCode(BillingManager.PLUSProductID, password);
                        await App.Cache.WriteCache();

                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("CheckPersistentUnlockCode error: " + ex.Message);
            }
            return false;
        }

        private async void BtnConfirm_Clicked(object sender, EventArgs e)
        {
            if (!await ValidateData())
                return;

            spinnerLogin.IsRunning = true;
            spinnerLogin.IsEnabled = true;
            spinnerLogin.IsVisible = true;
            btnConfirm.IsEnabled = false;
            App.Statistic.Freeze = true;

            try
            {
                // Verifica se si sta utilizzando un codice di sblocco con scadenza
                if (CheckExpireUnlockCode())
                {
                    if (!await DisplayAlert(App.Title, AppResources.warnExpireUnlockCode, AppResources.btnYes, AppResources.btnNo))
                        return;
                }

                string oldPassword = entryOldPassword.Text;
                string newPassword = entryNewPassword.Text.Trim();

                // Verifica la presenza del file su Cloud
                bool updateCloud = false;
                bool cloudFileExists = App.IsCloudAvailable && App.PwdManager.CloudFileExists();
                if (cloudFileExists)
                {
                    // Chiede se si vuole aggiornare anche il file su Cloud
                    if (await DisplayAlert(App.Title, AppResources.msgQuestionCloudChangeLogin, AppResources.btnYes, AppResources.btnNo))
                    {
                        updateCloud = true;

                        // Se non si è collegati, chiede di collegarsi al Cloud
                        if (!App.IsCloudEnabled())
                        {
                            if (await DisplayAlert(App.Title, AppResources.msgQuestionCloudConn, AppResources.btnYes, AppResources.btnNo))
                            {
                                await Navigation.PushModalAsync(new CloudPage
                                {
                                    BindingContext = new Utility.RedirectData
                                    {
                                        Modal = true,
                                        RedirectTo = this
                                    }
                                }, true);
                                return;
                            }
                            else
                                updateCloud = false;
                            // ... allora non sarà possibile aggiornare i file su Cloud
                        }
                    }
                }

                WaitPage waitPage = await Utility.StartWait(this);
                try
                {
                    if (cloudFileExists && updateCloud)
                    {
                        // Aggiorna i file del cloud locali
                        try
                        {
                            App.PwdManager.Cloud = true;
                            ReadResult resultUpdate = await App.PwdManager.UpdateLogin(oldPassword, newPassword);
                            if (resultUpdate == ReadResult.SuccessWithWarning)
                                await DisplayAlert(App.Title, AppResources.warnFileCorruptedButRecover, "Ok");
                        }
                        finally
                        {
                            App.PwdManager.Cloud = false;
                        }
                    }
                    else if (cloudFileExists)
                    {
                        // Faccio comunque un backup dei file locali del cloud
                        try
                        {
                            App.PwdManager.Cloud = true;
                            App.PwdManager.Backup();
                        }
                        finally
                        {
                            App.PwdManager.Cloud = false;
                        }
                    }

                    // Aggiornare file locali
                    App.PwdManager.Cloud = false;
                    ReadResult result = await App.PwdManager.UpdateLogin(oldPassword, newPassword);
                    if (result == ReadResult.SuccessWithWarning)
                        await DisplayAlert(App.Title, AppResources.warnFileCorruptedButRecover, "Ok");

                    // Verifica se c'è un codice di sblocco senza scadenza da aggioranare
                    if (await CheckPersistentUnlockCode(newPassword))
                        await DisplayAlert(App.Title, AppResources.warnPersistentUnlockCode, "Ok");

                    await DisplayAlert(App.Title, AppResources.msgChangeLoginDone, "Ok");

                    // Avvisa l'utente che va riconfigurato l'accesso con impronta digitale
                    if (App.PwdManager.IsFingerprintAccessConfigured())
                    {
                        // Elimina il file
                        App.PwdManager.RemoveFingerprintAccessFile();
                        // Avvisa l'utente
                        await DisplayAlert(App.Title, AppResources.msgFingerprintReconfigure, "Ok");
                    }
                }
                finally
                {
                    await Utility.StopWait(waitPage);
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("Unable to complete the procedure. Error: " + Ex.Message);
                await DisplayAlert(App.Title, AppResources.errChangePassword + " " + Ex.Message, "Ok");
            }
            finally
            {
                spinnerLogin.IsRunning = false;
                spinnerLogin.IsEnabled = false;
                spinnerLogin.IsVisible = false;
                btnConfirm.IsEnabled = true;
                App.Statistic.Freeze = false;
            }

            // Invia un messaggio al menu hamburger per visualizzare la pagina di riepilogo
            MessagingCenter.Send<Page, Type>(this, "NavigateTo", typeof(WelcomePage));
        }

        private async Task<bool> ValidateData()
        {
            if (string.IsNullOrWhiteSpace(entryOldPassword.Text))
            {
                await DisplayAlert(App.Title, AppResources.errOldPassword, "Ok");
                return false;
            }

            if (string.IsNullOrWhiteSpace(entryNewPassword.Text))
            {
                await DisplayAlert(App.Title, AppResources.errNewPassword, "Ok");
                return false;
            }

            if (string.IsNullOrWhiteSpace(entryRepeatPassword.Text))
            {
                await DisplayAlert(App.Title, AppResources.errRepeatPassword, "Ok");
                return false;
            }

            string oldPassword = entryOldPassword.Text;
            string newPassword = entryNewPassword.Text.Trim();
            string repeatPassword = entryRepeatPassword.Text.Trim();

            if (oldPassword.CompareTo(newPassword) == 0)
            {
                await DisplayAlert(App.Title, AppResources.errSamePassword, "Ok");
                return false;
            }
            if (newPassword.CompareTo(repeatPassword) != 0)
            {
                await DisplayAlert(App.Title, AppResources.errCheckPassword, "Ok");
                return false;
            }

            if (entryNewPassword.PasswordStrength < 2)
            {
                if (!await DisplayAlert(App.Title, AppResources.warnWeakPassword, AppResources.btnYes, AppResources.btnNo))
                    return false;
            }

            return true;
        }

        protected override bool OnBackButtonPressed()
        {
            return true;
        }
    }
}