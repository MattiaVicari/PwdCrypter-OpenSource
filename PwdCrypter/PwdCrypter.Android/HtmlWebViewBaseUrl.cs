using Xamarin.Forms;

[assembly: Dependency(typeof(PwdCrypter.Droid.HtmlWebViewBaseUrl))]
namespace PwdCrypter.Droid
{
    class HtmlWebViewBaseUrl : IHtmlWebViewBaseUrl
    {
        public string Get()
        {
            return "file:///android_asset/";
        }
    }
}