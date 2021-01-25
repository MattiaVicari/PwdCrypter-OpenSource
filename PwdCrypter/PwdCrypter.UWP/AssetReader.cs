using System.IO;
using System.Text;
using Xamarin.Forms;

[assembly: Dependency(typeof(PwdCrypter.UWP.AssetReader))]
namespace PwdCrypter.UWP
{
    public class AssetReader : IAssetReader
    {
        public string ReadText(string assetName)
        {
            string content = File.ReadAllText(Path.Combine("Assets", assetName), Encoding.UTF8);
            return content;
        }
    }
}
