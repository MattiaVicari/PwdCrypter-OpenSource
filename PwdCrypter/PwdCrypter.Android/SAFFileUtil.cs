using Android.Content;
using Android.OS;
using Android.Net;
using Android.Provider;
using Android.Graphics;
using System.Linq;

namespace PwdCrypter.Droid
{
    /// <summary>
    /// Riferimento:
    /// https://stackoverflow.com/questions/20067508/get-real-path-from-uri-android-kitkat-new-storage-access-framework/20559175#20559175
    /// aFileChooser GitHub: https://github.com/iPaulPro/aFileChooser
    /// </summary>
    public class SAFFileUtil
    {
        /// <summary>
        /// Restituisce il percorso assoluto di un uri in formato Storage Access Framework (SAF).
        /// https://developer.android.com/guide/topics/providers/document-provider
        /// </summary>
        /// <param name="context">Context corrente</param>
        /// <param name="uri">Uri di cui restituire il percorso assoluto</param>
        /// <returns>Percorso reale dell'uri</returns>
        [Android.Annotation.TargetApi(Value = (int)BuildVersionCodes.Kitkat)]
        public static string GetPath(Context context, Uri uri)
        {
            bool isKitKat = Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat;

            // DocumentProvider (da Kitkat in su)
            if (isKitKat && DocumentsContract.IsDocumentUri(context, uri))
            {
                // ExternalStorageProvider
                if (IsExternalStorageDocument(uri))
                {
                    string docId = DocumentsContract.GetDocumentId(uri);
                    string[] split = docId.Split(":");
                    string type = split[0];

                    if (type.Equals("primary", System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (split.Length > 1)                            
                            return System.IO.Path.Combine(ExtractExternalStorageFolderRoot(context.GetExternalFilesDir(null).AbsolutePath), split[1]);
                        else
                            return ExtractExternalStorageFolderRoot(context.GetExternalFilesDir(null).AbsolutePath) + Java.IO.File.Separator;                            
                    }

                    Java.IO.File[] externalPaths = Android.App.Application.Context.GetExternalFilesDirs(null);
                    if (externalPaths.Length <= 1)
                        throw new System.Exception("Unable to access to the external storage");

                    // SDCard è in posizione 1                    
                    return System.IO.Path.Combine(ExtractExternalStorageFolderRoot(externalPaths[1].AbsolutePath), split[1]);
                }
                // DownloadsProvider
                else if (IsDownloadsDocument(uri))
                {
                    string id = DocumentsContract.GetDocumentId(uri);
                    Uri contentUri = ContentUris.WithAppendedId(
                            Uri.Parse("content://downloads/public_downloads"), long.Parse(id));

                    return GetDataColumn(context, contentUri, null, null);
                }
                // MediaProvider
                else if (IsMediaDocument(uri))
                {
                    string docId = DocumentsContract.GetDocumentId(uri);
                    string[] split = docId.Split(":");
                    string type = split[0];

                    Uri contentUri = null;
                    if (type.Equals("image"))
                    {
                        contentUri = MediaStore.Images.Media.ExternalContentUri;
                    }
                    else if (type.Equals("video"))
                    {
                        contentUri = MediaStore.Video.Media.ExternalContentUri;
                    }
                    else if (type.Equals("audio"))
                    {
                        contentUri = MediaStore.Audio.Media.ExternalContentUri;
                    }

                    string selection = "_id=?";
                    string[] selectionArgs = new string[] {
                        split[1]
                    };

                    return GetDataColumn(context, contentUri, selection, selectionArgs);
                }
            }
            // MediaStore (generale)
            else if (uri.Scheme.Equals("content", System.StringComparison.InvariantCultureIgnoreCase))
            {
                if (IsGooglePhotosUri(uri))
                    return uri.LastPathSegment;

                return GetDataColumn(context, uri, null, null);
            }
            // File
            else if (uri.Scheme.Equals("file", System.StringComparison.InvariantCultureIgnoreCase))
            {
                return uri.Path;
            }

            throw new System.Exception("Unable to compute the real path of uri: " + uri.Path);
        }

        /// <summary>
        /// Estrae il percorso base della cartella dell'external storage files directory
        /// </summary>
        /// <param name="externalFolderPath">Percorso dell'external files directory</param>
        /// <returns>Percorso base dell'external files directory (es: /storage/sdcard/)</returns>
        private static string ExtractExternalStorageFolderRoot(string externalFolderPath)
        {
            int minLen = 4;
            string[] splitFolder = externalFolderPath.Split(Java.IO.File.Separator);

            if (!externalFolderPath.StartsWith(Java.IO.File.Separator))
                throw new System.Exception(string.Format("Invalid folder path: {0}", externalFolderPath));
            if (splitFolder.Length > 1 && !splitFolder[1].Equals("storage", System.StringComparison.InvariantCultureIgnoreCase))
                throw new System.Exception(string.Format("Invalid external storage folder: {0}", externalFolderPath));
            if (splitFolder.Length > 2 && splitFolder[2].StartsWith("sdcard"))      // /storage/sdcard/...
                minLen = 3;
            if (splitFolder.Length > 2 && splitFolder[2].StartsWith("emulated"))    // /storage/emulated/0/...
                minLen = 4;

            if (splitFolder.Length < minLen)
                throw new System.Exception(string.Format("Invalid external storage folder: {0}", externalFolderPath));

            string path = splitFolder[0];
            for (int i = 1; i < minLen; i++)
                path += Java.IO.File.Separator + splitFolder[i];
            return path;
        }        

        /// <summary>
        /// Restituisce il valore di un elemento dell'albero
        /// </summary>
        /// <param name="context">Context corrente</param>
        /// <param name="uri">Uri da parserizzare</param>
        /// <param name="selection">Elemento dell'albero di cui restituire il valore</param>
        /// <param name="selectionArgs">Elementi dell'albero</param>
        /// <returns></returns>
        private static string GetDataColumn(Context context, Uri uri, string selection, string[] selectionArgs)
        {
            Android.Database.ICursor cursor = null;
            string column = "_data";
            string[] projection = {
                column
            };

            try
            {
                cursor = context.ContentResolver.Query(uri, projection, selection, selectionArgs, null);
                if (cursor != null && cursor.MoveToFirst())
                {
                    int index = cursor.GetColumnIndexOrThrow(column);
                    return cursor.GetString(index);
                }
            }
            finally
            {
                if (cursor != null)
                    cursor.Close();
            }
            return null;
        }


        /// <summary>
        /// Verifica se l'uri è relativo all'external storage
        /// </summary>
        /// <param name="uri">Uri da verificare</param>
        /// <returns>Restituisce true se l'uri è relativo all'extenal storage, false altrimenti</returns>
        private static bool IsExternalStorageDocument(Uri uri)
        {
            return uri.Authority.Equals("com.android.externalstorage.documents");
        }

        /// <summary>
        /// Verifica se l'uri è relativo alla cartella download
        /// </summary>
        /// <param name="uri">Uri da verificare</param>
        /// <returns>Restituisce true se l'uri è relativo alla cartella download, false altrimenti</returns>
        private static bool IsDownloadsDocument(Uri uri)
        {
            return uri.Authority.Equals("com.android.providers.downloads.documents");
        }

        /// <summary>
        /// Verifica se l'uri è relativo alla cartella dei media
        /// </summary>
        /// <param name="uri">Uri da verificare</param>
        /// <returns>Restituisce true se l'uri è relativo alla cartella dei media, false altrimenti</returns>
        private static bool IsMediaDocument(Uri uri)
        {
            return uri.Authority.Equals("com.android.providers.media.documents");
        }

        /// <summary>
        /// Verifica se l'uri è relativo alla cartella delle foto
        /// </summary>
        /// <param name="uri">Uri da verificare</param>
        /// <returns>Restituisce true se l'uri è relativo alla cartella delle foto, false altrimenti</returns>
        private static bool IsGooglePhotosUri(Uri uri)
        {
            return uri.Authority.Equals("com.google.android.apps.photos.content");
        }
    }
}