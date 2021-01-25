using PwdCrypter.Controls;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(FontAwesomeLabel), typeof(PwdCrypter.UWP.FontAwesomeLabelRenderer))]
namespace PwdCrypter.UWP
{
    public class FontAwesomeLabelRenderer: LabelRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Label> e)
        {
            base.OnElementChanged(e);
            if (e.OldElement == null)
            {
                Control.FontFamily = new Windows.UI.Xaml.Media.FontFamily("/Assets/Fonts/" + FontAwesomeLabel.FontAwesomeName + ".otf#Font Awesome 5 Free");
            }
        }
    }
}
