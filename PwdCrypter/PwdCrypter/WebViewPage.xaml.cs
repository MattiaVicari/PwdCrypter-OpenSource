using System;
using System.Diagnostics;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class WebViewPage : ContentPage
	{
        private string _HtmlData = null;
        private bool _ModalMode = false;

        public string HtmlData { get => _HtmlData; set => SetHtmlData(value); }
        public bool ModalMode 
        { 
            get => _ModalMode; 
            set
            {
                btnClose.IsVisible = value;
                _ModalMode = value;
            }
        }

        private void SetHtmlData(string value)
        {
            _HtmlData = value;
            webViewContent.HtmlToLoad = _HtmlData;
        }

        public WebViewPage ()
		{
			InitializeComponent ();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext != null && BindingContext is string fileName)
            {
                try
                {
                    string htmlSource = DependencyService.Get<IAssetReader>().ReadText(fileName);
                    SetHtmlData(htmlSource);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(string.Format("Read license file {0}. Error: {1}", fileName, ex.Message));
                }
            }
        }

        private void Close_Clicked(object sender, EventArgs e)
        {
            Navigation.PopModalAsync(true);
        }

        private void WebViewContent_Navigating(object sender, WebNavigatingEventArgs e)
        {
            // Apre i link esternamente
            if (e.Url.StartsWith("http"))
            {
                try
                {
                    var uri = new Uri(e.Url);
                    Xamarin.Essentials.Launcher.OpenAsync(uri);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Navigating error: " + ex.Message);
                }

                e.Cancel = true;
            }
        }
    }
}