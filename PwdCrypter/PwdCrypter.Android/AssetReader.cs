using System.IO;
using Android.Content.Res;
using Xamarin.Forms;

[assembly: Dependency(typeof(PwdCrypter.Droid.AssetReader))]
namespace PwdCrypter.Droid
{
    public class AssetReader : IAssetReader
    {
        public string ReadText(string assetName)
        {
            string content;
            AssetManager assets = Android.App.Application.Context.Assets;
            StreamReader stream = new StreamReader(assets.Open(assetName));
            content = stream.ReadToEnd();
            stream.Close();
            return content;
        }
    }
}