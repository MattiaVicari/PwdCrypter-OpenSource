using Android.Content;
using PwdCrypter.Controls;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(CustomWebView), typeof(PwdCrypter.Droid.Controls.CustomWebViewRenderer))]
namespace PwdCrypter.Droid.Controls
{
    class CustomWebViewRenderer : WebViewRenderer
    {
        public CustomWebViewRenderer(Context context) : base(context)
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<WebView> e)
        {
            base.OnElementChanged(e);
            if (e.OldElement == null)
            {
                CustomWebView customControl = (CustomWebView)Element;
                if (!string.IsNullOrEmpty(customControl.HtmlToLoad))
                    Control.LoadData(customControl.HtmlToLoad, "text/html", "UTF-8");
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            if (e.PropertyName == nameof(CustomWebView.HtmlToLoad))
            {
                CustomWebView customControl = (CustomWebView)Element;
                if (!string.IsNullOrEmpty(customControl.HtmlToLoad))
                    Control.LoadData(customControl.HtmlToLoad, "text/html", "UTF-8");
            }
        }
    }
}