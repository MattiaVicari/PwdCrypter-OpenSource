using System.Runtime.CompilerServices;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter.Controls
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ImageTextButton : Frame
	{
        public static BindableProperty IconProperty =
            BindableProperty.Create(nameof(Icon), typeof(string), typeof(ImageTextButton), default(string), BindingMode.OneWay);
        public static BindableProperty TextProperty =
            BindableProperty.Create(nameof(Text), typeof(string), typeof(ImageTextButton), default(string), BindingMode.OneWay);
        public static BindableProperty FontSizeProperty =
            BindableProperty.Create(nameof(FontSize), typeof(double), typeof(ImageTextButton), default(double), BindingMode.OneWay);

        public event System.EventHandler OnClicked = null;

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }
        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }


        public ImageTextButton ()
		{
			InitializeComponent ();

            labelButtonText.Text = Text;
            faLabelIcon.Text = Icon;
            if (FontSize > 0)
            {
                labelButtonText.FontSize = FontSize;
                faLabelIcon.FontSize = FontSize;
            }

            ClickGestureRecognizer GestClick = new ClickGestureRecognizer
            {
                NumberOfClicksRequired = 1
            };
            GestClick.Clicked += GestClick_Clicked;

            TapGestureRecognizer GestTap = new TapGestureRecognizer();
            GestTap.Tapped += GestTap_Tapped;

            GestureRecognizers.Add(GestClick);
            GestureRecognizers.Add(GestTap);
        }

        private void GestTap_Tapped(object sender, System.EventArgs e)
        {
            OnClicked?.Invoke(sender, e);
        }

        private void GestClick_Clicked(object sender, System.EventArgs e)
        {
            OnClicked?.Invoke(sender, e);
        }

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == TextProperty.PropertyName)
                labelButtonText.Text = Text;
            if (propertyName == IconProperty.PropertyName)
                faLabelIcon.Text = Icon;
            if (propertyName == FontSizeProperty.PropertyName && FontSize > 0)
            {
                labelButtonText.FontSize = FontSize;
                faLabelIcon.FontSize = FontSize;
            }
        }
    }
}