using Newtonsoft.Json.Linq;
using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PwdCrypter
{
    /// <summary>
    /// Tipo di nome utente da utilizzare per il login
    /// </summary>
    public enum UsernameLoginOption
    {
        Username,
        Email
    };

    /// <summary>
    /// Tipologia di account
    /// </summary>
    public enum AccountType
    {
        Other,
        Email,
        SocialNetwork,
        Bank,
        CreditDebitCard,
        ECommerce,
        DeviceHome,
        DeviceWork,
        Device,
        App,
        Software,
        Institutional
    };

    /// <summary>
    /// Tipo di ordinamento delle password
    /// </summary>
    public enum PwdOrder
    {
        Registration,
        Name,
        AccountType
    };

    /// <summary>
    /// Formati di esportazione del file delle password
    /// </summary>
    public enum ImportExportFileFormat
    {
        JSON,
        ZIP,
        LIST
    };

    /// <summary>
    /// Risultato della lettura
    /// </summary>
    public enum ReadResult
    {
        Success,
        Failed,
        SuccessWithWarning
    }

    /// <summary>
    /// Password manager
    /// </summary>
    public class PasswordManager
    {
        private const string LineBreak = "\r\n";

        // Nomi dei file della lista della password
        private const string LocalPwdFileName = "pwd.list";
        private const string CloudPwdFileName = "pwdcloud.list";
        // Cartelle degli allegati delle password
        private const string LocalAttachmentFolder = "attachment";
        private const string CloudAttachmentFolder = "attachment.cloud";
        // Nome del file di registrazione dell'accesso tramite impronta digitale
        private const string FingerprintAccessFileName = "fingerprint.access";

        // Nomi dei tag per il file delle password
        private const string PwdlistTagName             = "[NOME]";
        private const string PwdlistTagUsername         = "[USERNAME]";
        private const string PwdlistTagEmail            = "[EMAIL]";
        private const string PwdlistTagPassword         = "[PWD]";
        private const string PwdlistTagSecurityQuestion = "[DOMANDA]";
        private const string PwdlistTagSecurityAnswer   = "[RISPOSTA]";
        private const string PwdlistTagNote             = "[NOTE]";
        private const string PwdlistTagUsernameLogin    = "[USERNAMELOGIN]";

        // Intestazioni principali del file delle password
        private const string VersionTag = "[VERSION]";
        private const string ContentTag = "[CONTENT]";
        private const string BoundTag = "[BOUND]";
        private const string PwdListTag = "[PWDLIST]";
        private const string TwoFATag = "[2FA]";
        
        // Percorso del file locale della lista delle password
        private readonly string LocalPwdFolder;
        private string LocalPwdPath { get => Path.Combine(LocalPwdFolder, LocalPwdFileName); }
        private string CloudPwdPath { get => Path.Combine(LocalPwdFolder, CloudPwdFileName); }
        private string LocalAttachmenPath { get => Path.Combine(LocalPwdFolder, LocalAttachmentFolder); }
        private string CloudAttachmenPath { get => Path.Combine(LocalPwdFolder, CloudAttachmentFolder); }
        private string FingerprintAccessFilePath { get => Path.Combine(LocalPwdFolder, FingerprintAccessFileName); }

        private bool FromCloud;
        public string PwdFileNameInCloud { get => CloudPwdFileName; }

        public string PwdFileNameInLocal { get => LocalPwdFileName; }

        // Lista di password
        private readonly ObservablePwdList PasswordsList = null;
        // Helper per la decodifica/codifica delle password
        private readonly EncDecHelper EncryptDecryt = null;
        // Informazioni di accesso con autenticazione a due fattori
        public TwoFAInfo Access2FA { get; private set; }

        /// <summary>
        /// Password di accesso alla lista delle password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Vale true se deve essere fatto accesso alla lista delle password
        /// su Cloud.
        /// </summary>
        public bool Cloud { get => FromCloud; set => SetCloud(value); }

        // Valore corrente del flag Cloud al momento del caricamento della lista delle password
        private bool CurrentPasswordListCloud { get; set; }

        /// <summary>
        /// Numero di elementi nella lista delle password
        /// </summary>
        public int PasswordsCount { get => PasswordsList.Count; }

        /// <summary>
        /// Prima password della lista
        /// </summary>
        public PwdListItem FirstPassword
        {
            get
            {
                if (PasswordsCount == 0)
                    return null;
                return PasswordsList.First();
            }
        }

        /// <summary>
        /// Ultima password della lista
        /// </summary>
        public PwdListItem LastPassword
        {
            get
            {
                if (PasswordsCount == 0)
                    return null;
                return PasswordsList.Last();
            }
        }

        /// <summary>
        /// Delegato per le funzioni che permettono di gestire l'avanzamento di una
        /// progress bar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="step">avanzamento</param>
        public delegate void UpdateProgressEventHandler(object sender, EventArgs e, double step);

        public PasswordManager()
        {
            Cloud = false;
            CurrentPasswordListCloud = false;

            LocalPwdFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // Crea la cartella per gli allegati, se non esiste
            Directory.CreateDirectory(LocalAttachmenPath);
            Directory.CreateDirectory(CloudAttachmenPath);

            PasswordsList = new ObservablePwdList();
            PasswordsList.OnAdd += OnAddPassword;
            PasswordsList.OnClear += OnClearPasswordsList;
            PasswordsList.OnRemove += OnRemovePassword;

            EncryptDecryt = new EncDecHelper();

            Access2FA = new TwoFAInfo();

            Debug.WriteLine("LocalApplicationData location: " + LocalPwdFolder);
        }

        private void SetCloud(bool value)
        {
            bool loadStats = FromCloud != value; 
            FromCloud = value;
            if (loadStats)
                App.Statistic.SyncStatistics().ConfigureAwait(true);
        }

        private void OnClearPasswordsList(object sender, EventArgs e)
        {
            App.Statistic.ResetPasswordData();
        }

        private void OnAddPassword(object sender, EventArgs e, PwdListItem item)
        {
            App.Statistic.RegisterPasswordInsert(item);
        }

        private void OnRemovePassword(object sender, EventArgs e, PwdListItem item)
        {
            App.Statistic.RegisterPasswordRemove(item);
        }

        /// <summary>
        /// Aggiorna i file delle password cambiando la password di accesso.
        /// </summary>
        /// <param name="oldPassword">Password di accesso corrente</param>
        /// <param name="newPassword">Nuova password di accesso</param>
        /// <returns>Tipo enumerato che identifica l'esito della lettura del file delle password.
        /// ATTENZIONE: la funzione non restituisce mai ReadResult.Failed. Nel caso la master password sia sbagliata,
        /// la funzione solleverà una eccezione.</returns>
        public async Task<ReadResult> UpdateLogin(string oldPassword, string newPassword)
        {
            string currentPassword = Password;

            try
            {
                Password = oldPassword;
                ReadResult result = await OpenPasswordFile();
                if (result == ReadResult.Failed)
                    throw new Exception(AppResources.errWrongPassword);                

                // Prima di salvare, ricodifica gli allegati (così sono già pronti per l'upload su Cloud)
                string cloudAttFolder = GetAttachmentFolderPath();
                string[] fileList = Directory.GetFiles(cloudAttFolder);
                foreach (string file in fileList)
                {
                    UpdateLoginAttachment(file, oldPassword, newPassword);
                }

                Password = newPassword;
                await SavePasswordFile();

                return result;
            }
            catch (Exception e)
            {
                Debug.WriteLine("UpdateLogin failed. Error: " + e.Message);
                Password = currentPassword;

                // Ripristina gli allegati con quelli con suffisso "_old"
                string cloudAttFolder = GetAttachmentFolderPath();
                string[] fileList = Directory.GetFiles(cloudAttFolder);
                foreach (string file in fileList)
                {
                    if (file.EndsWith("_old"))
                        Utility.MoveFile(file, file.Substring(0, file.Length - 4), true);
                }

                throw;
            }
        }

        private void UpdateLoginAttachment(string file, string oldPassword, string newPassword)
        {
            // Effettua una copia di backup
            string ext = Path.GetExtension(file);
            if (ext.EndsWith("_old"))
                return;

            string backupFile = file.Replace(ext, ext + "_old");
            File.Copy(file, backupFile, true);

            string jsonContentText = File.ReadAllText(file, System.Text.Encoding.UTF8);
            EncryptDecryt.PasswordString = oldPassword;
            string fileContent = EncryptDecryt.AESDecrypt(jsonContentText, out bool warning);
            if (warning)
            {
                Debug.Write(string.Format("UpdateLoginAttachment: file {0} seems to be corrupted. App has tried to recover the data.", file));
            }
            EncryptDecryt.PasswordString = newPassword;
            fileContent = EncryptDecryt.AESEncrypt(fileContent);
            File.WriteAllText(file, fileContent, System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// Effettua il backup dei file delle password, allegati compresi.
        /// </summary>
        /// <returns>Restituisce l'oggetto per la gestione del bakcup</returns>
        public BackupManager Backup()
        {
            // Rinomina il file con un eventuale suffisso, compresa la cartella degli allegati
            BackupManager backupManager = new BackupManager();
            backupManager.AddFile(GetPasswordFilePath());
            backupManager.AddFolder(GetAttachmentFolderPath());
            backupManager.Backup();
            return backupManager;
        }

        /// <summary>
        /// Scarica gli allegati presenti sul Cloud
        /// </summary>
        /// <returns></returns>
        public async Task DownloadAttachmentFromCloud()
        {
            if (!App.CloudConnector.IsLoggedIn())
            {
                Debug.WriteLine("DownloadAttachmentFromCloud: user is not logged in");
                return;
            }

            // Crea la cartella se non esiste
            Directory.CreateDirectory(GetAttachmentFolderPath());

            // Scorre tutti le password alla ricerca degli allegati
            string localAttachmentFolder = GetAttachmentFolderPath();
            List<PwdListItem> pwdList = await GetPasswordList();
            foreach (PwdListItem pwd in pwdList)
            {
                if (pwd.Attachments.Count > 0)
                {
                    // Per ogni allegato
                    foreach (PwdAttachment attachment in pwd.Attachments)
                    {
                        string localAttachmentPath = Path.Combine(localAttachmentFolder, attachment.Filename);
                        if (File.Exists(localAttachmentPath))
                        {
                            try
                            {
                                string cloudPath = Path.Combine(CloudAttachmentFolder, attachment.Filename);
                                Cloud.CloudFileStatus status = await App.CloudConnector.GetFile(cloudPath, localAttachmentPath);
                                if (status == PwdCrypter.Cloud.CloudFileStatus.FileDownloadError)                                
                                    throw new Exception(string.Format("Error occurred during the download of the file {0} from Cloud", attachment.Filename));
                            }
                            catch(Exception ex)
                            {
                                Debug.WriteLine("DownloadAttachmentFromCloud: {0}", ex.Message);
                                App.Logger.Debug(string.Format("DownloadAttachmentFromCloud: {0}", ex.Message));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Verifica se il password manager è stato inizializzato.
        /// In particolare, verifica se è già presente il file della lista delle password.
        /// </summary>
        /// <returns>True se il password manager è stato inizializzato, false altrimenti</returns>
        public bool IsInitialized()
        {
            bool init = File.Exists(LocalPwdPath);
            return init;
        }

        /// <summary>
        /// Restituisce il percorso del file delle password
        /// </summary>
        /// <returns>Percorso del file delle password</returns>
        public string GetPasswordFilePath()
        {
            return Cloud ? CloudPwdPath : LocalPwdPath;
        }

        /// <summary>
        /// Restituisce il percorso della cartella degli allegati
        /// delle password
        /// </summary>
        /// <returns></returns>
        public string GetAttachmentFolderPath()
        {
            return Cloud ? CloudAttachmenPath : LocalAttachmenPath;
        }

        async private Task<ReadResult> ReadPasswordFromBuffer(byte[] buffer, Func<string, bool, Task<bool>> readCallback)
        {
            ReadResult result;
            try
            {
                string decodedContent = "";
                byte[] newData = Utility.SanitizeData(buffer);
                string content = System.Text.Encoding.UTF8.GetString(newData);
                
                if (content.IndexOf(ContentTag) < 0)
                    throw new Exception(AppResources.errInvalidFile);

                string pwdContent = content.Substring(content.IndexOf(ContentTag) + ContentTag.Length);
                EncryptDecryt.PasswordString = Password;
                decodedContent = EncryptDecryt.AESDecrypt(pwdContent, out bool warning);
                result = warning ? ReadResult.SuccessWithWarning : ReadResult.Success;

                if (!decodedContent.StartsWith(PwdListTag) && !decodedContent.StartsWith(BoundTag))
                {
                    Debug.WriteLine("The password is wrong");
                    return ReadResult.Failed;
                }

                if (readCallback != null)
                {                    
                    if (await readCallback.Invoke(decodedContent, false))
                        await LoadPasswordList(decodedContent);
                }
                else
                    await LoadPasswordList(decodedContent);

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to read the password list. Error: " + ex.Message);
                throw;
            }            
        }

        /// <summary>
        /// Apre il file della lista delle password.
        /// Se si sta aprendo un file della vecchia App, lo ricodifica.
        /// </summary>
        /// <returns>Tipo enumerato che indentifica l'esito dell'operazione.</returns>
        async public Task<ReadResult> OpenPasswordFile()
        {
            CurrentPasswordListCloud = Cloud;

            try
            {
                byte[] buffer = File.ReadAllBytes(GetPasswordFilePath());
                ReadResult result = await ReadPasswordFromBuffer(buffer, async (content, reCode) =>
                {
                    if (reCode)
                    {
                        if (string.IsNullOrEmpty(content))
                            return false;

                        // Effettua una copia di sicurezza del file
                        File.Copy(GetPasswordFilePath(), GetPasswordFilePath() + ".tmp", true);
                        try
                        {
                            string fileContent = VersionTag + App.Version + ContentTag;
                            EncryptDecryt.PasswordString = Password;
                            fileContent += EncryptDecryt.AESEncrypt(content);

                            File.WriteAllText(GetPasswordFilePath(), fileContent, System.Text.Encoding.UTF8);
                        }
                        catch (Exception Ex)
                        {
                            // Ripristina la copia, per sicurezza
                            File.Copy(GetPasswordFilePath() + ".tmp", GetPasswordFilePath(), true);
                            Debug.WriteLine("RecodePassword: unable to save the recoded file. Error: " + Ex.Message);
                            return false;
                        }
                        finally
                        {
                            // Elimina il file temporaneo
                            File.Delete(GetPasswordFilePath() + ".tmp");
                        }

                        // Ricarica il file su Cloud
                        if (Cloud)
                        {
                            try
                            {
                                await App.CloudConnector.UploadFile(GetPasswordFilePath(), CloudPwdFileName);
                            }
                            catch(Exception e)
                            {
                                Debug.WriteLine("Upload {0} to Cloud failed with error: {1}", GetPasswordFilePath(), e.Message);
                            }
                        }
                    }

                    return true;
                });
                
                return result;
            }
            catch (FileNotFoundException ex)
            {
                // Il file può non esistere
                PasswordsList.Clear();
                Access2FA.Clear();
                Debug.WriteLine("OpenPasswordFile: file not found. Message: " + ex.Message);
                return ReadResult.Success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to open the password list. Error: " + ex.Message);
                return ReadResult.Failed;
            }
        }

        private void SaveAttachmentFromByte(byte[] buffer, string filename)
        {
            JObject json = new JObject
            {
                { "md5", new JValue(EncDecHelper.MD5(buffer)) },
                { "base64", new JValue(Convert.ToBase64String(buffer)) },
                { "bytecount", new JValue(buffer.Length) }
            };

            EncryptDecryt.PasswordString = Password;
            string fileContent = EncryptDecryt.AESEncrypt(json.ToString());
            string destPath = Path.Combine(GetAttachmentFolderPath(), filename);
            File.WriteAllText(destPath, fileContent, System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// Salva un allegato in archivio
        /// </summary>
        /// <param name="fileStream">Stream con i byte dell'allegato da salvare</param>
        /// <param name="updateProgressEvent">Evento di notifica di avanzamento dell'operazione di salvataggio</param>
        /// <returns>Restituisce il nome del file dell'allegato</returns>
        public async Task<string> SaveAttachment(Stream fileStream, UpdateProgressEventHandler updateProgressEvent)
        {
            const int MaxBlock = 1024 * 1024;

            if (fileStream.Length == 0)
                throw new Exception("File is empty");

            Guid guidGenerator = Guid.NewGuid();
            string filename = guidGenerator.ToString("D") + ".att";

            int posBuffer = 0;
            int stepPhases = Math.Max(1, (int)(fileStream.Length / MaxBlock));
            double step = 0.8 / stepPhases, progress;
            byte[] buffer = new byte[fileStream.Length];
            do
            {
                int count = Math.Min(MaxBlock, buffer.Length);
                if (posBuffer + count > fileStream.Length)
                    count = (int)fileStream.Length - posBuffer;
                int read = await fileStream.ReadAsync(buffer, posBuffer, count);
                posBuffer += read;

                // Evento della progress bar
                progress = (fileStream.Position / fileStream.Length) * 0.8;
                updateProgressEvent?.Invoke(this, null, progress);

            } while (fileStream.Position < fileStream.Length);

            SaveAttachmentFromByte(buffer, filename);
            updateProgressEvent?.Invoke(this, null, progress += step);

            // Salva anche su Cloud
            string sourceFile = Path.Combine(GetAttachmentFolderPath(), filename);
            if (Cloud && File.Exists(sourceFile))
            {
                // Verifica se la cartella esiste già su Cloud
                Cloud.CloudFileStatus status = await App.CloudConnector.FolderExists(CloudAttachmentFolder);
                if (status == PwdCrypter.Cloud.CloudFileStatus.FileNotFound)
                    await App.CloudConnector.CreateFolder(CloudAttachmentFolder);   // Crea la cartella                
                else if (status == PwdCrypter.Cloud.CloudFileStatus.FileError)                
                    throw new Exception("Destination folder not available");                

                string fileOnCloud = Path.Combine(CloudAttachmentFolder, filename);
                if (!await App.CloudConnector.UploadFile(sourceFile, fileOnCloud))
                    throw new Exception("Unable to save the file " + filename + " in the Cloud");
            }

            // Evento della progress bar
            updateProgressEvent?.Invoke(this, null, 1.0);

            return filename;   
        }

        /// <summary>
        /// Elimina un file allegato
        /// </summary>
        /// <param name="filename">Nome del file allegato da eliminare</param>
        /// <returns></returns>
        public async Task RemoveAttachment(string filename)
        {
            string filePath = Path.Combine(GetAttachmentFolderPath(), filename);

            if (File.Exists(filePath))
                File.Delete(filePath);

            if (Cloud)
            {
                string fileOnCloud = Path.Combine(CloudAttachmentFolder, filename);

                Cloud.CloudFileStatus fileStatus = await App.CloudConnector.FileExists(fileOnCloud);
                if (fileStatus == PwdCrypter.Cloud.CloudFileStatus.FileFound)
                    await App.CloudConnector.DeleteFile(fileOnCloud);
                else if (fileStatus == PwdCrypter.Cloud.CloudFileStatus.FileNotFound)
                {
                    Debug.WriteLine("File " + filename + " not found in the Cloud");
                }
                else if (fileStatus == PwdCrypter.Cloud.CloudFileStatus.FileError)
                {
                    throw new Exception("Unable to find the file " + filename + "in the Cloud");
                }
            }
        }        

        private string GetContentWithBound(string tag, string bound, string content)
        {
            string data = tag + LineBreak +
                bound + LineBreak + content + LineBreak + bound;
            return data;
        }

        private byte[] SavePasswordListToBytes(bool attachmentIncluded)
        {
            JArray jsonContent = new JArray();
            foreach (PwdListItem item in PasswordsList)
                jsonContent.Add(item.AsJSON(attachmentIncluded));

            string data;
            string fileContent = VersionTag + App.Version + ContentTag;

            // Salvataggio informazioni sull'autenticazione a 2 fattori. E' necessaria la separazione
            // con bound separator
            if (Access2FA.IsAvailable)
            {
                string bound = Utility.ComputeBound();
                data = BoundTag + bound + LineBreak;
                data += GetContentWithBound(PwdListTag, bound, jsonContent.ToString());
                data += LineBreak + GetContentWithBound(TwoFATag, bound, Access2FA.AsJSON().ToString());
            }
            else
                data = PwdListTag + LineBreak + jsonContent.ToString();

            EncryptDecryt.PasswordString = Password;
            fileContent += EncryptDecryt.AESEncrypt(data);

            return System.Text.Encoding.UTF8.GetBytes(fileContent);
        }

        /// <summary>
        /// Salva la lista delle password
        /// </summary>
        /// <param name="bUploadAttachments">Passare true per eventualmente aggiornare gli allegati su Cloud, false altrimenti</param>        
        public async Task SavePasswordFile(bool bUploadAttachments=true)
        {
            byte[] data = SavePasswordListToBytes(false);
            string fileContent = System.Text.Encoding.UTF8.GetString(data);
            File.WriteAllText(GetPasswordFilePath(), fileContent, System.Text.Encoding.UTF8);

            if (Cloud)
            {
                if (!await App.CloudConnector.UploadFile(GetPasswordFilePath(), CloudPwdFileName))
                    throw new Exception("Upload failed");
                // Carica su cloud anche gli allegati
                if (bUploadAttachments)
                    await UploadAttachments();
            }

            // Aggiorna le informazioni statistiche sulle password
            await App.Statistic.UpdateStatistics();
        }

        /// <summary>
        /// Aggiorna il file locale della lista delle password.
        /// ATTENZIONE: se si ha aperto il file della lista su Cloud, la funzione aprirà il file locale,
        /// lo salverà e infine riaprirà la copia del file su Cloud.
        /// </summary>
        /// <returns></returns>
        public async Task UpdateLocalPasswordFile()
        {
            bool prevCloud = Cloud;
            try
            {
                // Mi assicuro di salvare il file locale
                if (Cloud)
                {
                    Cloud = false;
                    await OpenPasswordFile();
                }
                await SavePasswordFile(false);
            }
            finally
            {
                if (prevCloud)
                {
                    Debug.WriteLine("UpdateLocalPasswordFile has been called with active Cloud");
                    App.Logger.Inform("UpdateLocalPasswordFile has been called with active Cloud");

                    Cloud = true;
                    await OpenPasswordFile();
                }
            }
        }

        /// <summary>
        /// Cancella i file locali provenienti dal cloud
        /// </summary>
        public void DeleteLocalFileFromCloud()
        {
            try
            {
                if (File.Exists(CloudPwdPath))
                    File.Delete(CloudPwdPath);
                if (Directory.Exists(CloudAttachmenPath))
                    Directory.Delete(CloudAttachmenPath, true);
            }
            catch(Exception e)
            {
                Debug.WriteLine(string.Format("DeleteLocalFileFromCloud fails with error: {0}", e.Message));
                App.Logger.Error(string.Format("DeleteLocalFileFromCloud fails with error: {0}", e.Message));
            }
        }

        /// <summary>
        /// Salva la configurazione di accesso per l'autenticazione a due fattori
        /// </summary>
        /// <param name="secret">Codice segreto</param>
        /// <param name="backupCode">Codice di backup</param>
        public void Save2FAData(string secret, string backupCode)
        {
            Access2FA.Code = secret;
            Access2FA.BackupCode = backupCode;            
        }
        /// <summary>
        /// Salva la configurazione di accesso per l'autenticazione a due fattori
        /// </summary>
        /// <param name="info">Oggetto con le informazioni da salvare</param>
        public void Save2FAData(TwoFAInfo info)
        {
            Access2FA.Code = info.Code;
            Access2FA.BackupCode = info.BackupCode;
        }

        /// <summary>
        /// Salva il file che identifica la configurazione di accesso tramite l'impronta digitale
        /// </summary>
        /// <param name="data">Dati firmati dall'impronta digitale</param>
        public void SaveFingerprintAccessFile(byte[] data)
        {
            File.WriteAllBytes(FingerprintAccessFilePath, data);
        }

        /// <summary>
        /// Legge il contenuto del file che identifica la configurazione di accesso
        /// con l'impront digitale.
        /// </summary>
        /// <returns>Dati firmati dall'impronta digitale</returns>
        public byte[] ReadFingerprintAccessFile()
        {
            try
            {
                return File.ReadAllBytes(FingerprintAccessFilePath);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(AppResources.errConfigFingerprint, ex.Message));
            }
        }

        /// <summary>
        /// Verifica se l'accesso con l'impronta digitale è stato configurato in precedenza.
        /// </summary>
        /// <returns>True se l'accesso con l'impronta digitale è stato configurato, false altrimenti</returns>
        public bool IsFingerprintAccessConfigured()
        {
            return File.Exists(FingerprintAccessFilePath);
        }

        /// <summary>
        /// Verifica se l'accesso con autenticazione a due fattori è stato configurato in precedenza.
        /// </summary>
        /// <returns>True se l'accesso con autenticazione a due fattori è stato configurato, false altrimenti</returns>
        public bool Is2FAAccessConfigured()
        {
            return Access2FA.IsAvailable;
        }

        /// <summary>
        /// Cancella il file di configurazione per l'accesso con l'impronta digitale.
        /// </summary>
        public void RemoveFingerprintAccessFile()
        {
            File.Delete(FingerprintAccessFilePath);
            App.Settings.Access = SecurityAccess.MasterPassword;
            App.Settings.WriteSettings();            
        }

        /// <summary>
        /// Aggiunge una nuova password alla lista
        /// </summary>
        /// <param name="item">Oggetto con le informazioni sulla nuova password</param>
        public void AddPassword(PwdListItem item)
        {
            if (item == null)
            {
                Debug.WriteLine("AddPassword failed. Password object is null");
                return;
            }

            item.Id = GetPasswordId(item);
            PasswordsList.Add(item);
        }

        /// <summary>
        /// Rimuove la password dalla lista
        /// </summary>
        /// <param name="item">Oggetto della password da rimuovere</param>
        public async Task RemovePassword(PwdListItem item)
        {
            if (item == null)
            {
                Debug.WriteLine("RemovePassword failed. Password object is null");
                return;
            }

            // Elimina gli allegati della password
            foreach (PwdAttachment pwdAttachment in item.Attachments)
            {
                if (pwdAttachment.Filename.Length > 0)
                    await RemoveAttachment(pwdAttachment.Filename);
            }

            // Rimuove ora la password dalla lista
            PasswordsList.Remove(item);
        }

        /// <summary>
        /// Carica su Cloud la lista delle password e i suoi allegati
        /// </summary>
        /// <returns></returns>
        public async Task UploadPwdToCloud()
        {
            bool oldCloud = Cloud;
            try
            {
                Cloud = false;
                string localFileName = GetPasswordFilePath();
                string pwdfileNameInCloud = PwdFileNameInCloud;
                string localAttachmentFolder = GetAttachmentFolderPath();

                if (!await App.CloudConnector.UploadFile(localFileName, pwdfileNameInCloud))
                    throw new Exception("Upload failed");

                // Carica gli allegati su Cloud
                await UploadAttachments();

                Cloud = true;

                // Tengo in locale una copia del file su cloud
                string cloudFileName = GetPasswordFilePath();
                File.Copy(localFileName, cloudFileName, true);

                // Svuota la cartella del Cloud
                string cloudAttachmentFolder = GetAttachmentFolderPath();
                if (Directory.Exists(cloudAttachmentFolder))
                    Directory.Delete(cloudAttachmentFolder, true);
                Directory.CreateDirectory(cloudAttachmentFolder);

                // Copia anche la cartella degli allegati
                string[] files = Directory.GetFiles(localAttachmentFolder);
                foreach (string filePath in files)
                {
                    File.Copy(filePath, Path.Combine(cloudAttachmentFolder, Path.GetFileName(filePath)), true);
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("UploadToCloud error: " + Ex.Message);
                throw;
            }
            finally
            {
                Cloud = oldCloud;
            }
        }

        /// <summary>
        /// Carica su Cloud gli allegati delle password
        /// </summary>
        /// <returns></returns>
        private async Task UploadAttachments()
        {
            if (!App.CloudConnector.IsLoggedIn())
            {
                Debug.WriteLine("UploadAttachments: user is not logged in");
                return;
            }

            try
            {
                // Verifica se la cartella esiste già su Cloud
                Cloud.CloudFileStatus status = await App.CloudConnector.FolderExists(CloudAttachmentFolder);
                if (status == PwdCrypter.Cloud.CloudFileStatus.FileNotFound)
                    await App.CloudConnector.CreateFolder(CloudAttachmentFolder);   // Crea la cartella
                else if (status == PwdCrypter.Cloud.CloudFileStatus.FileError)                
                    throw new Exception("Destination folder not available");                

                string cloudAttFolder = GetAttachmentFolderPath();
                string[] fileList = Directory.GetFiles(cloudAttFolder);
                foreach (string file in fileList)
                {
                    if (!await App.CloudConnector.UploadFile(file, Path.Combine(CloudAttachmentFolder, Path.GetFileName(file))))
                        throw new Exception(string.Format("Upload of the file {0} failed", file));
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("UploadAttachments error: " + Ex.Message);
                throw new Exception(AppResources.errCloudAttachmentsUpload + " " + Ex.Message);
            }
        }

        /// <summary>
        /// Verifica l'esistenza del file della sincronizzazione con il Cloud.
        /// ATTENZIONE: la funzione non fa accesso al Cloud, verifica la presenza locale del file.
        /// </summary>
        /// <returns>True se il file esiste, false altrimenti</returns>
        public bool CloudFileExists()
        {
            return File.Exists(CloudPwdPath);
        }

        /// <summary>
        /// Crea un file delle password (vuoto)
        /// </summary>
        public void CreatePasswordFile()
        {
            if (Password.Trim().Length == 0)
                throw new Exception(AppResources.errInvalidPassword);

            string fileContent = VersionTag + App.Version + ContentTag;
            string content = PwdListTag;

            EncryptDecryt.PasswordString = Password;
            fileContent += EncryptDecryt.AESEncrypt(content);

            StreamWriter streamWriter = File.CreateText(GetPasswordFilePath());
            streamWriter.Write(fileContent);
            streamWriter.Close();            
        }

        /// <summary>
        /// Ricodifica il file delle password e restituisce il contenuto stesso
        /// del file.
        /// </summary>
        /// <param name="content">Contenuto del file (lista delle password)</param>
        /// <returns>Contenuto del file delle password, convertito nel formato utilizzato dall'App</returns>
        private string RecodePassword(string content)
        {              
            try
            {
                // Trasformazione in JSON
                JArray jsonContent = new JArray();

                string[] tags = 
                {
                    PwdlistTagName + "=",
                    PwdlistTagUsername + "=",
                    PwdlistTagEmail + "=",
                    PwdlistTagPassword + "=",
                    PwdlistTagSecurityQuestion + "=",
                    PwdlistTagSecurityAnswer + "=",
                    PwdlistTagNote + "=",
                    PwdlistTagUsernameLogin + "="
                };

                string line = "";
                int endLine = 0;
                int posLine = 0;

                do
                {
                    line = "";
                    posLine = content.IndexOf(PwdlistTagName + "=", endLine);
                    if (posLine > -1)
                    {
                        endLine = content.IndexOf(PwdlistTagName + "=", posLine + 1);
                        if (endLine == -1)
                            endLine = content.Length - 1;
                        line = content.Substring(posLine, endLine - posLine);
                    }
                    if (line.Length == 0)
                        break;

                    PwdListItem pwdItem = new PwdListItem();
                    int pos = -1;
                    for (int i = 0; i < tags.Length; i++)
                    {
                        string tag = tags[i];

                        int currentTagEnd = -1;
                        int currentTagPos = line.IndexOf(tag, pos + 1);
                        if (currentTagPos == -1)
                            break;
                        if (i == tags.Length - 1)
                            currentTagEnd = line.Length - 1;
                        else
                        {
                            currentTagEnd = line.IndexOf(tags[i + 1], currentTagPos);
                            if (currentTagEnd == -1)
                                currentTagEnd = line.Length - 1;
                        }
                        pos = currentTagEnd - 1;

                        string value = "";
                        int valueEnd = currentTagEnd - currentTagPos - tag.Length;
                        if (valueEnd >= 0)
                            value = line.Substring(currentTagPos + tag.Length, valueEnd);

                        switch (i)
                        {
                            case 0:
                                {
                                    pwdItem.Name = value;
                                    pwdItem.Id = GetPasswordId(pwdItem);
                                }
                                break;
                            case 1: pwdItem.Username = value; break;
                            case 2: pwdItem.Email = value; break;
                            case 3: pwdItem.Password = value; break;
                            case 4: pwdItem.SecurityQuestion = value; break;
                            case 5: pwdItem.SecurityAnswer = value; break;
                            case 6: pwdItem.Note = value; break;
                            case 7:
                                {
                                    if (Utility.TryParseEnum(value, UsernameLoginOption.Username, out UsernameLoginOption resultUsernameLoginOption))
                                        pwdItem.LoginOption = resultUsernameLoginOption;
                                    else
                                    {
                                        if (pwdItem.Username.Trim().Length > 0)
                                            pwdItem.LoginOption = UsernameLoginOption.Username;
                                        else if (pwdItem.Email.Trim().Length > 0)
                                            pwdItem.LoginOption = UsernameLoginOption.Email;
                                        else
                                            pwdItem.LoginOption = UsernameLoginOption.Username;
                                    }
                                }
                                break;
                            default:
                                throw new Exception($"Recoding passwords list file: unknown tag {tag}");
                        }
                    }

                    jsonContent.Add(pwdItem.AsJSON());
                }
                while (line.Length > 0);

                string jsonContentText = PwdListTag + LineBreak + jsonContent.ToString();
                return jsonContentText;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("RecodePassword: unable to recode the file. Error: " + ex.Message);
            }

            return "";
        }

        /// <summary>
        /// Restituisce l'id per identificare univocamente la password.
        /// Chiamare la funzione per ottenere l'id da assegnare alla password, al momento della sua creazione
        /// per l'inserimento in lista.
        /// </summary>
        /// <param name="item">Oggetto della password di cui calcolare l'id</param>
        /// <returns>Id da assegnare alla password</returns>
        private string GetPasswordId(PwdListItem item)
        {
            return EncDecHelper.SHA256(item.Name + DateTime.Now.ToString("yyyyMMddTHH:mm:ssZ"));
        }

        /// <summary>
        /// Restituisce la lista delle password.
        /// </summary>
        /// <param name="filter">Filtro opzionale per la lista delle password</param>
        /// <returns>Lista delle password</returns>
        public async Task<List<PwdListItem>> GetPasswordList(FilterOption filter = null)
        {
            // Se è cambiato il file di accesso delle password, ricarica la lista
            if ((filter == null && CurrentPasswordListCloud != Cloud) || 
                (filter != null && CurrentPasswordListCloud != filter.InCloud))
            {
                Cloud = filter != null ? filter.InCloud : Cloud;
                ReadResult result = await OpenPasswordFile();
                if (result == ReadResult.Failed)
                    PasswordsList.Clear();
                else if (result == ReadResult.SuccessWithWarning)
                {
                    Debug.WriteLine(AppResources.warnFileCorruptedButRecover);                    
                }
            }

            if (filter == null)
                return PasswordsList;

            // Filtro
            List<PwdListItem> list = PasswordsList.Where((PwdListItem item) => 
            {
                return IsMatchFilter(item, filter);
            }).ToList();

            // Ordinamento
            if (filter.PassowrdOrder == PwdOrder.Name)
                list.Sort(PasswordComparer.ComparePasswordByName);
            else if (filter.PassowrdOrder == PwdOrder.AccountType)
                list.Sort(PasswordComparer.ComparePasswordByAccountType);

            return list;
        }

        /// <summary>
        /// Verifica se la password soddisfa il filtro
        /// </summary>
        /// <param name="item">Oggetto che rappresenta la password</param>
        /// <param name="filter">Oggetto che contiene i criteri di ricerca</param>
        /// <returns>Restituisce true se la password soddisfa il criterio di ricerca, false altrimenti</returns>
        private bool IsMatchFilter(PwdListItem item, FilterOption filter)
        {
            if (filter.AccountType.HasValue && item.AccountOption != filter.AccountType.Value)
                return false;
            if (!filter.AllFields)
                return filter.Criteria.Trim().Length == 0 || item.Name.ToLower().Contains(filter.Criteria.Trim().ToLower());
            else if (filter.Criteria.Trim().Length > 0)
            {
                string strSearch = filter.Criteria.Trim().ToLower();
                return item.Name.ToLower().Contains(strSearch) ||
                        item.Email.ToLower().Contains(strSearch) ||
                        item.Note.ToLower().Contains(strSearch) ||
                        item.SecurityAnswer.ToLower().Contains(strSearch) ||
                        item.SecurityQuestion.ToLower().Contains(strSearch) ||
                        item.Username.ToLower().Contains(strSearch);
            }
            return true;
        }

        /// <summary>
        /// Esportazione della lista delle password, compresi gli allegati (a seconda del formato).
        /// </summary>
        /// <param name="fileFormat">Formato del file</param>
        /// <param name="streamData">Stream di output</param>
        /// <returns>Restituisce il numero di byte nello stream</returns>
        public async Task<long> ExportPasswordListToBytes(ImportExportFileFormat fileFormat, Stream streamData)
        {
            if (fileFormat == ImportExportFileFormat.LIST)
            {
                byte[] data = SavePasswordListToBytes(true);
                await streamData.WriteAsync(data, 0, data.Length);
                return streamData.Length;
            }
            else if (fileFormat == ImportExportFileFormat.ZIP)
            {
                ICrossPlatformSpecialFolder specialFolder = DependencyService.Get<ICrossPlatformSpecialFolder>();
                string tempFolder = specialFolder.GetTemporaryFolder();
                string tempExport = Path.Combine(tempFolder, DateTime.Now.ToString("ddMMyyyy_HHmmss"));
                string tempZIPFolder = Path.Combine(tempExport, "zip");
                string tempZIPAttFolder = Path.Combine(tempZIPFolder, "attachments");
                Directory.CreateDirectory(tempZIPAttFolder);

                // ZIP con il file delle password in formato JSON, più gli allegati
                try
                {
                    JArray jsonContent = new JArray();
                    foreach (PwdListItem item in PasswordsList)
                    {
                        jsonContent.Add(item.AsJSON());
                        if (item.Attachments.Count > 0)
                        {
                            foreach (PwdAttachment attachment in item.Attachments)
                            {
                                byte[] buffer = ReadAttachment(attachment);
                                int suffixCounter = 0;
                                string attFilePath = Path.Combine(tempZIPAttFolder, attachment.OriginalFilename);
                                string startFilePath = attFilePath;
                                while (File.Exists(attFilePath))
                                {
                                    suffixCounter++;
                                    attFilePath = startFilePath.Replace(Path.GetExtension(startFilePath), "_" + suffixCounter + "." + Path.GetExtension(startFilePath).Substring(1));
                                }
                                File.WriteAllBytes(attFilePath, buffer);                                    

                                // Aggiorna il nome del file nel JSON
                                if (suffixCounter > 0)
                                {
                                    JObject lastJson = jsonContent[jsonContent.Count - 1] as JObject;
                                    JArray attsJson = lastJson.GetValue(nameof(item.Attachments)) as JArray;
                                    foreach (JToken jToken in attsJson)
                                    {
                                        JObject attJson = jToken as JObject;
                                        if (attJson.ContainsKey(nameof(attachment.AttachmentFile)))
                                        {
                                            JObject attFile = attJson.GetValue(nameof(attachment.AttachmentFile)) as JObject;
                                            string filename = attFile.GetValue(nameof(attachment.AttachmentFile.OriginalFileName)).ToString();
                                            if (filename.CompareTo(Path.GetFileName(startFilePath)) == 0)
                                            {
                                                attFile[nameof(attachment.AttachmentFile.OriginalFileName)] = new JValue(Path.GetFileName(attFilePath));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    string pwdFilePath = Path.Combine(tempZIPFolder, "pwd.json");
                    File.WriteAllText(pwdFilePath, jsonContent.ToString(), System.Text.Encoding.UTF8);
                    await ArchiveUtility.ZipArchive(streamData, tempZIPFolder);
                }
                catch (Exception Ex)
                {
                    Debug.WriteLine("ExportPasswordListToFile failed. Error: " + Ex.Message);
                    throw new Exception("Error occurred during the export to ZIP file. Error: " + Ex.Message);
                }
                finally
                {
                    if (Directory.Exists(tempExport))
                        Directory.Delete(tempExport, true);
                }

                return streamData.Length;
            }
            else if (fileFormat == ImportExportFileFormat.JSON)
            {
                JArray jsonContent = new JArray();
                foreach (PwdListItem item in PasswordsList)
                {
                    jsonContent.Add(item.AsJSON());

                    // Rimuove le informazioni sugli allegati
                    JObject lastJson = jsonContent[jsonContent.Count - 1] as JObject;
                    if (lastJson.ContainsKey(nameof(item.Attachments)))
                        lastJson.Remove(nameof(item.Attachments));
                }

                string fileContent = jsonContent.ToString();
                byte[] data = Utility.GetBytesFromString(fileContent);
                await streamData.WriteAsync(data, 0, data.Length);
                return streamData.Length;
            }

            Debug.WriteLine("ExportPasswordListToFile: unknown format. Unable to export the password list.");
            throw new Exception("Unknown format");
        }

        /// <summary>
        /// Importazione della lista delle password.
        /// </summary>
        /// <param name="buffer">Buffer con i byte del file da importare</param>
        /// <param name="fileFormat">Formato del file che si sta importando</param>
        /// <returns>Tipo enumerato che identifica l'esito della lettura del file</returns>
        public async Task<ReadResult> ImportPasswordListFromBytes(byte[] buffer, ImportExportFileFormat fileFormat)
        {            
            ReadResult result = ReadResult.Failed;                       
            try
            {
                if (fileFormat == ImportExportFileFormat.LIST)
                {
                    // N.B.: questa funzione salva anche gli allegati su file
                    App.Statistic.Freeze = false;
                    App.Statistic.ResetData();
                    result = await ReadPasswordFromBuffer(buffer, null);
                    if (result == ReadResult.Failed)
                        throw new Exception(AppResources.errImportWrongPassword);                    
                }
                else if (fileFormat == ImportExportFileFormat.JSON)
                {
                    App.Statistic.Freeze = false;
                    App.Statistic.ResetData();

                    string content = PwdListTag + System.Text.Encoding.UTF8.GetString(buffer);
                    await LoadPasswordList(content);
                    result = ReadResult.Success;
                }
                else if (fileFormat == ImportExportFileFormat.ZIP)
                {
                    ICrossPlatformSpecialFolder specialFolder = DependencyService.Get<ICrossPlatformSpecialFolder>();
                    string tempFolder = specialFolder.GetTemporaryFolder();
                    string tempExport = Path.Combine(tempFolder, DateTime.Now.ToString("ddMMyyyy_HHmmss"));
                    string tempZIPFolder = Path.Combine(tempExport, "zip");
                    Directory.CreateDirectory(tempZIPFolder);

                    try
                    {
                        string zipFile = Path.Combine(tempExport, "temp.zip");
                        File.WriteAllBytes(zipFile, buffer);

                        App.Statistic.Freeze = false;
                        App.Statistic.ResetData();
                        
                        System.IO.Compression.ZipFile.ExtractToDirectory(zipFile, tempZIPFolder);
                        string content = PwdListTag + File.ReadAllText(Path.Combine(tempZIPFolder, "pwd.json"), System.Text.Encoding.UTF8);
                        await LoadPasswordList(content);

                        // Copia degli allegati
                        if (!Directory.Exists(GetAttachmentFolderPath()))
                            Directory.CreateDirectory(GetAttachmentFolderPath());
                        foreach (PwdListItem pwd in PasswordsList)
                        {
                            if (pwd.Attachments.Count > 0)
                            {
                                foreach (PwdAttachment attachment in pwd.Attachments)
                                {
                                    string attZIPFile = Path.Combine(Path.Combine(tempZIPFolder, "attachments"), attachment.OriginalFilename);
                                    if (File.Exists(attZIPFile))
                                    {
                                        byte[] attData = File.ReadAllBytes(attZIPFile);
                                        string md5 = EncDecHelper.MD5(attData);
                                        if (md5.ToLower().CompareTo(attachment.AttachmentFile.MD5.ToLower()) != 0)
                                        {
                                            Debug.WriteLine("ImportPasswordListFromBytes: MD5 test failed for " + attZIPFile);
                                            continue;
                                        }

                                        SaveAttachmentFromByte(attData, attachment.Filename);
                                    }
                                }
                            }
                        }

                        result = ReadResult.Success;
                    }
                    finally
                    {
                        if (Directory.Exists(tempExport))
                            Directory.Delete(tempExport, true);
                    }
                }
                else
                {
                    Debug.WriteLine("ImportPasswordListFromBytes: unknown format. Unable to import the password list.");
                    throw new Exception(AppResources.errUnknownFormat);
                }

                return result;
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("Unable to serialize the password list. Error: " + Ex.Message);
                throw;
            }                        
        }

        private byte[] ReadAttachment(PwdAttachment attachment)
        {
            string attachmentPath = Path.Combine(GetAttachmentFolderPath(), attachment.Filename);
            if (!File.Exists(attachmentPath))
                throw new Exception(AppResources.errFileNotFound);

            string jsonContentText = File.ReadAllText(attachmentPath, System.Text.Encoding.UTF8);
            EncryptDecryt.PasswordString = Password;
            string fileContent = EncryptDecryt.AESDecrypt(jsonContentText, out bool warning);
            if (warning)
            {
                Debug.Write(string.Format("ReadAttachment: file {0} seems to be corrupted. App has tried to recover the data.", attachmentPath));
            }

            JObject jObject = JObject.Parse(fileContent);
            int bytecount = int.Parse(jObject.GetValue("bytecount").ToString());
            string md5 = jObject.GetValue("md5").ToString();
            string base64 = jObject.GetValue("base64").ToString();
            byte[] buffer = Convert.FromBase64String(base64);

            // Verifiche di validità
            if (bytecount != buffer.Length)
                throw new Exception(AppResources.errCorruptedFile);
            if (string.Compare(md5.ToLower(), EncDecHelper.MD5(buffer).ToLower()) != 0)
                throw new Exception(AppResources.errCorruptedFile);

            return buffer;
        }

        /// <summary>
        /// La funzione permette di aprire il file allegato alla password
        /// </summary>
        /// <param name="attachment">Oggetto con le informazioni sull'allegato da scaricare</param>
        public async Task GetAttachment(PwdAttachment attachment)
        {
            byte[] buffer = ReadAttachment(attachment);
            ICrossPlatformSpecialFolder specialFolderHandler = DependencyService.Get<ICrossPlatformSpecialFolder>();
            string tmpFolder = specialFolderHandler.GetTemporaryFolder();
            string destDocPath = Path.Combine(tmpFolder, attachment.OriginalFilename);
            File.WriteAllBytes(destDocPath, buffer);
            await specialFolderHandler.OpenFileByDefaultApp(destDocPath);
        }

        public PwdListItem GetPasswordById(string id)
        {
            return PasswordsList.FindById(id);
        }        

        async private Task LoadPasswordList(string content)
        {
            PasswordsList.Clear();
            Access2FA.Clear();
            try
            {
                string bound = "", pwdContent = content;
                if (content.StartsWith(BoundTag))
                {
                    bound = content.Substring(BoundTag.Length, content.IndexOf(LineBreak) - BoundTag.Length);
                    pwdContent = pwdContent.Substring(BoundTag.Length + bound.Length + LineBreak.Length);
                }

                int posNext = -1;
                string toJson;
                if (string.IsNullOrEmpty(bound))
                    toJson = pwdContent.Substring(PwdListTag.Length);
                else
                    toJson = Utility.ExtractDataWithBound(PwdListTag, bound, pwdContent, out posNext);
                if (toJson.Length > 0)
                {
                    JArray jsonContent = JArray.Parse(toJson);
                    foreach (JObject jObject in jsonContent)
                    {
                        PwdListItem item = new PwdListItem();
                        item.LoadFromJSON(jObject);
                        PasswordsList.Add(item);
                    }
                }
                if (posNext > -1 && posNext < pwdContent.Length - 1)
                {
                    string next = pwdContent.Substring(posNext + LineBreak.Length);
                    if (next.StartsWith(TwoFATag))
                    {
                        toJson = Utility.ExtractDataWithBound(TwoFATag, bound, next, out posNext);
                        JObject twoFAJson = JObject.Parse(toJson);
                        Access2FA.LoadFromJSON(twoFAJson);
                    }
                }

                // Aggiorna le statistiche sulle password
                await App.Statistic.GatherStatistics();
            }
            catch (Newtonsoft.Json.JsonException e)
            {
                PasswordsList.Clear();
                Debug.WriteLine("LoadPasswordList error: " + e.Message);
                throw new Exception(AppResources.errInvalidFileFormat);                                
            }
            catch (Exception e)
            {
                PasswordsList.Clear();
                Debug.WriteLine("LoadPasswordList error: " + e.Message);
                throw new Exception(AppResources.errReadPassword + " " + e.Message);                              
            }
        }
    }

    /// <summary>
    /// Classe che rappresente un file allegato a una password
    /// </summary>
    public class PwdAttachmentFile: ICloneable
    {
        private PwdAttachmentFile ClonedObject = null;

        public string MD5 { get; set; }
        public string OriginalFileName { get; set; }

        public bool CurrentStateSaved { get => ClonedObject != null; }

        /// <summary>
        /// Restituisce l'oggetto JSON con le informazioni sull'allegato.
        /// </summary>
        /// <returns>Oggetto JSON con le informazioni sull'allegato</returns>
        public JObject AsJSON()
        {
            JObject jObject = new JObject
            {
                { nameof(MD5), new JValue(MD5) },
                { nameof(OriginalFileName), new JValue(OriginalFileName) }
            };

            return jObject;
        }

        /// <summary>
        /// Genera un clone dell'oggettp
        /// </summary>
        /// <returns>Oggetto clone</returns>
        public object Clone()
        {
            var curr = this;
            PwdAttachmentFile clonedObject = new PwdAttachmentFile
            {
                MD5 = curr.MD5,
                OriginalFileName = curr.OriginalFileName
            };

            return clonedObject;
        }

        /// <summary>
        /// Salva lo stato corrente dell'oggetto.
        /// </summary>
        public void SaveCurrentState()
        {
            if (CurrentStateSaved)
                throw new Exception("The state has been already saved. Invoke RestoreState or DiscardChanges.");

            ClonedObject = (PwdAttachmentFile)Clone();
        }

        /// <summary>
        /// Ripristina lo stato dell'oggetto.
        /// Chiamare la funzione SaveCurrentState per salvare lo stato.
        /// </summary>
        public void RestoreState()
        {
            if (ClonedObject == null)
                return;

            MD5 = ClonedObject.MD5;
            OriginalFileName = ClonedObject.OriginalFileName;

            ClonedObject = null;
        }

        /// <summary>
        /// Scarta le modifiche all'oggetto eliminando le informazioni salvate sullo stato
        /// </summary>
        public void DiscardChanges()
        {
            ClonedObject = null;
        }

        /// <summary>
        /// Carica i dati da un JSON.
        /// </summary>
        /// <param name="jObject">JSON di cui effettuare il parsing per ottenere i dati</param>
        public void LoadFromJSON(JObject jObject)
        {
            MD5 = jObject[nameof(MD5)].ToString();
            OriginalFileName = jObject[nameof(OriginalFileName)].ToString();
        }
    }

    /// <summary>
    /// Rappresenta un allegato associato a una password
    /// </summary>
    public class PwdAttachment: ICloneable, INotifyPropertyChanged
    {
        private PwdAttachment ClonedObject = null;

        public event PropertyChangedEventHandler PropertyChanged;

        private int _Id = 0;
        public int Id { get { return _Id; } set { _Id = value; OnPropertyChanged(nameof(Id)); } }

        private string _Name = null;
        public string Name { get { return _Name; } set { _Name = value; OnPropertyChanged(nameof(Name)); } }

        private string _Filename = null;
        public string Filename { get { return _Filename; } set { _Filename = value; OnPropertyChanged(nameof(Filename)); } }

        public string OriginalFilename
        {
            get => AttachmentFile?.OriginalFileName;
        }
        public PwdAttachmentFile AttachmentFile { get; set; }

        public bool CurrentStateSaved { get => ClonedObject != null; }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Restituisce l'oggetto JSON con le informazioni sull'allegato.
        /// </summary>
        /// <param name="fileStream">True per includere nel JSON il contenuto del file (in base 64).</param>
        /// <returns>Oggetto JSON con le informazioni sull'allegato</returns>
        public JObject AsJSON(bool fileStream=false)
        {
            JObject jObject = new JObject
            {
                { nameof(Id), new JValue(Id) },
                { nameof(Name), new JValue(Name) },
                { nameof(Filename), new JValue(Filename) }
            };

            if (AttachmentFile != null)
            {
                JObject jAttachmentFile = AttachmentFile.AsJSON();
                if (fileStream)
                {
                    byte[] buffer = File.ReadAllBytes(Path.Combine(App.PwdManager.GetAttachmentFolderPath(), Filename));
                    string base64 = Convert.ToBase64String(buffer);
                    jAttachmentFile.Add("base64", new JValue(base64));
                }
                jObject.Add(nameof(AttachmentFile), jAttachmentFile);
            }

            return jObject;
        }

        /// <summary>
        /// Genera un clone dell'oggetto
        /// </summary>
        /// <returns>Oggetto clone</returns>
        public object Clone()
        {
            var curr = this;
            PwdAttachment clonedObject = new PwdAttachment
            {
                Filename = curr.Filename,
                Id = curr.Id,
                Name = curr.Name
            };

            if (AttachmentFile != null)
                clonedObject.AttachmentFile = (PwdAttachmentFile)AttachmentFile.Clone();

            return clonedObject;
        }

        /// <summary>
        /// Salva lo stato corrente dell'oggetto
        /// </summary>
        public void SaveCurrentState()
        {
            if (CurrentStateSaved)
                throw new Exception("The state has been already saved. Invoke RestoreState or DiscardChanges.");

            ClonedObject = (PwdAttachment)Clone();
            if (AttachmentFile != null)
                AttachmentFile.SaveCurrentState();
        }

        /// <summary>
        /// Ripristina lo stato corrente dell'oggetto.
        /// Chiamare la funzione SaveCurrentState per salvare lo stato
        /// </summary>
        public void RestoreState()
        {
            if (ClonedObject == null)
                return;

            Filename = ClonedObject.Filename;
            Id = ClonedObject.Id;
            Name = ClonedObject.Name;
            if (ClonedObject.AttachmentFile == null)
                AttachmentFile = null;
            else if (AttachmentFile != null)
                AttachmentFile.RestoreState();

            ClonedObject = null;
        }

        /// <summary>
        /// Scarta le modifiche all'oggetto eliminando le informazioni salvate sullo stato
        /// </summary>
        public void DiscardChanges()
        {
            ClonedObject = null;
            if (AttachmentFile != null)
                AttachmentFile.DiscardChanges();
        }

        /// <summary>
        /// Carica i dati da un JSON.
        /// </summary>
        /// <param name="jObject">JSON di cui effettuare il parsing per ottenere i dati</param>
        public void LoadFromJSON(JObject jObject)
        {
            if (Utility.SafeTryGetJSONNumber(jObject, nameof(Id), out double retID))
                Id = (int)retID;
            Name = jObject[nameof(Name)].ToString();
            Filename = jObject[nameof(Filename)].ToString();
            AttachmentFile = null;

            if (jObject.ContainsKey(nameof(AttachmentFile)))
            {
                AttachmentFile = new PwdAttachmentFile();
                JObject jObjectAttFile = jObject[nameof(AttachmentFile)].ToObject<JObject>();
                AttachmentFile.LoadFromJSON(jObjectAttFile);

                if (jObjectAttFile.ContainsKey("base64"))
                {
                    string base64 = jObjectAttFile["base64"].ToString();
                    byte[] buffer = Convert.FromBase64String(base64);
                    if (!Directory.Exists(App.PwdManager.GetAttachmentFolderPath()))
                        Directory.CreateDirectory(App.PwdManager.GetAttachmentFolderPath());
                    File.WriteAllBytes(Path.Combine(App.PwdManager.GetAttachmentFolderPath(), Filename), buffer);
                }
            }
        }
    }

    /// <summary>
    /// Classe che rappresenta una collezione di campi speciali per le password
    /// </summary>
    public class PwdSpecialFields: ICloneable
    {
        private PwdSpecialFields ClonedObject = null;
        private readonly Dictionary<string, string> SpecialFieldsDictionary;

        public delegate void EventPropertyChanged([CallerMemberName] string propertyName = null);
        public event EventPropertyChanged OnPropertyChanged = null;

        public string this[string fieldName]
        {
            get => SpecialFieldByName(fieldName);
            set => SetSpecialField(fieldName, value);
        }

        public bool CurrentStateSaved { get => ClonedObject != null; }

        public PwdSpecialFields()
        {
            SpecialFieldsDictionary = new Dictionary<string, string>();
        }

        /// <summary>
        /// Ricerca il valore di un determinato campo specifico per tipo di
        /// account.
        /// </summary>
        /// <param name="fieldName">Nome del campo</param>
        /// <returns>Valore del campo o stringa vuota se non viene trovato</returns>
        public string SpecialFieldByName(string fieldName)
        {
            if (SpecialFieldsDictionary.TryGetValue(fieldName, out string fieldValue))
                return fieldValue;
            return "";
        }

        /// <summary>
        /// Imposta il valore di un campi specifico per tipo di account
        /// </summary>
        /// <param name="fieldName">Nome del campo</param>
        /// <param name="fieldValue">Valore del campo</param>
        public void SetSpecialField(string fieldName, string fieldValue)
        {
            if (SpecialFieldsDictionary.ContainsKey(fieldName))
                SpecialFieldsDictionary[fieldName] = fieldValue;
            else
                SpecialFieldsDictionary.Add(fieldName, fieldValue);

            // Evento di notifica della modifica della proprietà dall'oggetto padre
            OnPropertyChanged?.Invoke("SpecialFields");
        }

        /// <summary>
        /// Restituisce un oggetto JSON che rappresenta la lista dei campi speciali
        /// </summary>
        /// <returns>Oggetto JSON cone le informazioni sui campo speciali</returns>
        public JArray AsJSON()
        {
            if (SpecialFieldsDictionary.Count == 0)
                return null;

            JArray jSpecialFields = new JArray();
            foreach (KeyValuePair<string, string> item in SpecialFieldsDictionary)
            {
                jSpecialFields.Add(new JObject
                {
                    { item.Key, new JValue(item.Value) }
                });
            }

            return jSpecialFields;
        }


        /// <summary>
        /// Carica i dati da un JSON.
        /// </summary>
        /// <param name="jObject">JSON di cui effettuare il parsing per ottenere i dati</param>
        public void LoadFromJSON(JArray jArray)
        {
            SpecialFieldsDictionary.Clear();
            if (jArray != null)
            {
                foreach (JObject item in jArray)
                {
                    string itemName = item.Properties().First().Name;
                    SpecialFieldsDictionary.Add(itemName, item[itemName].ToString());
                }
            }   
        }

        /// <summary>
        /// Genera un clone dell'oggetto
        /// </summary>
        /// <returns>Oggetto clone</returns>
        public object Clone()
        {
            PwdSpecialFields clonedObject = new PwdSpecialFields();
            foreach (KeyValuePair<string, string> item in SpecialFieldsDictionary)
            {
                clonedObject[item.Key] = item.Value;
            }
            return clonedObject;
        }

        /// <summary>
        /// Salva lo stato corrente dell'oggetto
        /// </summary>
        public void SaveCurrentState()
        {
            if (CurrentStateSaved)
                throw new Exception("The state has been already saved. Invoke RestoreState or DiscardChanges.");

            ClonedObject = (PwdSpecialFields)Clone();
        }

        /// <summary>
        /// Ripristina lo stato dell'oggetto.
        /// Chiamare la funzione SaveCurrentState per salvare lo stato dell'oggetto
        /// </summary>
        public void RestoreState()
        {
            if (ClonedObject == null)
                return;

            SpecialFieldsDictionary.Clear();
            foreach (KeyValuePair<string, string> item in ClonedObject.SpecialFieldsDictionary)
            {
                if (item.Value.Length > 0)
                    SetSpecialField(item.Key, ClonedObject[item.Key]);
            }
            ClonedObject = null;
        }

        /// <summary>
        /// Scarta le modifiche all'oggetto eliminando le informazioni salvate sullo stato
        /// </summary>
        public void DiscardChanges()
        {
            ClonedObject = null;
        }
    }

    /// <summary>
    /// Elemento della lista delle password
    /// </summary>
    public class PwdListItem : ICloneable, INotifyPropertyChanged
    {
        static readonly Newtonsoft.Json.Converters.IsoDateTimeConverter dateTimeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter
        {
            DateTimeFormat = "yyyy-MM-ddTH:mm:ss.fffK"
        };

        private PwdListItem ClonedObject = null;

        public event PropertyChangedEventHandler PropertyChanged;

        private string _Id = null;
        public string Id { get { return _Id; } set { _Id = value; OnPropertyChanged(nameof(Id)); } }

        private DateTime _CreationDateTime = default;
        public DateTime CreationDateTime { get { return _CreationDateTime; } set { _CreationDateTime = value; OnPropertyChanged(nameof(CreationDateTime)); } }

        public DateTime LastPwdChangeDateTime = default;

        private string _Name = null;
        public string Name { get { return _Name; } set { _Name = value; OnPropertyChanged(nameof(Name)); } }

        private string _Username = null;
        public string Username { get { return _Username; } set { _Username = value; OnPropertyChanged(nameof(Username)); } }

        private string _Email = null;
        public string Email { get { return _Email; } set { _Email = value; OnPropertyChanged(nameof(Email)); } }

        private string _Password = null;
        public string Password { get { return _Password; } set { _Password = value; OnPropertyChanged(nameof(Password)); } }

        private string _SecurityQuestion = null;
        public string SecurityQuestion { get { return _SecurityQuestion; } set { _SecurityQuestion = value; OnPropertyChanged(nameof(SecurityQuestion)); } }

        private string _SecurityAnswer = null;
        public string SecurityAnswer { get { return _SecurityAnswer; } set { _SecurityAnswer = value; OnPropertyChanged(nameof(SecurityAnswer)); } }

        private string _Note = null;
        public string Note { get { return _Note; } set { _Note = value; OnPropertyChanged(nameof(Note)); } }

        private bool _SkipCheck = false;
        public bool SkipCheck { get { return _SkipCheck; } set { _SkipCheck = value; OnPropertyChanged(nameof(SkipCheck)); } }

        private UsernameLoginOption _LoginOption = default;
        public UsernameLoginOption LoginOption { get { return _LoginOption; } set { _LoginOption = value; OnPropertyChanged(nameof(LoginOption)); } }

        private AccountType _AccountOption = default;
        public AccountType AccountOption { get { return _AccountOption; } set { _AccountOption = value; OnPropertyChanged(nameof(AccountOption)); } }

        public List<PwdAttachment> Attachments { get; }

        public PwdSpecialFields SpecialFields { get; protected set; }

        public string AccountUsername
        {
            get
            {
                switch (LoginOption)
                {
                    case UsernameLoginOption.Email:
                        return Email;
                    default:
                    case UsernameLoginOption.Username:
                        return Username;
                }
            }
        }

        public bool CurrentStateSaved { get => ClonedObject != null; }

        public PwdListItem()
        {
            CreationDateTime = DateTime.Now;
            LastPwdChangeDateTime = DateTime.Now;
            Attachments = new List<PwdAttachment>();
            SpecialFields = new PwdSpecialFields();
            SpecialFields.OnPropertyChanged += OnPropertyChanged;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Restituisce la descrizione del tipo di account
        /// </summary>
        /// <returns>Descrizione del tipo di account</returns>
        public string GetAccountOptionName()
        {
            return Utility.EnumHelper.GetDescription(AccountOption);
        }

        /// <summary>
        /// Aggiunge un allegato alla lista degli allegati della password.
        /// </summary>
        /// <returns>Oggetto dell'allegato appena creato e aggiunto alla lista</returns>
        public PwdAttachment AddAttachment()
        {
            int id = 0;
            foreach (PwdAttachment item in Attachments)
                id = Math.Max(id, item.Id);
            
            PwdAttachment att = new PwdAttachment
            {
                Id = id + 1
            };
            Attachments.Add(att);
            return att;
        }

        /// <summary>
        /// Rimuove un allegato dalla lista degli allegati associati alla password
        /// </summary>
        /// <param name="item">Oggetto dell'allegato da eliminare</param>
        public void RemoveAttachment(PwdAttachment item)
        {
            Attachments.Remove(item);
        }

        /// <summary>
        /// Restituisce un oggetto JSON che rappresenta le informazioni
        /// sulla password.
        /// </summary>
        /// <param name="attachmentIncluded">True per includere nel JSON i file allegati (in base 64).</param>
        /// <returns>Oggetto JSON cone le informazioni sulla password</returns>
        public JObject AsJSON(bool attachmentIncluded=false)
        {
            JObject jObject = new JObject
            {
                { nameof(Id), new JValue(Id) },
                {
                    nameof(CreationDateTime),
                    new JValue(Newtonsoft.Json.JsonConvert.SerializeObject(CreationDateTime, dateTimeConverter))
                },

                { nameof(Name), new JValue(Name) },
                { nameof(Username), new JValue(Username) },
                { nameof(Email), new JValue(Email) },
                { nameof(Password), new JValue(Password) },
                { nameof(SecurityQuestion), new JValue(SecurityQuestion) },
                { nameof(SecurityAnswer), new JValue(SecurityAnswer) },
                { nameof(Note), new JValue(Note) },
                { nameof(SkipCheck), new JValue(SkipCheck) },

                { nameof(LoginOption), new JValue(LoginOption) },
                { nameof(AccountOption), new JValue(AccountOption) }
            };

            JArray jArray = new JArray();
            foreach (PwdAttachment attachment in Attachments)
                jArray.Add(attachment.AsJSON(attachmentIncluded));
            jObject.Add(nameof(Attachments), jArray);

            // Campi spciali
            JArray jSpecialFields = SpecialFields.AsJSON();
            if (jSpecialFields != null)
                jObject.Add(nameof(SpecialFields), jSpecialFields);

            return jObject;
        }

        /// <summary>
        /// Carica i dati da un JSON.
        /// </summary>
        /// <param name="jObject">JSON di cui effettuare il parsing per ottenere i dati</param>
        public void LoadFromJSON(JObject jObject)
        {
            Id = jObject[nameof(Id)].ToString();
            CreationDateTime = Newtonsoft.Json.JsonConvert.DeserializeObject<DateTime>(
                jObject[nameof(CreationDateTime)].ToString(), 
                dateTimeConverter);
            LastPwdChangeDateTime = CreationDateTime;
            if (jObject.ContainsKey(nameof(LastPwdChangeDateTime)))
            {
                LastPwdChangeDateTime = Newtonsoft.Json.JsonConvert.DeserializeObject<DateTime>(
                    jObject[nameof(LastPwdChangeDateTime)].ToString(),
                    dateTimeConverter);
            }

            Name = jObject[nameof(Name)].ToString();
            Username = jObject[nameof(Username)].ToString();
            Email = jObject[nameof(Email)].ToString();
            Password = jObject[nameof(Password)].ToString();
            SecurityQuestion = jObject[nameof(SecurityQuestion)].ToString();
            SecurityAnswer = jObject[nameof(SecurityAnswer)].ToString();
            Note = jObject[nameof(Note)].ToString();
            if (jObject.ContainsKey(nameof(SkipCheck)))
                SkipCheck = jObject[nameof(SkipCheck)].ToObject<bool>();

            string loginOptionValue = jObject[nameof(LoginOption)].ToString();
            Utility.TryParseEnum(loginOptionValue, UsernameLoginOption.Username, out UsernameLoginOption resultUsernameLoginOption);
            LoginOption = resultUsernameLoginOption;

            string accountOptionValue = jObject[nameof(AccountOption)].ToString();
            Utility.TryParseEnum(accountOptionValue, AccountType.Other, out AccountType resultAccountType);
            AccountOption = resultAccountType;

            // Allegati
            Attachments.Clear();
            if (jObject.ContainsKey(nameof(Attachments)))
            {
                JArray jArray = jObject[nameof(Attachments)].ToObject<JArray>();
                if (jArray != null)
                {
                    foreach (JObject jsonAttachment in jArray)
                    {
                        PwdAttachment attachment = new PwdAttachment();
                        attachment.LoadFromJSON(jsonAttachment);
                        Attachments.Add(attachment);
                    }
                }
            }

            // Campi speciali
            if (jObject.ContainsKey(nameof(SpecialFields)))
                SpecialFields.LoadFromJSON(jObject[nameof(SpecialFields)].ToObject<JArray>());
        }

        /// <summary>
        /// Genera un oggetto clone
        /// </summary>
        /// <returns>Clone dell'oggetto</returns>
        public object Clone()
        {
            var curr = this;
            PwdListItem clonedObject = new PwdListItem
            {
                AccountOption = curr.AccountOption,
                CreationDateTime = curr.CreationDateTime,
                LastPwdChangeDateTime = curr.LastPwdChangeDateTime,
                Email = curr.Email,
                Id = curr.Id,
                LoginOption = curr.LoginOption,
                Name = curr.Name,
                Note = curr.Note,
                SkipCheck = curr.SkipCheck,
                Password = curr.Password,
                SecurityAnswer = curr.SecurityAnswer,
                SecurityQuestion = curr.SecurityQuestion,
                Username = curr.Username
            };

            clonedObject.SpecialFields = (PwdSpecialFields)SpecialFields.Clone();
            foreach (PwdAttachment pwdAttachment in Attachments)
            {
                clonedObject.Attachments.Add((PwdAttachment)pwdAttachment.Clone());
            }

            return clonedObject;
        }

        /// <summary>
        /// Salva lo stato corrente dell'oggetto
        /// </summary>
        public void SaveCurrentState()
        {
            if (CurrentStateSaved)
                throw new Exception("The state has been already saved. Invoke RestoreState or DiscardChanges.");

            ClonedObject = (PwdListItem)Clone();
            SpecialFields.SaveCurrentState();
            foreach (PwdAttachment pwdAttachment in Attachments)
                pwdAttachment.SaveCurrentState();
        }

        /// <summary>
        /// Ripristina lo stato dell'oggetto.
        /// Per salvare lo stato chiamare la funzione SaveCurrentState
        /// </summary>
        public void RestoreState()
        {
            if (ClonedObject == null)
                return;

            AccountOption = ClonedObject.AccountOption;
            CreationDateTime = ClonedObject.CreationDateTime;
            LastPwdChangeDateTime = ClonedObject.LastPwdChangeDateTime;
            Email = ClonedObject.Email;
            Id = ClonedObject.Id;
            LoginOption = ClonedObject.LoginOption;
            Name = ClonedObject.Name;
            Note = ClonedObject.Note;
            SkipCheck = ClonedObject.SkipCheck;
            Password = ClonedObject.Password;
            SecurityAnswer = ClonedObject.SecurityAnswer;
            SecurityQuestion = ClonedObject.SecurityQuestion;
            Username = ClonedObject.Username;

            SpecialFields.RestoreState();
            foreach (PwdAttachment pwdAttachment in Attachments)
            {
                pwdAttachment.RestoreState();
            }

            ClonedObject = null;
        }

        /// <summary>
        /// Scarta le modifiche all'oggetto eliminando le informazioni salvate sullo stato
        /// </summary>
        public void DiscardChanges()
        {
            ClonedObject = null;
            SpecialFields.DiscardChanges();
            foreach (PwdAttachment pwdAttachment in Attachments)
            {
                pwdAttachment.DiscardChanges();
            }
        }
    }

    /// <summary>
    /// Oggetto che raggruppa le informazioni sull'accesso con autenticazione a due fattori
    /// </summary>
    public class TwoFAInfo
    {
        public string Code { get; set; }
        public string BackupCode { get; set; }
        public bool IsAvailable => IsConfigured();

        private bool IsConfigured()
        {
            return !string.IsNullOrEmpty(Code) && !string.IsNullOrEmpty(BackupCode);
        }

        /// <summary>
        /// Carica i dati da un JSON.
        /// </summary>
        /// <param name="jObject">JSON di cui effettuare il parsing per ottenere i dati</param>
        public void LoadFromJSON(JObject jObject)
        {
            Code = jObject[nameof(Code)].ToString();
            BackupCode = jObject[nameof(BackupCode)].ToString();
        }

        /// <summary>
        /// Restituisce un oggetto JSON con le informazioni sull'autenticazione 2FA        
        /// </summary>        
        /// <returns>Oggetto JSON cone le informazioni sull'autenticazione 2FA</returns>
        public JObject AsJSON()
        {
            JObject jObject = new JObject
            {
                {nameof(Code), new JValue(Code) },
                {nameof(BackupCode), new JValue(BackupCode) }
            };
            return jObject;
        }

        /// <summary>
        /// Reset dell'oggetto
        /// </summary>
        public void Clear()
        {
            Code = string.Empty;
            BackupCode = string.Empty;            
        }
    }
}
