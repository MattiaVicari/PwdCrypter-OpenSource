using Newtonsoft.Json.Linq;
using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace PwdCrypter
{
    /// <summary>
    /// Classe per la gestione del backup
    /// </summary>
    public class BackupManager
    {
        const string CacheFile = "backup.cache";

        private readonly Dictionary<string, string> Files;
        private readonly Dictionary<string, string> Folders;
        private readonly CacheInfo Cache = new CacheInfo();

        public string CacheFilePath => GetCacheFilePath();

        private string GetCacheFilePath()
        {
            string appFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string filePath = Path.Combine(appFolder, CacheFile);
            return filePath;
        }

        public BackupManager()
        {
            Files = new Dictionary<string, string>();
            Folders = new Dictionary<string, string>();            
        }

        /// <summary>
        /// Aggiunge un file alla lista dei file di cui fare il backup
        /// </summary>
        /// <param name="filePath">Percorso assoluto del file</param>
        public void AddFile(string filePath)
        {
            Files.Add(filePath, "");
        }

        /// <summary>
        /// Aggiunge una cartella alla lista delle cartelle di cui fare il backup
        /// </summary>
        /// <param name="folderPath">Percorso assoluto della cartella</param>
        public void AddFolder(string folderPath)
        {
            Folders.Add(folderPath, "");
        }

        /// <summary>
        /// Cancella tutte le informazioni di backup dal backup manager (lasciando intatti i file)
        /// </summary>
        public void ClearAll()
        {
            Files.Clear();
            Folders.Clear();
        }

        /// <summary>
        /// Effettua il backup dei file e delle cartelle
        /// </summary>
        public void Backup()
        {
            // Rinomina il file con un eventuale suffisso, compresa la cartella degli allegati
            string timestamp = DateTime.Now.ToString("ddMMyyyy_HHmmss");

            try
            {
                List<string> filesKey = new List<string>(Files.Keys);
                foreach (string filePath in filesKey)
                {
                    string ext = Path.GetExtension(filePath);
                    string newPath = filePath.Replace(ext, ext + "_" + timestamp);
                    File.Move(filePath, newPath);
                    Files[filePath] = newPath;
                }

                List<string> foldersKey = new List<string>(Folders.Keys);
                foreach (string folderPath in foldersKey)
                {
                    if (Directory.Exists(folderPath))
                    {
                        string newAttachmentPath = folderPath + "_" + timestamp;
                        Directory.Move(folderPath, newAttachmentPath);
                        Folders[folderPath] = newAttachmentPath;
                    }
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("Error occurred during the backup. Error: " + Ex.Message);

                // Ripristina i file e le cartelle
                Restore();
                throw;
            }
        }

        /// <summary>
        /// Effettua il backup dei file e delle cartelle specificate, in formato ZIP.        
        /// </summary>
        /// <param name="fileStream">Stream del file ZIP con il backup dei file e delle cartelle</param>
        /// <returns>Dimensione in byte del file di backup</returns>
        public async Task<long> BackupToFile(Stream fileStream)
        {
            ICrossPlatformSpecialFolder specialFolder = DependencyService.Get<ICrossPlatformSpecialFolder>();
            string tempFolder = specialFolder.GetTemporaryFolder();
            string tempExport = Path.Combine(tempFolder, DateTime.Now.ToString("ddMMyyyy_HHmmss"));
            string tempZIPFolder = Path.Combine(tempExport, "zip");
            Directory.CreateDirectory(tempZIPFolder);

            try
            {
                foreach (string filePath in Files.Keys)
                {
                    string newPath = Path.Combine(tempZIPFolder, Path.GetFileName(filePath));
                    File.Copy(filePath, newPath, true);
                }
                foreach (string folderPath in Folders.Keys)
                {
                    string newFolder = Path.Combine(tempZIPFolder, Path.GetFileName(folderPath));
                    Directory.CreateDirectory(newFolder);
                    string[] files = Directory.GetFiles(folderPath);
                    foreach(string filePath in files)
                    {
                        string newPath = Path.Combine(newFolder, Path.GetFileName(filePath));
                        File.Copy(filePath, newPath, true);
                    }
                }
                await ArchiveUtility.ZipArchive(fileStream, tempZIPFolder);
            }
            finally
            {
                if (Directory.Exists(tempExport))
                    Directory.Delete(tempExport, true);
            }
            return fileStream.Length;
        }

        /// <summary>
        /// Effettua il backup dei file e delle cartelle specificate, in formato ZIP.
        /// </summary>
        /// <param name="filePath">Percorso del file di destinazione del backup</param>
        /// <returns></returns>
        public async Task BackupToFile(string filePath)
        {
            FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate);
            try
            {
                long count = await BackupToFile(fileStream);
                if (count == 0)
                    throw new Exception("Backup failed");
                fileStream.Flush();
            }
            finally
            {
                fileStream.Dispose();
            }
        }

        private void DoRestore(string zipFolder)
        {
            string[] zipFiles = Directory.GetFiles(zipFolder, "*", SearchOption.TopDirectoryOnly);
            foreach (string filePath in Files.Keys)
            {
                string path = zipFiles.First((item) => { return Path.GetFileName(item).CompareTo(Path.GetFileName(filePath)) == 0; });
                if (!string.IsNullOrEmpty(path))
                {
                    File.Delete(filePath);
                    File.Move(path, filePath);
                }
            }
            zipFiles = Directory.GetDirectories(zipFolder, "*", SearchOption.TopDirectoryOnly);
            foreach (string folderPath in Folders.Keys)
            {
                string path = zipFiles.First((item) => { return Path.GetFileName(item).CompareTo(Path.GetFileName(folderPath)) == 0; });
                if (!string.IsNullOrEmpty(path))
                {
                    Directory.Delete(folderPath, true);
                    Directory.Move(path, folderPath);
                }
            }
        }

        /// <summary>
        /// Esegue il ripristino di un backup.
        /// </summary>
        /// <param name="fileStream">Stream del contenuto del file del backup da ripristinare</param>
        /// <returns></returns>
        public async Task RestoreFromFile(Stream fileStream)
        {
            ICrossPlatformSpecialFolder specialFolder = DependencyService.Get<ICrossPlatformSpecialFolder>();
            string tempFolder = specialFolder.GetTemporaryFolder();
            string tempExport = Path.Combine(tempFolder, "zip_restore_" + DateTime.Now.ToString("ddMMyyyy_HHmmss"));
            Directory.CreateDirectory(tempExport);

            try
            {
                await ArchiveUtility.UnzipArchive(fileStream, tempExport);
                DoRestore(tempExport);
            }
            finally
            {
                if (Directory.Exists(tempExport))
                    Directory.Delete(tempExport, true);
            }
        }

        /// <summary>
        /// Esegue il ripristino di un backup.
        /// </summary>
        /// <param name="backupFilePath">Percorso del file zip di backup</param>
        public void RestoreFromFile(string backupFilePath)
        {
            ICrossPlatformSpecialFolder specialFolder = DependencyService.Get<ICrossPlatformSpecialFolder>();
            string tempFolder = specialFolder.GetTemporaryFolder();
            string tempExport = Path.Combine(tempFolder, "zip_restore_" + DateTime.Now.ToString("ddMMyyyy_HHmmss"));
            Directory.CreateDirectory(tempExport);

            try
            {
                ArchiveUtility.UnzipArchive(backupFilePath, tempExport);
                DoRestore(tempExport);
            }
            finally
            {
                if (Directory.Exists(tempExport))
                    Directory.Delete(tempExport, true);
            }
        }

        /// <summary>
        /// Ripristina i file e le cartelle dal backup
        /// </summary>
        public void Restore()
        {
            foreach (KeyValuePair<string, string> filePair in Files)
            {
                if (!string.IsNullOrWhiteSpace(filePair.Value) && File.Exists(filePair.Value))
                {
                    if (File.Exists(filePair.Key))
                        File.Delete(filePair.Key);
                    File.Move(filePair.Value, filePair.Key);
                }
            }

            foreach (KeyValuePair<string, string> folderPair in Folders)
            {
                if (!string.IsNullOrWhiteSpace(folderPair.Value) && Directory.Exists(folderPair.Value))
                {
                    if (Directory.Exists(folderPair.Key))
                        Directory.Delete(folderPair.Key, true);
                    Directory.Move(folderPair.Value, folderPair.Key);
                }
            }

            DeleteBackup();

            Debug.WriteLine("File and folder restored");
        }

        /// <summary>
        /// Cancella i file e le cartelle di backup
        /// </summary>
        public void DeleteBackup()
        {
            foreach (KeyValuePair<string, string> filePair in Files)
            {
                if (!string.IsNullOrWhiteSpace(filePair.Value) && File.Exists(filePair.Value))
                    File.Delete(filePair.Value);
            }

            foreach (KeyValuePair<string, string> folderPair in Folders)
            {
                if (!string.IsNullOrWhiteSpace(folderPair.Value) && Directory.Exists(folderPair.Value))
                    Directory.Delete(folderPair.Value, true);
            }

            ClearAll();
        }

        #region CacheInfo
        /// <summary>
        /// Restituisce l'oggetto con le informazioni in cache sull'ultimo backup
        /// </summary>
        /// <returns>Oggetto CacheInfo</returns>
        public CacheInfo GetCacheInfo()
        {
            LoadCacheInfo();
            return Cache;
        }

        /// <summary>
        /// Carica le informazioni presenti in cache.
        /// Utilizzare GetCacheInfo per leggere i dati presenti in cache.
        /// </summary>
        public void LoadCacheInfo()
        {
            try
            {
                string jsonContent = File.ReadAllText(CacheFilePath, System.Text.Encoding.UTF8);
                JObject json = JObject.Parse(jsonContent);
                Cache.LoadFromJSON(json);
            }
            catch(FileNotFoundException)
            {
                Cache.Clear();
            }
            catch(Exception e)
            {
                Debug.WriteLine("Unable to load data from the file " + CacheFilePath + ". Error: " + e.Message);
                throw new Exception(string.Format(AppResources.errBackupLoadCacheFailed, e.Message));
            }            
        }

        /// <summary>
        /// Salva i dati correnti riguardo al backup in cache.
        /// </summary>
        public void SaveCacheInfo()
        {
            try
            {
                JObject json = Cache.SaveToJSON();
                File.WriteAllText(CacheFilePath, json.ToString(), System.Text.Encoding.UTF8);
            }
            catch(Exception e)
            {
                Debug.WriteLine("Unable to save to the file " + CacheFilePath + ". Error: " + e.Message);
                throw new Exception(string.Format(AppResources.errBackupSaveCacheFailed, e.Message));
            }
        }

        /// <summary>
        /// Classe per la raccolte delle informazioni da salvare in cache
        /// </summary>
        public class CacheInfo
        {
            static public readonly Newtonsoft.Json.Converters.IsoDateTimeConverter dateTimeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter
            {
                DateTimeFormat = "yyyy-MM-ddTH:mm:ss.fffK"                
            };

            public DateTime? LastBackupDate { get; set; }
            public DateTime? NextBackupDate { get; set; }

            public void Clear()
            {
                LastBackupDate = null;
                NextBackupDate = null;
            }

            public void LoadFromJSON(JObject json)
            {              
                if (!Utility.IsJTokenNullOrEmpty(json[nameof(LastBackupDate)]))
                {
                    LastBackupDate = Newtonsoft.Json.JsonConvert.DeserializeObject<DateTime>(
                                            json[nameof(LastBackupDate)].ToString(),
                                            dateTimeConverter);
                }
                else
                    LastBackupDate = null;
                if (!Utility.IsJTokenNullOrEmpty(json[nameof(NextBackupDate)]))
                {
                    NextBackupDate = Newtonsoft.Json.JsonConvert.DeserializeObject<DateTime>(
                                            json[nameof(NextBackupDate)].ToString(),
                                            dateTimeConverter);
                }
                else
                    NextBackupDate = null;
            }

            public JObject SaveToJSON()
            {
                return new JObject
                {                    
                    { nameof(LastBackupDate), new JValue(Newtonsoft.Json.JsonConvert.SerializeObject(LastBackupDate, dateTimeConverter)) },
                    { nameof(NextBackupDate), new JValue(Newtonsoft.Json.JsonConvert.SerializeObject(NextBackupDate, dateTimeConverter)) }
                };
            }
        }
        #endregion
    }
}
