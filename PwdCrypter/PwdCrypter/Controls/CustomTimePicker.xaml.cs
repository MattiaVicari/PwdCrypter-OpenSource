using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter.Controls
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CustomTimePicker : TimePicker
    {
        public static BindableProperty MinutesIntervalProperty =
            BindableProperty.Create(nameof(MinutesInterval), typeof(int), typeof(CustomTimePicker), 1);


        public int MinutesInterval
        {
            get => (int)GetValue(MinutesIntervalProperty);
            set => SetValue(MinutesIntervalProperty, value);
        }

        public CustomTimePicker()
        {
            InitializeComponent();
        }
    }
}