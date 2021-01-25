using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class WaitPage : ContentPage
	{
        public static Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }

        public WaitPage ()
		{
			InitializeComponent ();
		}

        protected override void OnAppearing()
        {
            base.OnAppearing();

            Animation animation = new Animation((value) => lblHourglass.Rotation = value, 0, 365);            
            animation.Commit(this, "HourglassRotation", 16, 2000, Easing.Linear,
                (finalValue, cancelled) => lblHourglass.Rotation = finalValue,
                () => true);
        }

        /// <summary>
        /// Chiude la pagina modale
        /// </summary>
        /// <returns></returns>
        public async Task Close()
        {
            await Navigation.PopModalAsync(true);
        }
	}
}