using PwdCrypter.Controls;
using System.ComponentModel;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(CustomWebView), typeof(PwdCrypter.UWP.Controls.CustomWebViewRenderer))]
namespace PwdCrypter.UWP.Controls
{
    public class CustomWebViewRenderer : WebViewRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.WebView> e)
        {
            base.OnElementChanged(e);
            if (Control != null)
            {
                CustomWebView customControl = (CustomWebView)Element;
                if (!string.IsNullOrEmpty(customControl.HtmlToLoad))
                    Control.NavigateToString(customControl.HtmlToLoad);
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            if (e.PropertyName == nameof(CustomWebView.HtmlToLoad))
            {
                CustomWebView customControl = (CustomWebView)Element;
                if (!string.IsNullOrEmpty(customControl.HtmlToLoad))
                    Control.NavigateToString(customControl.HtmlToLoad);
            }
        }
    }
}
