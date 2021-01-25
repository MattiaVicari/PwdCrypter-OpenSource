using Xamarin.Forms;
using System.IO;
using System.Threading.Tasks;
using Android.Content;
using System.Collections.Generic;
using System;
using Android.Runtime;

[assembly: Dependency(typeof(PwdCrypter.Droid.CrossPlatformSpecialFolder))]
namespace PwdCrypter.Droid
{
    public class CrossPlatformSpecialFolder: ICrossPlatformSpecialFolder
    {
        public const string SaveFileActivityMessage = "PICK_FOLDER_REQUEST_RESULT";

        public static CrossPlatformSpecialFolder Instance = null;

        public event EventHandlerSaveFile OnSaveFileHandler;

        private bool IsInit = false;

        public CrossPlatformSpecialFolder()
        {
            Instance = this;
        }

        public string GetMIMEType(string extension)
        {
            string application;

            switch (extension.ToLower())
            {
                case ".txt":
                case ".json":
                    application = "text/plain";
                    break;
                case ".zip":
                    application = "application/zip";
                    break;
                case ".doc":
                case ".docx":
                    application = "application/msword";
                    break;
                case ".pdf":
                    application = "application/pdf";
                    break;
                case ".xls":
                case ".xlsx":
                    application = "application/vnd.ms-excel";
                    break;
                case ".jpg":
                case ".jpeg":
                case ".png":
                    application = "image/jpeg";
                    break;
                default:
                    application = "*/*";
                    break;
            }

            return application;
        }

        public string GetTemporaryFolder()
        {
            var context = Android.App.Application.Context;
            string tmp = Path.Combine(context.FilesDir.AbsolutePath, "tmp");
            Java.IO.File file = new Java.IO.File(tmp);
            file.Mkdirs();
            if (!file.Exists())
                file.CreateNewFile();
            return file.AbsolutePath;
        }

        public async Task OpenFileByDefaultApp(string filepath)
        {
            await Task.Run(() =>
            {
                Java.IO.File file = new Java.IO.File(filepath);
                file.SetReadable(true);

                string extension = Path.GetExtension(filepath);
                string application = GetMIMEType(extension);

                Android.Net.Uri uri = Android.Net.Uri.FromFile(file);
                Intent intent = new Intent(Intent.ActionView);
                intent.SetDataAndType(uri, application);
                intent.SetFlags(ActivityFlags.ClearWhenTaskReset | ActivityFlags.NewTask);

                Context context = Android.App.Application.Context;
                context.StartActivity(intent);
            });
        }

        public async Task<bool> SaveFile(string defaultFileName, List<KeyValuePair<string, List<string>>> fileTypeList, byte[] data)
        {           
            // N.B.: la selezione del formato in fileTypeList non serve
            return await Task.Run(() =>
            {
                string tempFolder = GetTemporaryFolder();
                string tempFile = Path.Combine(tempFolder, Environment.TickCount + ".temp");
                File.WriteAllBytes(tempFile, data);

                try
                {
                    Context context = Android.App.Application.Context;
                    Intent intent = new Intent(context, typeof(FolderPickerActivity));
                    intent.PutExtra("FILENAME", defaultFileName);
                    intent.PutExtra("TEMP_FILENAME_PATH", tempFile);
                    intent.SetFlags(ActivityFlags.NewTask);
                    context.StartActivity(intent);

                    return true;
                }
                catch (Exception)
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                    throw;
                }
            });
        }

        public async Task<bool> SaveFile(string defaultFileName, List<KeyValuePair<string, List<string>>> fileTypeList, Stream streamData)
        {
            // N.B.: la selezione del formato in fileTypeList non serve
            return await Task.Run(async () =>
            {
                string tempFolder = GetTemporaryFolder();
                string tempFile = Path.Combine(tempFolder, Environment.TickCount + ".temp");
                FileStream fileStream = new FileStream(tempFile, FileMode.Create);
                streamData.Position = 0;
                await streamData.CopyToAsync(fileStream);
                fileStream.Close();

                try
                {
                    Context context = Android.App.Application.Context;
                    Intent intent = new Intent(context, typeof(FolderPickerActivity));
                    intent.PutExtra("FILENAME", defaultFileName);
                    intent.PutExtra("TEMP_FILENAME_PATH", tempFile);
                    intent.SetFlags(ActivityFlags.NewTask);
                    context.StartActivity(intent);

                    return true;
                }
                catch(Exception)
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                    throw;
                }
            });
        }

        /// <summary>
        /// Metodo da chiamare prima di utilizzare l'oggetto
        /// </summary>
        public void Init()
        {
            if (!IsInit)
            {
                MessagingCenter.Subscribe(this, SaveFileActivityMessage,
                    (CrossPlatformSpecialFolder sender, FolderPickerResult info) =>
                    {
                        OnSaveFileHandler?.Invoke(sender, info);
                    });
            }
            IsInit = true;
        }

        /// <summary>
        /// Metodo da chiamare quando si termina di utilizzare l'oggetto
        /// </summary>
        public void Finish()
        {
            if (IsInit)
                MessagingCenter.Unsubscribe<CrossPlatformSpecialFolder, FolderPickerResult>(this, SaveFileActivityMessage);
            IsInit = false;
        }        
    }
}