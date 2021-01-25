using System;
using System.Collections.Generic;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ExportFilterPage : ContentPage
    {
        public static Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }

        public delegate void EventForFilterOption(object sender, EventArgs e, FilterOption item);

        // Eventi sulla pagina del filtro di ricerca
        public event EventForFilterOption ConfirmFilter;
        public event EventHandler CancelFilter;

        private FilterOption Filter = new FilterOption();

        public ExportFilterPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            switchCloud.IsEnabled = App.IsCloudEnabled();
            labelCloudRequirement.IsVisible = !switchCloud.IsEnabled;

            if (BindingContext != null && BindingContext is FilterOption filterOption)
            {
                Filter = filterOption;
                switchCloud.IsToggled = Filter.InCloud;
            }
        }

        private void ContentPage_SizeChanged(object sender, EventArgs e)
        {
            const double AutoWidth = -1;

            if (Width > App.ViewStepMaxWidth)
            {
                double fixedWidth = App.ViewStepMaxWidth - 140;

                gridMain.HorizontalOptions = LayoutOptions.CenterAndExpand;
                gridMain.WidthRequest = fixedWidth;
            }
            else
            {
                gridMain.HorizontalOptions = LayoutOptions.FillAndExpand;
                gridMain.WidthRequest = AutoWidth;
            }
        }

        private async void ButtonConfirm_Clicked(object sender, EventArgs e)
        {
            Filter.InCloud = switchCloud.IsToggled;

            if (ConfirmFilter != null)
                ConfirmFilter.Invoke(this, e, Filter);
            await Navigation.PopAsync(true);
        }

        private async void ButtonCancel_Clicked(object sender, EventArgs e)
        {
            if (CancelFilter != null)
                CancelFilter.Invoke(this, e);
            await Navigation.PopAsync(true);
        }
    }
}