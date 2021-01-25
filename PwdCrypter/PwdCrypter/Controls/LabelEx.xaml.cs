using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter.Controls
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LabelEx : StackLayout
    {
        public static BindableProperty LabelIconProperty =
            BindableProperty.Create(nameof(LabelIcon), typeof(string), typeof(LabelEx), default(string), BindingMode.OneWay);
        public static BindableProperty TextColorProperty =
            BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(LabelEx), Color.Black, BindingMode.OneWay);
        public static BindableProperty FontSizeProperty =
            BindableProperty.Create(nameof(FontSize), typeof(double), typeof(LabelEx), default(double), BindingMode.OneWay);
        public static BindableProperty TextProperty =
            BindableProperty.Create(nameof(Text), typeof(string), typeof(LabelEx), default(string), BindingMode.OneWay);

        public string LabelIcon
        {
            get => (string)GetValue(LabelIconProperty);
            set => SetValue(LabelIconProperty, value);
        }
        public Color TextColor
        {
            get => (Color)GetValue(TextColorProperty);
            set => SetValue(TextColorProperty, value);
        }
        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public LabelEx()
        {
            InitializeComponent();
        }
    }
}