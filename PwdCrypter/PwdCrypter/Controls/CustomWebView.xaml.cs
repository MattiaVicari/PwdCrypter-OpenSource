using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter.Controls
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CustomWebView : WebView
    {
        public static BindableProperty HtmlToLoadProperty =
            BindableProperty.Create(nameof(HtmlToLoad), typeof(string), typeof(CustomWebView), default(string));

        public string HtmlToLoad
        {
            get => (string)GetValue(HtmlToLoadProperty);
            set => SetValue(HtmlToLoadProperty, value);
        }

        public CustomWebView()
        {
            InitializeComponent();
        }
    }
}