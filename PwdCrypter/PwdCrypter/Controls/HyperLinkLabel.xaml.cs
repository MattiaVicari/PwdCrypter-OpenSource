using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter.Controls
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class HyperLinkLabel : Label
	{
        public static BindableProperty UrlProperty =
            BindableProperty.Create(nameof(Url), typeof(string), typeof(HyperLinkLabel), default(string), BindingMode.OneWay);
        
        public event System.EventHandler OnClicked = null;

        public HyperLinkLabel ()
		{
			InitializeComponent ();

            ClickGestureRecognizer gestClick = new ClickGestureRecognizer
            {
                NumberOfClicksRequired = 1
            };
            gestClick.Clicked += GestOpenLink;

            TapGestureRecognizer gestTap = new TapGestureRecognizer();
            gestTap.Tapped += GestOpenLink;

            GestureRecognizers.Add(gestClick);
            GestureRecognizers.Add(gestTap);
        }

        private void GestOpenLink(object sender, System.EventArgs e)
        {
            if (OnClicked != null)
                OnClicked.Invoke(sender, e);
            else
                Xamarin.Essentials.Launcher.OpenAsync(new System.Uri(Url));
        }

        public string Url
        {
            get => (string)GetValue(UrlProperty);
            set => SetValue(UrlProperty, value);
        }
    }
}