using System.Threading.Tasks;

namespace PwdCrypter.Cloud
{
    /// <summary>
    /// Tipo enumerato che identifica la piattaforma Cloud
    /// </summary>
    public enum CloudPlatform
    {
        OneDrive,
        GoogleDrive,
        Unknown
    }

    /// <summary>
    /// Interfaccia che rappresenta una connessione con la piattaforma Cloud
    /// </summary>
    public interface ICloudConnector
    {
        event System.EventHandler LoggedIn;
        event System.EventHandler LoggedOut;

        string GetAccessToken();
        bool IsTokenExpired();
        string GetLastError();
        bool IsLoggedIn();

        void SetRememberMe(bool remember);
        bool GetRememberMe();

        Task<bool> Login();
        Task<bool> Logout();
        Task<string> Refresh();
        Task<bool> ValidateToken();

        Task<CloudFileStatus> FileExists(string FileName);
        Task<CloudFileStatus> FolderExists(string FolderName);
        Task<CloudFileStatus> GetFile(string FileName, string DestFileName);
        Task<bool> UploadFile(string FileName, string DestFileName);
        Task DeleteFile(string FileName);
        Task<System.Collections.Generic.IList<CloudFileInfo>> GetFilesInfo(string FolderName);

        Task CreateFolder(string folderName);
    }
}
