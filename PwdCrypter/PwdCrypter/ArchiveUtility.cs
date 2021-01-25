using System;
using System.IO.Compression;
using System.IO;
using Xamarin.Forms;
using System.Threading.Tasks;

namespace PwdCrypter
{
    public class ArchiveUtility
    {
        public static void ZipArchive(string zipFile, string folderToArchive)
        {
            ZipFile.CreateFromDirectory(folderToArchive, zipFile, CompressionLevel.Optimal, false);
        }

        public static async Task ZipArchive(Stream outStream, string folderToArchive)
        {
            ICrossPlatformSpecialFolder specialFolder = DependencyService.Get<ICrossPlatformSpecialFolder>();
            string tempFolder = specialFolder.GetTemporaryFolder();
            string zipFile = Path.Combine(tempFolder, "temp" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".zip");
            try
            {
                ZipFile.CreateFromDirectory(folderToArchive, zipFile, CompressionLevel.Optimal, false);
                var stream = File.OpenRead(zipFile);
                try
                {
                    await stream.CopyToAsync(outStream);
                }
                finally
                {
                    stream.Dispose();
                }
            }
            finally
            {
                if (File.Exists(zipFile))
                    File.Delete(zipFile);
            }
        }

        public static void UnzipArchive(string zipFile, string destinationFolder)
        {
            Directory.CreateDirectory(destinationFolder);
            ZipFile.ExtractToDirectory(zipFile, destinationFolder);            
        }

        public static async Task UnzipArchive(Stream inStream, string destinationFolder)
        {
            ICrossPlatformSpecialFolder specialFolder = DependencyService.Get<ICrossPlatformSpecialFolder>();
            string tempFolder = specialFolder.GetTemporaryFolder();
            string zipFile = Path.Combine(tempFolder, "temp" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".zip");
            try
            {
                FileStream file = File.Create(zipFile);                
                try
                {
                    await inStream.CopyToAsync(file);                                        
                }
                finally
                {
                    file.Dispose();
                }
                UnzipArchive(zipFile, destinationFolder);
            }
            finally
            {
                if (File.Exists(zipFile))
                    File.Delete(zipFile);
            }
        }

        public static void GZipArchive(string gzipFile, string folderToArchive)
        {
            string[] files = Directory.GetFiles(folderToArchive, "*.*", SearchOption.AllDirectories);
            int folderLen = folderToArchive[folderToArchive.Length - 1] == Path.DirectorySeparatorChar ? folderToArchive.Length : folderToArchive.Length + 1;

            FileStream outFile = new FileStream(gzipFile, FileMode.Create, FileAccess.Write, FileShare.None);
            try
            {
                GZipStream gzipStream = new GZipStream(outFile, CompressionMode.Compress);
                try
                {
                    foreach (string sFilePath in files)
                    {
                        string relativePath = sFilePath.Substring(folderLen);
                        CompressFile(folderToArchive, relativePath, gzipStream);
                    }
                }
                finally
                {
                    gzipStream.Close();
                }
            }
            finally
            {
                outFile.Close();
            }
        }

        public static async Task GZipArchive(Stream outStream, string folderToArchive)
        {
            string[] files = Directory.GetFiles(folderToArchive, "*.*", SearchOption.AllDirectories);
            int folderLen = folderToArchive[folderToArchive.Length - 1] == Path.DirectorySeparatorChar ? folderToArchive.Length : folderToArchive.Length + 1;

            await Task.Run(() =>
            {
                GZipStream gzipStream = new GZipStream(outStream, CompressionMode.Compress);
                try
                {
                    foreach (string sFilePath in files)
                    {
                        string relativePath = sFilePath.Substring(folderLen);
                        CompressFile(folderToArchive, relativePath, gzipStream);
                    }
                }
                finally
                {
                    gzipStream.Close();
                }
            });
        }

        private static void CompressFile(string folderToArchive, string relativePath, GZipStream gzipStream)
        {
            // Compressione del nome del file
            char[] chars = relativePath.ToCharArray();
            gzipStream.Write(BitConverter.GetBytes(chars.Length), 0, sizeof(int));
            foreach (char c in chars)
                gzipStream.Write(BitConverter.GetBytes(c), 0, sizeof(char));

            // Compressione del contenuto del file
            byte[] bytes = File.ReadAllBytes(Path.Combine(folderToArchive, relativePath));
            gzipStream.Write(BitConverter.GetBytes(bytes.Length), 0, sizeof(int));
            gzipStream.Write(bytes, 0, bytes.Length);
        }
    }
}
