using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Xamarin.Forms;

namespace PwdCrypter.Droid
{
    [Activity(Label = "Select folder")]
    public class FolderPickerActivity : Activity
    {
        const string Tag = "FolderPickerActivity";
        const int PICK_FOLDER_REQUEST = 1;

        private string FileName;
        private string TempFilePath;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            FileName = Intent.GetStringExtra("FILENAME");
            TempFilePath = Intent.GetStringExtra("TEMP_FILENAME_PATH");

            if (string.IsNullOrEmpty(FileName))
                throw new System.Exception("Filename is missing for folder picker");
            if (string.IsNullOrEmpty(TempFilePath))
                throw new System.Exception("File with data to save is missing. Unable to complete the operation");

            Intent intentPicker = new Intent(Intent.ActionOpenDocumentTree);
            StartActivityForResult(Intent.CreateChooser(intentPicker, "Select folder"), PICK_FOLDER_REQUEST);
        }

        protected async override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == PICK_FOLDER_REQUEST)
            {
                FolderPickerResult info = new FolderPickerResult
                {
                    Succeeded = (resultCode == Result.Ok)
                };

                try
                {
                    if (resultCode == Result.Ok)
                    {
                        Android.Net.Uri uri = data.Data;
                        info.FilePath = await SaveFileToFolder(uri, FileName);                    

                        // Invia la notifica all'utente dell'avvenuto salvataggio
                        Notifier toastNotifier = new Notifier();
                        toastNotifier.SendNotification(App.Title, string.Format(Resources.GetString(Resource.String.msgPwdExportTo), info.FilePath));
                    }

                    Java.IO.File tempFile = new Java.IO.File(TempFilePath);
                    if (tempFile.Exists())
                        tempFile.Delete();

                    SetResult(resultCode);
                    Finish();
                }
                catch(Exception e)
                {
                    Log.Error(Tag, string.Format("OnActivityResult failed!. Error: {0}", e.Message));
                    info.Succeeded = false;
                    info.Error = e.Message;
                    SetResult(Result.Canceled);
                    Finish();                    
                }
                finally
                {
                    MessagingCenter.Send(CrossPlatformSpecialFolder.Instance, CrossPlatformSpecialFolder.SaveFileActivityMessage, info);
                }
            }
        }

        private async Task<string> SaveFileToFolder(Android.Net.Uri uri, string fileName)
        {
            Android.Net.Uri docUri = Android.Provider.DocumentsContract.BuildDocumentUriUsingTree(uri,
                                        Android.Provider.DocumentsContract.GetTreeDocumentId(uri));

            string fileFolder = SAFFileUtil.GetPath(this, docUri);

            Java.IO.File destFolder = new Java.IO.File(fileFolder);
            Java.IO.File file = new Java.IO.File(destFolder, fileName);
            Java.IO.File tempFile = new Java.IO.File(TempFilePath);
            destFolder.Mkdirs();
            if (!file.Exists())
                file.CreateNewFile();

            Java.IO.FileInputStream fileInput = new Java.IO.FileInputStream(tempFile);
            Java.IO.FileOutputStream fileOutput = new Java.IO.FileOutputStream(file, false);

            while (true)
            {
                byte[] data = new byte[1024];
                int read = await fileInput.ReadAsync(data, 0, data.Length);
                if (read <= 0)
                    break;
                await fileOutput.WriteAsync(data, 0, read);
            }
            fileInput.Close();
            fileOutput.Close();

            return file.AbsolutePath;
        }
    }
}