using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter.Controls
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class NestedScrollView : ScrollView
    {
        public NestedScrollView ()
		{
			InitializeComponent ();
		}
	}
}