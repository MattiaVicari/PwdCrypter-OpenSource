using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Xamarin.Forms;

[assembly: Dependency(typeof(PwdCrypter.UWP.CrossPlatformSpecialFolder))]
namespace PwdCrypter.UWP
{
    public class CrossPlatformSpecialFolder : ICrossPlatformSpecialFolder
    {
        public event EventHandlerSaveFile OnSaveFileHandler;

        public string GetMIMEType(string extension)
        {
            return extension.ToLower();
        }

        public string GetTemporaryFolder()
        {
            StorageFolder temporaryFolder = ApplicationData.Current.TemporaryFolder;
            return temporaryFolder.Path;
        }

        public async Task OpenFileByDefaultApp(string filepath)
        {
            StorageFolder temporaryFolder = ApplicationData.Current.TemporaryFolder;
            StorageFile file = await temporaryFolder.GetFileAsync(Path.GetFileName(filepath));
            if (file == null)
                throw new Exception("Unable to open the file");
            
            if (!await Windows.System.Launcher.LaunchFileAsync(file))
                throw new Exception("Unable to launch the default App for open the file");
        }

        private async Task<StorageFile> SelectFile(string defaultFileName, List<KeyValuePair<string, List<string>>> fileTypeList)
        {
            var picker = new Windows.Storage.Pickers.FileSavePicker
            {
                SuggestedFileName = defaultFileName,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
                DefaultFileExtension = Path.GetExtension(defaultFileName)
            };
            foreach (KeyValuePair<string, List<string>> keyValue in fileTypeList)
                picker.FileTypeChoices.Add(keyValue.Key, keyValue.Value);

            StorageFile file = await picker.PickSaveFileAsync();
            return file;
        }

        public async Task<bool> SaveFile(string defaultFileName, List<KeyValuePair<string, List<string>>> fileTypeList, byte[] data)
        {
            try
            {
                StorageFile file = await SelectFile(defaultFileName, fileTypeList);
                if (file == null)
                    return false;  // Selezione annullata
                await FileIO.WriteBytesAsync(file, data);
                
                OnSaveFileHandler?.Invoke(this, new FolderPickerResult
                {
                    Succeeded = true,
                    FilePath = file.Path
                });

                Windows.ApplicationModel.Resources.ResourceLoader ResLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                string msg = string.Format(ResLoader.GetString("msgPwdExportTo"), file.Path);

                Notifier notifier = new Notifier();
                notifier.SendNotification("PwdCrypter", msg);

                return true;
            }
            catch (Exception Ex)
            {
                throw new Exception("Unable to save the password list. Error: " + Ex.Message);
            }
        }

        public async Task<bool> SaveFile(string defaultFileName, List<KeyValuePair<string, List<string>>> fileTypeList, Stream streamData)
        {
            try
            {
                StorageFile file = await SelectFile(defaultFileName, fileTypeList);
                if (file == null)
                    return false;  // Selezione annullata

                int pos = -1, read = 0;
                byte[] data = new byte[streamData.Length];
                do
                {
                    long count = Math.Min(1024, streamData.Length - streamData.Position);
                    read = await streamData.ReadAsync(data, pos + 1, (int)count);                    
                    pos += read;
                }
                while (pos < streamData.Length - 1);                
                await FileIO.WriteBytesAsync(file, data);                

                OnSaveFileHandler?.Invoke(this, new FolderPickerResult
                {
                    Succeeded = true,
                    FilePath = file.Path
                });

                Windows.ApplicationModel.Resources.ResourceLoader ResLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                string msg = string.Format(ResLoader.GetString("msgPwdExportTo"), file.Path);

                Notifier notifier = new Notifier();
                notifier.SendNotification("PwdCrypter", msg);

                return true;
            }
            catch (Exception Ex)
            {
                throw new Exception("Unable to save the password list. Error: " + Ex.Message);
            }            
        }

        public void Init()
        {
            // Nulla
        }

        public void Finish()
        {
            // Nulla
        }
    }
}
