using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter.Controls
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class FontAwesomeLabel : Label
	{
        public static readonly string FontAwesomeName = "FontAwesome";

        public event System.EventHandler OnClicked = null;

        public FontAwesomeLabel ()
		{
			InitializeComponent ();
            FontFamily = FontAwesomeName;

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

        public FontAwesomeLabel(string fontAwesomeLabel = null)
        {
            InitializeComponent();
            FontFamily = FontAwesomeName;
            Text = fontAwesomeLabel;
        }
    }

    public static class Icon
    {
        public static readonly string FAFolderOpen = "\uf07c";
        public static readonly string FAMinusCircle = "\uf056";
        public static readonly string FAPlusCircle = "\uf055";
        public static readonly string FAPencilAlt = "\uf303";
        public static readonly string FACloudSolid = "\uf0c2";
        public static readonly string FAArrowCircleUpSolid = "\uf0aa";
        public static readonly string FAArrowCircleDownSolid = "\uf0ab";
        public static readonly string FAAngleRightSolid = "\uf105";
        public static readonly string FADownloadSolid = "\uf019";
        public static readonly string FAUploadSolid = "\uf093";
        public static readonly string FAHourglassEndSolid = "\uf253";
        public static readonly string FAExchangeAltSolid = "\uf362";
        public static readonly string FACloudUploadAltSolid = "\uf382";
        public static readonly string FACloudDownloadAltSolid = "\uf381";
        public static readonly string FAUnlockAltSolid = "\uf13e";
        public static readonly string FAFingerprintSolid = "\uf577";
        public static readonly string FAMobileAltSolid = "\uf3cd";
        public static readonly string FAWindowCloseSolid = "\uf410";
        public static readonly string FACheckSquareSolid = "\uf14a";
        public static readonly string FAShieldAltSolid = "\uf3ed";
        public static readonly string FAAlignLeftSolid = "\uf036";
        public static readonly string FASkullCrossBonesSolid = "\uf714";
        public static readonly string FAAtomSolid = "\uf5d2";
        public static readonly string FAMedalSolid = "\uf5a2";
        public static readonly string FAForwardSolid = "\uf04e";
    }
}