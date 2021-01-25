using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PwdCrypter
{    
    public delegate void EventHandlerSaveFile(object sender, FolderPickerResult info);

    /// <summary>
    /// Classe che rappresenta il risultato del salvataggio di un file
    /// nella cartella selezionata.
    /// </summary>
    public class FolderPickerResult
    {
        public string FilePath { get; set; }
        public bool Succeeded { get; set; }
        public string Error { get; set; }
    }

    public interface ICrossPlatformSpecialFolder
    {
        event EventHandlerSaveFile OnSaveFileHandler;

        void Init();
        void Finish();

        string GetTemporaryFolder();
        string GetMIMEType(string extension);

        Task OpenFileByDefaultApp(string filepath);
        Task<bool> SaveFile(string defaultFileName, List<KeyValuePair<string, List<string>>> fileTypeList, byte[] data);
        Task<bool> SaveFile(string defaultFileName, List<KeyValuePair<string, List<string>>> fileTypeList, Stream streamData);
    }
}
