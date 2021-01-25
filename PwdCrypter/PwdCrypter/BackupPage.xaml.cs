using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;
using PwdCrypter.Cloud;
using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BackupPage : ContentPage
    {
        const string CloudBackupFolder = "backups";

        public static Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }

        private readonly BackupManager Backup;
        private bool CurrentCloud = false;
        private WaitPage _currentWaitPage = null;

        private async Task StartWait()
        {
            _currentWaitPage = await Utility.StartWait(this);
        }
        private async Task StopWait()
        {
            await Utility.StopWait(_currentWaitPage);
            _currentWaitPage = null;
        }

        public BackupPage()
        {
            InitializeComponent();
            Backup = new BackupManager();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateBackupInfo();
        }

        private void UpdateBackupInfo()
        {
            try
            {
                BackupManager.CacheInfo info = Backup.GetCacheInfo();                

                labelLastBackup.IsVisible = info.LastBackupDate.HasValue;
                if (info.LastBackupDate.HasValue)
                    labelLastBackup.Text = string.Format(AppResources.txtLastBackup, info.LastBackupDate.Value.ToString("dd-MM-yyyy, HH:mm:ss"));
                btnRestore.IsVisible = info.LastBackupDate.HasValue;

                // Data prossima esecuzione. Utilizzo il dato presente in cache oppure quello restituisco
                // dal Background Task Manager
                DateTime nextDate;
                if (info.NextBackupDate.HasValue)
                    nextDate = info.NextBackupDate.Value;
                else
                {
                    IBackgroundTaskManager taskManager = DependencyService.Get<IBackgroundTaskManager>();
                    nextDate = taskManager.GetNextExecutionDate(App.BackupBackgroundTaskName);
                }
                if (nextDate > DateTime.MinValue)
                {
                    labelNextBackup.IsVisible = true;
                    labelNextBackup.Text = string.Format(AppResources.txtNextBackup, nextDate.ToString("dd-MM-yyyy, HH:mm:ss"));                    
                }
                else                
                    labelNextBackup.IsVisible = false;                
            }
            catch(Exception e)
            {
                labelLastBackup.IsVisible = false;
                labelNextBackup.IsVisible = false;
                btnRestore.IsVisible = true;    // Per sicurezza, per permettere all'utente di provare a fare un ripristino 
                                                // anche in caso di errore
                DisplayAlert(App.Title, e.Message, "Ok");
            }
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

        private async void BtnBackup_Clicked(object sender, EventArgs e)
        {
            if (App.IsCloudAvailable)
            {
                // Apre la pagina per selezionare la locazione del backup
                BackupLocationPage page = new BackupLocationPage();
                page.OnSelection += BackupLocation_OnSelection;
                await Navigation.PushModalAsync(page, true);
            }
            else
            {
                // Backup in locale
                await DoBackup();
            }            
        }

        private async void BackupLocation_OnSelection(object sender, BackupLocation location)
        {
            switch (location)
            {
                case BackupLocation.Local:
                    await DoBackup();
                    break;
                case BackupLocation.Cloud:
                    await DoBackupToCloud();
                    break;
                default:
                    {
                        Debug.WriteLine("Invalid location " + location.ToString());
                        await DisplayAlert(App.Title, string.Format(AppResources.errBackup, "Invalid location " + location.ToString()), "Ok");
                        break;
                    }
            }
        }

        private async Task DoBackupToCloud()
        {
            if (!App.IsCloudAvailable)
                return;

            if (!App.IsCloudEnabled())
            {
                if (await DisplayAlert(App.Title, AppResources.msgQuestionCloudNoConn, AppResources.btnYes, AppResources.btnNo))
                {
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
            
            await StartWait();
            try
            {
                bool bBackupSuccess = false;
                CurrentCloud = App.PwdManager.Cloud;
                App.PwdManager.Cloud = false;

                // Backup su Cloud
                try
                {
                    Backup.AddFile(App.PwdManager.GetPasswordFilePath());
                    Backup.AddFolder(App.PwdManager.GetAttachmentFolderPath());

                    ICrossPlatformSpecialFolder specialFolder = DependencyService.Get<ICrossPlatformSpecialFolder>();
                    string tempFolder = specialFolder.GetTemporaryFolder();
                    string fileName = "backup_" + DateTime.Now.ToString("yyyyMMdd") + ".zip";
                    string backupFileName = Path.Combine(tempFolder, fileName);
                    try
                    {
                        await Backup.BackupToFile(backupFileName);

                        Cloud.CloudFileStatus status = await App.CloudConnector.FolderExists(CloudBackupFolder);
                        if (status == Cloud.CloudFileStatus.FileNotFound)
                            await App.CloudConnector.CreateFolder(CloudBackupFolder);
                        else if (status == Cloud.CloudFileStatus.FileError)
                            throw new Exception("Destination folder not available");

                        if (!await App.CloudConnector.UploadFile(backupFileName, Path.Combine(CloudBackupFolder, fileName)))
                            throw new Exception(string.Format("Upload of the file {0} failed", fileName));

                        bBackupSuccess = true;
                        await DisplayAlert(App.Title, AppResources.msgBackupSaved, "Ok");

                        BackupManager.CacheInfo cache = Backup.GetCacheInfo();
                        cache.LastBackupDate = DateTime.Now;
                        Backup.SaveCacheInfo();

                        UpdateBackupInfo();
                    }
                    finally
                    {
                        if (File.Exists(backupFileName))
                            File.Delete(backupFileName);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(string.Format("Backup error: {0}", e.Message));
                    await DisplayAlert(App.Title, string.Format(AppResources.errBackup, e.Message), "Ok");
                }
                finally
                {
                    App.PwdManager.Cloud = CurrentCloud;
                    Backup.ClearAll();                    
                }

                // Verifica se bisogna pulire lo storico dei backup
                if (bBackupSuccess && App.Settings.BackupHistory > 0)
                    await CheckBackupHistory();
            }
            finally
            {
                await StopWait();
            }
        }

        private async Task CheckBackupHistory()
        {
            if (App.Settings.BackupHistory <= 0)
                return;

            try
            {                
                CloudFileStatus status = await App.CloudConnector.FolderExists(CloudBackupFolder);
                if (status == CloudFileStatus.FileNotFound)
                    return; // Non c'è la cartella
                else if (status == CloudFileStatus.FileError)
                    throw new Exception("[backups] folder not available");

                // Elimina i file di backup ormai scaduti
                DateTime expirationDate = DateTime.Now.AddMinutes(-1.0 * App.Settings.BackupHistory);
                IList<CloudFileInfo> backupsFiles = await App.CloudConnector.GetFilesInfo(CloudBackupFolder);
                foreach (CloudFileInfo file in backupsFiles)
                {
                    if (file.LastModifiedDateTime < expirationDate)
                        await App.CloudConnector.DeleteFile(Path.Combine(CloudBackupFolder, file.Name));
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(string.Format("Check backup history error: {0}", e.Message));
                await DisplayAlert(App.Title, string.Format(AppResources.errBackupHistory, e.Message), "Ok");
            }
        }

        private async Task DoBackup()
        {
            CurrentCloud = App.PwdManager.Cloud;
            App.PwdManager.Cloud = false;
            await StartWait();
            try
            {                
                Backup.AddFile(App.PwdManager.GetPasswordFilePath());
                Backup.AddFolder(App.PwdManager.GetAttachmentFolderPath());

                string defaultFilePath = "backup_" + DateTime.Now.ToString("yyyyMMdd") + ".zip";
                ICrossPlatformSpecialFolder specialFolder = DependencyService.Get<ICrossPlatformSpecialFolder>();
                
                MemoryStream streamData = new MemoryStream();
                try
                {
                    long count = await Backup.BackupToFile(streamData);
                    streamData.Position = 0;
                    if (count == 0)
                        throw new Exception("Backup failed!");

                    string fileTypeDesc = Utility.EnumHelper.GetDescription(ImportExportFileFormat.ZIP);
                    List<KeyValuePair<string, List<string>>> fileTypeList = new List<KeyValuePair<string, List<string>>>
                    {
                        new KeyValuePair<string, List<string>>(fileTypeDesc, new List<string>() { ".zip" })
                    };

                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        try
                        {
                            specialFolder.Init();
                            specialFolder.OnSaveFileHandler += Backup_OnSaveFileHandler;
                            if (!await specialFolder.SaveFile(defaultFilePath, fileTypeList, streamData))
                            {
                                specialFolder.Finish();
                                specialFolder.OnSaveFileHandler -= Backup_OnSaveFileHandler;

                                App.PwdManager.Cloud = CurrentCloud;
                                Backup.ClearAll();
                                await StopWait();
                            }
                        }
                        catch(Exception e)
                        {
                            specialFolder.Finish();
                            Debug.WriteLine(string.Format("Backup error: {0}", e.Message));
                            await DisplayAlert(App.Title, string.Format(AppResources.errBackup, e.Message), "Ok");

                            App.PwdManager.Cloud = CurrentCloud;
                            Backup.ClearAll();
                            await StopWait();
                        }
                    });
                }
                catch(Exception)
                {
                    streamData.Dispose();
                    throw;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(string.Format("Backup error: {0}", e.Message));
                await DisplayAlert(App.Title, string.Format(AppResources.errBackup, e.Message), "Ok");

                App.PwdManager.Cloud = CurrentCloud;
                Backup.ClearAll();
                await StopWait();
            }
        }

        private async void Backup_OnSaveFileHandler(object sender, FolderPickerResult info)
        {
            ICrossPlatformSpecialFolder specialFolder = DependencyService.Get<ICrossPlatformSpecialFolder>();
            specialFolder.Finish();
            specialFolder.OnSaveFileHandler -= Backup_OnSaveFileHandler;

            if (info.Succeeded)
            {
                BackupManager.CacheInfo cache = Backup.GetCacheInfo();
                cache.LastBackupDate = DateTime.Now;
                Backup.SaveCacheInfo();

                UpdateBackupInfo();
            }
            else
            {
                await DisplayAlert(App.Title, string.Format(AppResources.errBackup, info.Error), "Ok");
            }

            App.PwdManager.Cloud = CurrentCloud;
            Backup.ClearAll();
            await StopWait();
        }

        private async void BtnRestore_Clicked(object sender, EventArgs e)
        {
            if (App.IsCloudAvailable)
            {
                // Apre la pagina per selezionare la locazione del backup
                BackupLocationPage page = new BackupLocationPage();
                page.OnSelection += RestoreLocation_OnSelection;
                await Navigation.PushModalAsync(page, true);
            }
            else
            {
                // Restore di un backup in locale
                await DoBackupRestore();
            }
        }

        private async Task DoBackupRestore()
        {
            CurrentCloud = App.PwdManager.Cloud;
            App.PwdManager.Cloud = false;
            await StartWait();
            try
            {
                // File da ripristinare
                Backup.AddFile(App.PwdManager.GetPasswordFilePath());
                Backup.AddFolder(App.PwdManager.GetAttachmentFolderPath());

                List<string> typeList = new List<string>();
                ICrossPlatformSpecialFolder crossPlatformSpecialFolder = DependencyService.Get<ICrossPlatformSpecialFolder>();
                typeList.Add(crossPlatformSpecialFolder.GetMIMEType(".zip"));

                Device.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {                        
                        FileData fileData = await CrossFilePicker.Current.PickFile(typeList.ToArray());
                        if (fileData == null)
                        {
                            Debug.WriteLine("Restore cancelled by user");
                            return;
                        }

                        try
                        {                            
                            fileData.GetStream().Position = 0;
                            await Backup.RestoreFromFile(fileData.GetStream());
                        }
                        finally
                        {
                            fileData.Dispose();
                        }

                        // Tutto ok
                        await DisplayAlert(App.Title, AppResources.msgRestoreDone, "Ok");
                        // Apre la lista delle password
                        await App.PwdManager.OpenPasswordFile();
                    }
                    catch(Exception e)
                    {
                        Debug.WriteLine(string.Format("Restore error: {0}", e.Message));
                        await DisplayAlert(App.Title, string.Format(AppResources.errBackupRestore, e.Message), "Ok");
                    }
                    finally
                    {                        
                        Backup.ClearAll();
                        App.PwdManager.Cloud = CurrentCloud;
                        await StopWait();
                    }
                });
            }
            catch (Exception e)
            {
                Debug.WriteLine(string.Format("Restore error: {0}", e.Message));
                await DisplayAlert(App.Title, string.Format(AppResources.errBackupRestore, e.Message), "Ok");

                Backup.ClearAll();
                App.PwdManager.Cloud = CurrentCloud;
                await StopWait();
            }
        }

        private async void RestoreLocation_OnSelection(object sender, BackupLocation location)
        {
            switch(location)
            {
                case BackupLocation.Local:
                    await DoBackupRestore();
                break;
                case BackupLocation.Cloud:
                    await DoBackupRestoreFromCloud();
                break;
                default:
                    {
                    Debug.WriteLine("Invalid location " + location.ToString());
                    await DisplayAlert(App.Title, string.Format(AppResources.errBackupRestore, "Invalid location " + location.ToString()), "Ok");
                    break;
                }
            }
        }

        private int CompareCloudFileInfo(CloudFileInfo item1, CloudFileInfo item2)
        {
            if (item1.CreatedDateTime < item2.CreatedDateTime)
                return 1;
            else if (item1.CreatedDateTime > item2.CreatedDateTime)
                return -1;
            return 0;
        }

        private async Task<IList<CloudFileInfo>> GetBackupsListFromCloud()
        {
            await StartWait();
            try
            {
                // Verifica se esiste la cartella dei backup su cloud
                CloudFileStatus status = await App.CloudConnector.FolderExists(CloudBackupFolder);
                if (status == CloudFileStatus.FileNotFound)
                    throw new Exception(AppResources.msgNoBackup);
                else if (status == CloudFileStatus.FileError)
                    throw new Exception(AppResources.errCloudAccess + " " + App.CloudConnector.GetLastError());

                // Recupera la lista dei file di backup
                IList<CloudFileInfo> files = await App.CloudConnector.GetFilesInfo(CloudBackupFolder);
                if (files.Count == 0)
                    throw new Exception(AppResources.msgNoBackup);

                List<CloudFileInfo> sortedList = new List<CloudFileInfo>(files);
                sortedList.Sort(CompareCloudFileInfo);
                return sortedList;
            }
            finally
            {
                await StopWait();
            }
        }

        private async Task DoBackupRestoreFromCloud()
        {
            if (!App.IsCloudAvailable)
                return;

            if (!App.IsCloudEnabled())
            {
                if (await DisplayAlert(App.Title, AppResources.msgQuestionCloudNoConn, AppResources.btnYes, AppResources.btnNo))
                {
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

            try
            {
                // Lista dei file da mostrare nella pagina di selezione
                IList<CloudFileInfo> files = await GetBackupsListFromCloud();
                List<FileSelectionPage.FileData> data = new List<FileSelectionPage.FileData>();
                int i = 0;
                foreach (CloudFileInfo file in files)
                {
                    data.Add(new FileSelectionPage.FileData
                    {
                        Order = i++,
                        Name = file.Name,
                        CreationDateTime = file.CreatedDateTime
                    });
                }

                FileSelectionPage page = new FileSelectionPage
                {
                    Title = AppResources.titleBackup,
                    BindingContext = new
                    {
                        Title = AppResources.titleBackup,
                        SubTitle = AppResources.txtBackupSelection,
                        FilesList = data,
                        ConfirmButtonText = AppResources.btnRestore
                    }
                };
                page.OnFileSelected += Page_OnBackupFileSelected;
                await Navigation.PushAsync(page, true);
            }
            catch(Exception e)
            {
                await DisplayAlert(App.Title, e.Message, "Ok");
            }
        }

        private async Task Page_OnBackupFileSelected(object sender, FileSelectionPage.FileData selectedFile)
        {
            // Scarica il file dal Cloud
            ICrossPlatformSpecialFolder specialFolder = DependencyService.Get<ICrossPlatformSpecialFolder>();
            string tempFolder = specialFolder.GetTemporaryFolder();
            string backupFile = Path.Combine(tempFolder, selectedFile.Name);

            CurrentCloud = App.PwdManager.Cloud;
            App.PwdManager.Cloud = false;
            await StartWait();
            try
            {
                CloudFileStatus status = await App.CloudConnector.GetFile(Path.Combine(CloudBackupFolder, selectedFile.Name), backupFile);
                if (status == CloudFileStatus.FileDownloadError)
                    throw new Exception(AppResources.errCloudDownload + " " + status.ToString());
                
                // File da ripristinare
                Backup.AddFile(App.PwdManager.GetPasswordFilePath());
                Backup.AddFolder(App.PwdManager.GetAttachmentFolderPath());
                // Ripristino
                Backup.RestoreFromFile(backupFile);

                // Tutto ok
                await DisplayAlert(App.Title, AppResources.msgRestoreDone, "Ok");
                // Apre la lista delle password
                await App.PwdManager.OpenPasswordFile();
            }
            catch(Exception e)
            {
                Debug.WriteLine(string.Format("Restore error: {0}", e.Message));
                await DisplayAlert(App.Title, string.Format(AppResources.errBackupRestore, e.Message), "Ok");
            }
            finally
            {
                if (File.Exists(backupFile))
                    File.Delete(backupFile);
                Backup.ClearAll();
                App.PwdManager.Cloud = CurrentCloud;
                await StopWait();
            }
        }
    }
}