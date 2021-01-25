using Xamarin.Forms;

[assembly: Dependency(typeof(PwdCrypter.UWP.HtmlWebViewBaseUrl))]
namespace PwdCrypter.UWP
{
    class HtmlWebViewBaseUrl : IHtmlWebViewBaseUrl
    {
        public string Get()
        {
            return "ms-appx-web:///";
        }
    }
}
