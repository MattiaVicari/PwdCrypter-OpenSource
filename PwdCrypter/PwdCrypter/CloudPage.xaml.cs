using PwdCrypter.Cloud;
using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Zxcvbn;

namespace PwdCrypter
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CloudPage : ContentPage
    {
        private Utility.RedirectData RedirectInfo = null;

        public static Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }
        public static double LogoCloudWidth
        {
            get
            {
                if (Device.RuntimePlatform == Device.UWP)
                    return 200;
                return 150;
            }
        }
        
        private bool pageIsBusy = false;
        public bool CloudIsBusy
        {
            get { return pageIsBusy; }
            set => SetCloudIsBusy(value);
        }

        private void SetCloudIsBusy(bool busy)
        {
            if (pageIsBusy == busy)
                return;

            pageIsBusy = busy;

            spinnerCloud.IsRunning = busy;
            spinnerCloud.IsEnabled = busy;
            spinnerCloud.IsVisible = busy;

            btnOneDrive.IsEnabled = !busy;
            btnOneDrive.BackgroundColor = busy ? AppStyle.ButtonDisabledBackgroundColor : AppStyle.ButtonBackgroundColor;
            btnOneDrive.TextColor = busy ? AppStyle.ButtonDisabledTextColor : AppStyle.ButtonTextColor;

            btnGoogleDrive.IsEnabled = !busy;
            btnGoogleDrive.BackgroundColor = busy ? AppStyle.ButtonDisabledBackgroundColor : AppStyle.ButtonBackgroundColor;
            btnGoogleDrive.TextColor = busy ? AppStyle.ButtonDisabledTextColor : AppStyle.ButtonTextColor;

            switchRememberMe.IsEnabled = !busy;
            btnCancel.IsEnabled = !busy;
        }

        public CloudPage()
        {
            InitializeComponent();
        }

        private async Task LoginOnCloud(CloudPlatform platform)
        {
            if (App.Settings.CloudPlatform != platform && App.Settings.CloudPlatform != CloudPlatform.Unknown
                && App.CloudConnector.IsLoggedIn())
            {
                await DisplayAlert(App.Title, AppResources.msgLogoutCloudPlatform, "Ok");
                return;
            }

            App.CloudConnector.SetRememberMe(switchRememberMe.IsToggled);

            CloudIsBusy = true;
            try
            {
                if (App.CloudConnector.IsLoggedIn())
                {
                    App.Settings.CloudPlatform = CloudPlatform.Unknown;
                    App.CloudConnector.LoggedOut += CloudConnector_LoggedOut;
                    await App.CloudConnector.Logout();
                }
                else
                {
                    App.Settings.CloudPlatform = platform;
                    App.CloudConnector.LoggedIn += CloudConnector_LoggedIn;
                    await App.CloudConnector.Login();
                }
            }
            catch (Exception e)
            {
                App.CloudConnector.LoggedIn -= CloudConnector_LoggedIn;
                App.CloudConnector.LoggedOut -= CloudConnector_LoggedOut;
                App.PwdManager.Cloud = false;
                OperationCompleted();                
                await DisplayAlert(App.Title, e.Message, "Ok");
            }
        }

        private async void BtnOneDrive_OnClicked(object sender, EventArgs e)
        {
            // Login su OneDrive
            if (App.CloudConnector == null || (!App.CloudConnector.IsLoggedIn() && App.Settings.CloudPlatform != CloudPlatform.OneDrive))
                App.CloudConnector = new OneDriveConnector();
            await LoginOnCloud(CloudPlatform.OneDrive);
        }

        private void OperationCompleted()
        {
            CloudIsBusy = false;
            Device.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    App.Settings.WriteSettings();
                }
                catch(Exception e)
                {
                    await DisplayAlert(App.Title, AppResources.errSettingsUpdate + " " + e.Message, "OK");
                }
                UpdateStatus();
            });
        }

        private void CloudConnector_LoggedOut(object sender, EventArgs e)
        {
            App.CloudConnector.LoggedOut -= CloudConnector_LoggedOut;
            OperationCompleted();
            App.PwdManager.Cloud = false;
        }

        private async void CloudConnector_LoggedIn(object sender, EventArgs e)
        {
            bool uploadToCloud = false;
            App.CloudConnector.LoggedIn -= CloudConnector_LoggedIn;

            App.PwdManager.Cloud = false;
            if (!App.CloudConnector.IsLoggedIn())
            {
                OperationCompleted();
                await DisplayAlert(App.Title, AppResources.errCloudAuth + " " + App.CloudConnector.GetLastError(), "Ok");
                return;
            }

            // Scarica il file delle password che si trova su Cloud, se c'è.
            // Se non c'è, chiede se si vuole caricare la lista locale delle password su Cloud
            try
            {
                // Lista delle password
                string pwdfileNameInCloud = App.PwdManager.PwdFileNameInCloud;
                CloudFileStatus status = await App.CloudConnector.FileExists(pwdfileNameInCloud);
                if (status == CloudFileStatus.FileError)
                    await DisplayAlert(App.Title, AppResources.errCloudAccess + " " + App.CloudConnector.GetLastError(), "OK");
                else if (status == CloudFileStatus.FileNotFound)
                    uploadToCloud = await UploadToCloud();
                else if (status == CloudFileStatus.FileFound)
                {
                    App.PwdManager.Cloud = true;
                    try
                    {
                        if (await App.CloudConnector.GetFile(pwdfileNameInCloud, App.PwdManager.GetPasswordFilePath()) == CloudFileStatus.FileDownloaded)
                        {
                            uploadToCloud = true;
                            // Scarica gli allegati
                            await App.PwdManager.DownloadAttachmentFromCloud();
                        }
                        else
                            App.PwdManager.Cloud = false;
                    }
                    catch(Exception ex)
                    {
                        await DisplayAlert(App.Title, ex.Message, "OK");
                        App.PwdManager.Cloud = false;
                    }
                }

                // Statistiche
                if (uploadToCloud)
                {
                    try
                    {
                        status = await App.CloudConnector.FileExists("appcloud.stat");
                        if (status == CloudFileStatus.FileError)
                            throw new Exception(AppResources.errCloudAccess + " " + App.CloudConnector.GetLastError());                            
                        else if (status == CloudFileStatus.FileNotFound && File.Exists(Path.Combine(App.Statistic.StatisticsFolder, "appcloud.stat")))
                            await UploadStatisticsToCloud();
                        else if (status == CloudFileStatus.FileFound)
                            await App.CloudConnector.GetFile("appcloud.stat", App.Statistic.StatisticsFilePath);
                    }
                    catch(Exception ex)
                    {
                        await DisplayAlert(App.Title, ex.Message, "OK");
                    }

                    // Verifica l'accesso
                    await CheckSecurityAccess();
                }
            }
            catch (Exception Ex)
            {
                await DisplayAlert(App.Title, AppResources.errCloudAccess + " " + Ex.Message, "OK");
            }
            finally
            {
                OperationCompleted();
            }
        }

        private async Task CloseIfRedirect()
        {
            if (RedirectInfo != null)
            {
                if (RedirectInfo.Modal)
                    await Navigation.PopModalAsync(true);
                else
                    await Navigation.PopAsync(true);
            }
        }

        private async Task CheckSecurityAccess()
        {
            try
            {
                // Apertura della lista delle password
                ReadResult result = await App.PwdManager.OpenPasswordFile();
                if (result == ReadResult.Success || result == ReadResult.SuccessWithWarning)
                {
                    if (result == ReadResult.SuccessWithWarning)
                        await DisplayAlert(App.Title, AppResources.warnFileCorruptedButRecover, "Ok");
                    if (App.PwdManager.Is2FAAccessConfigured())
                        await DoLogin2FA();
                    else
                    {
                        await App.Statistic.SyncStatistics();
                        await CloseIfRedirect();
                    }
                }
                else
                {
                    App.PwdManager.Cloud = true;    // Voglio essere sicuro di cancellare la copia del file sul Cloud
                    File.Delete(App.PwdManager.GetPasswordFilePath());
                    App.PwdManager.Cloud = false;
                    await DisplayAlert(App.Title, AppResources.errWrongPassword, "Ok");
                    await CloseIfRedirect();
                }
            }
            catch(Exception)
            {
                App.PwdManager.Cloud = false;
                throw;
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
            App.PwdManager.DeleteLocalFileFromCloud();                
            App.PwdManager.Cloud = false;
            
            await DisplayAlert(App.Title, AppResources.msgTOTPCancelled, "Ok");
        }

        private async void TOTP_OnSuccess(object sender, EventArgs e)
        {
            await App.Statistic.SyncStatistics();
            await CloseIfRedirect();
        }

        private async Task<bool> UploadStatisticsToCloud()
        {
            try
            {
                string localFileName = App.Statistic.StatisticsLocalFilePath;

                if (!await App.CloudConnector.UploadFile(localFileName, "appcloud.stat"))
                    throw new Exception("Upload failed");

                // Tengo in locale una copia del file su cloud
                string cloudFileName = App.Statistic.StatisticsCloudFilePath;
                File.Copy(localFileName, cloudFileName, true);
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("UploadToCloud error: " + Ex.Message);
                throw;
            }

            return true;
        }

        private async Task<bool> UploadToCloud()
        {
            bool res = await DisplayAlert(App.Title, AppResources.msgUploadToCloud, AppResources.btnYes, AppResources.btnNo);
            if (!res)
                return false;

            await App.PwdManager.UploadPwdToCloud();
            return true;
        }

        private void UpdateStatus()
        {
            lblCloudStatus.Text = AppResources.txtCloudYouAreDisconnected;
            btnOneDrive.Text = "Login";
            btnGoogleDrive.Text = "Login";
            stackRememberMe.IsVisible = true;
            if (App.CloudConnector != null && App.CloudConnector.IsLoggedIn())
            {
                lblCloudStatus.Text = string.Format(AppResources.txtCloudYouAreConnected, App.GetCurrentCloudPlatformName());
                stackRememberMe.IsVisible = false;

                if (App.Settings.CloudPlatform == CloudPlatform.OneDrive)
                    btnOneDrive.Text = "Logout";
                else if (App.Settings.CloudPlatform == CloudPlatform.GoogleDrive)
                    btnGoogleDrive.Text = "Logout";
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            App.PwdSecurity.BeginOperation();

            if (BindingContext != null && BindingContext is Utility.RedirectData data)
                RedirectInfo = data;

            btnCancel.IsVisible = (RedirectInfo != null && RedirectInfo.Modal);
            CloudIsBusy = false;
            UpdateStatus();
        }

        private async void BtnCancel_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync(true);
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

        private async void BtnGoogleDrive_Clicked(object sender, EventArgs e)
        {
            // Login su GoogleDrive
            if (App.CloudConnector == null || (!App.CloudConnector.IsLoggedIn() && App.Settings.CloudPlatform != CloudPlatform.GoogleDrive))
                App.CloudConnector = new GoogleDriveConnector();
            await LoginOnCloud(CloudPlatform.GoogleDrive);
        }
    }
}