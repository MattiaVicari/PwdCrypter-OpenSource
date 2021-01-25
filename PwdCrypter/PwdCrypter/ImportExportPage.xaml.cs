using Plugin.FilePicker;
using PwdCrypter.Cloud;
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
    public partial class ImportExportPage : ContentPage
    {
        public static Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }

        private readonly AppState CurrentState = new AppState();
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

            stackOperations.IsVisible = !busy;
        }

        public ImportExportPage()
        {
            InitializeComponent();
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();
                
            CloudIsBusy = true;
            await App.CurrentApp.CheckPurchasedItem(true, () => {
                CloudIsBusy = false;

                btnDownloadFromCloud.IsVisible = App.IsCloudAvailable;
                btnUploadToCloud.IsVisible = App.IsCloudAvailable;
                return true;
            });

            App.PwdSecurity.BeginOperation();
        }

        private void ContentPage_SizeChanged(object sender, EventArgs e)
        {
            const double AutoWidth = -1;
            LayoutOptions layoutOptions;
            double widthRequest;

            if (Width > App.ViewStepMaxWidth)
            {
                widthRequest = App.ViewStepMaxWidth - 140;
                layoutOptions = LayoutOptions.Center;
            }
            else
            {
                widthRequest = AutoWidth;
                layoutOptions = LayoutOptions.FillAndExpand;
            }

            gridMain.HorizontalOptions = layoutOptions;
            gridMain.WidthRequest = widthRequest;
        }

        private async void BtnImport_OnClicked(object sender, EventArgs e)
        {
            if (!await App.PwdSecurity.CheckOperation(this, "Import"))
                return;            

            // Chiede conferma all'utente
            if (!await DisplayAlert(App.Title, AppResources.msgQuestionPwdImport, AppResources.btnYes, AppResources.btnNo))
                return;

            ReadResult result = ReadResult.Failed;
            WaitPage wait = await Utility.StartWait(this);
            try
            {
                List<string> typeList = new List<string>();
                Dictionary<string, ImportExportFileFormat> extList = new Dictionary<string, ImportExportFileFormat>();
                ICrossPlatformSpecialFolder crossPlatformSpecialFolder = DependencyService.Get<ICrossPlatformSpecialFolder>();
                foreach (ImportExportFileFormat fileFormat in Utility.EnumHelper.GetValues<ImportExportFileFormat>())
                {
                    typeList.Add(crossPlatformSpecialFolder.GetMIMEType("." + fileFormat.ToString()));
                    extList.Add("." + fileFormat.ToString(), fileFormat);
                }

                // Chiede il file da importare
                Plugin.FilePicker.Abstractions.FileData FileData = await CrossFilePicker.Current.PickFile(typeList.ToArray());
                if (FileData == null)
                {
                    Debug.WriteLine("Import file picker cancelled by user");
                    await DisplayAlert(App.Title, AppResources.msgPwdImportCancelled, "Ok");
                    return;
                }
                
                CurrentState.Save();
                App.Statistic.Freeze = true;
                App.PwdManager.Cloud = false;
                try
                {
                    // Per aggiornare la lista delle password in base al flag del Cloud
                    await App.PwdManager.OpenPasswordFile();
                    await App.PwdManager.GetPasswordList();

                    result = await Task.Run(async () =>
                    {
                        string selectedFileExt = System.IO.Path.GetExtension(FileData.FileName);
                        ImportExportFileFormat CurrentFileFormat = extList.First((item) => { return item.Key.ToLower().CompareTo(selectedFileExt.ToLower()) == 0; }).Value;

                        long count = FileData.GetStream().Length;
                        byte[] data = new byte[count];
                        FileData.GetStream().Position = 0;
                        await FileData.GetStream().ReadAsync(data, 0, (int)count);

                        if (CurrentFileFormat != ImportExportFileFormat.ZIP)    // Sanifica i file non binari                                             
                            return await App.PwdManager.ImportPasswordListFromBytes(Utility.SanitizeData(data), CurrentFileFormat);                        
                        else
                            return await App.PwdManager.ImportPasswordListFromBytes(data, CurrentFileFormat);
                    });

                    if (result == ReadResult.SuccessWithWarning)
                        await DisplayAlert(App.Title, AppResources.warnFileCorruptedButRecover, "Ok");
                }
                catch(Exception)
                {
                    await CurrentState.Restore();                    
                    throw;
                }                
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("Error occurred during the import process. Error: " + Ex.Message);
                await DisplayAlert(App.Title, AppResources.errImportPwdList + " " + Ex.Message, "Ok");
            }
            finally
            {
                await Utility.StopWait(wait);
            }

            if (result != ReadResult.Failed)
                await CheckSecurityAccess();
        }

        private async Task CheckSecurityAccess()
        {
            try
            {
                if (App.PwdManager.Is2FAAccessConfigured())
                    await DoLogin2FA();
                else
                {
                    await App.PwdManager.SavePasswordFile();
                    CurrentState.Backup.DeleteBackup();
                    await CurrentState.Restore(false);
                    await DisplayAlert(App.Title, AppResources.msgPwdImportDone, "Ok");
                }
            }
            catch(Exception)
            {
                await CurrentState.Restore();
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
            await CurrentState.Restore();
        }

        private async void TOTP_OnSuccess(object sender, EventArgs e)
        {
            await App.PwdManager.SavePasswordFile();
            CurrentState.Backup.DeleteBackup();
            await CurrentState.Restore(false);
            await DisplayAlert(App.Title, AppResources.msgPwdImportDone, "Ok");

            try
            {
                App.Sync2FASetting();
            }
            catch (Exception ex)
            {
                await DisplayAlert(App.Title, AppResources.errSettingsUpdate + " " + ex.Message, "OK");
            }
        }

        private async void BtnExport_OnClicked(object sender, EventArgs e)
        {
            if (!await App.PwdSecurity.CheckOperation(this, "Export"))
                return;
            await Navigation.PushAsync(new ExportPage(), true);
        }

        private async void BtnDownloadFromCloud_OnClicked(object sender, EventArgs e)
        {
            if (!App.IsCloudAvailable)
                return;

            if (!await App.PwdSecurity.CheckOperation(this, "Download"))
                return;

            WaitPage wait = await Utility.StartWait(this);
            try
            {
                if (!App.IsCloudEnabled())
                {
                    if (await DisplayAlert(App.Title, AppResources.msgQuestionCloudNoConn, AppResources.btnYes, AppResources.btnNo))
                    {
                        await Utility.StopWait(wait);
                        await Navigation.PushModalAsync(new CloudPage
                        {
                            BindingContext = new Utility.RedirectData
                            {
                                Modal = true,
                                RedirectTo = this
                            }
                        }, true);
                    }
                    return;
                }

                // Chiede conferma all'utente
                if (!await DisplayAlert(App.Title, AppResources.msgQuestionPwdDonwload, AppResources.btnYes, AppResources.btnNo))
                    return;

                bool oldCloud = App.PwdManager.Cloud;
                App.PwdManager.Cloud = false;
                try
                {
                    BackupManager backup = App.PwdManager.Backup();
                    try
                    {
                        string pwdfileNameInCloud = App.PwdManager.PwdFileNameInCloud;
                        CloudFileStatus status = await App.CloudConnector.FileExists(pwdfileNameInCloud);
                        if (status == CloudFileStatus.FileError)
                            throw new Exception(AppResources.errCloudAccess + " " + App.CloudConnector.GetLastError());
                        else if (status == CloudFileStatus.FileNotFound)
                            throw new Exception(AppResources.errCloudFileNotFound);
                        else if (status == CloudFileStatus.FileFound)
                        {
                            CloudFileStatus downloadStatus = await App.CloudConnector.GetFile(pwdfileNameInCloud, App.PwdManager.GetPasswordFilePath());
                            if (downloadStatus == CloudFileStatus.FileDownloaded)
                            {
                                // Scarica gli allegati
                                await App.PwdManager.DownloadAttachmentFromCloud();

                                backup.DeleteBackup();
                                await DisplayAlert(App.Title, AppResources.msgPwdDownloadDone, "Ok");

                                // Verifica se devo aggiornare l'impostazione di accesso
                                ReadResult result = await App.PwdManager.OpenPasswordFile();
                                if (result != ReadResult.Failed)
                                {
                                    try
                                    {
                                        App.Sync2FASetting();
                                    }
                                    catch (Exception ex)
                                    {
                                        await DisplayAlert(App.Title, AppResources.errSettingsUpdate + " " + ex.Message, "OK");
                                    }                                    
                                }
                            }
                            else
                                throw new Exception("Unable to download the file. Status: " + downloadStatus.ToString());
                        }
                        else
                            throw new Exception("Inconsistent status: " + status.ToString());   // Molto strano... stato infondato XD
                    }
                    catch (Exception Ex)
                    {
                        Debug.WriteLine("BtnDownloadFromCloud_OnClicked: error occurred during the donwload from cloud. Error: " + Ex.Message);
                        backup.Restore();
                        await DisplayAlert(App.Title, AppResources.errCloudDownload + " " + Ex.Message, "OK");
                    }
                }
                finally
                {
                    App.PwdManager.Cloud = oldCloud;
                }
            }
            finally
            {
                await Utility.StopWait(wait);
            }
        }

        private async void BtnUploadToCloud_OnClicked(object sender, EventArgs e)
        {
            if (!App.IsCloudAvailable)
                return;

            if (!await App.PwdSecurity.CheckOperation(this, "Upload"))
                return;

            WaitPage wait = await Utility.StartWait(this);
            try
            {
                if (!App.IsCloudEnabled())
                {
                    if (await DisplayAlert(App.Title, AppResources.msgQuestionCloudNoConn, AppResources.btnYes, AppResources.btnNo))
                    {
                        await Utility.StopWait(wait);
                        await Navigation.PushModalAsync(new CloudPage
                        {
                            BindingContext = new Utility.RedirectData
                            {
                                Modal = true,
                                RedirectTo = this
                            }
                        }, true);
                    }
                    return;
                }

                // Chiede conferma all'utente
                if (!await DisplayAlert(App.Title, AppResources.msgQuestionPwdUpload, AppResources.btnYes, AppResources.btnNo))
                    return;

                try
                {
                    await App.PwdManager.UploadPwdToCloud();
                    await DisplayAlert(App.Title, AppResources.msgPwdUploadDone, "Ok");
                }
                catch (Exception Ex)
                {
                    Debug.WriteLine("BtnUploadToCloud_OnClicked: error occurred during the upload to the cloud. Error: " + Ex.Message);
                    await DisplayAlert(App.Title, AppResources.errCloudUpload + " " + Ex.Message, "OK");
                }
            }
            finally
            {
                await Utility.StopWait(wait);
            }
        }

        /// <summary>
        /// Classe che rappresenta lo stato dell'applicazione
        /// </summary>
        private class AppState
        {
            public bool Cloud { get; set; }
            public bool StatisticFreeze { get; set; }
            public BackupManager Backup { get; set; }

            public void Save()
            {
                Backup = App.PwdManager.Backup();
                Cloud = App.PwdManager.Cloud;
                StatisticFreeze = App.Statistic.Freeze;
            }

            public async Task Restore(bool includeBackup = true)
            {
                if (includeBackup)
                    Backup.Restore();
                App.PwdManager.Cloud = Cloud;
                App.Statistic.Freeze = StatisticFreeze;
                await App.PwdManager.OpenPasswordFile();
                await App.Statistic.GatherStatistics();
            }
        }
    }
}