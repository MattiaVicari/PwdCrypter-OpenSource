using System;
using System.Collections.Generic;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LicensesPage : ContentPage
    {
        public static Dictionary<string, string> LicenseIconColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#000000" }, { "currentOpacity", "1.0" } };
        }
        public static Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }

        public LicensesPage()
        {
            InitializeComponent();
            LoadLicenses();
        }

        private void LoadLicenses()
        {            
            IAppInformant informant = DependencyService.Get<IAppInformant>();
            List<LicenseItem> licenses = informant.GetThirdPartyLicenses();

            // Aggiunge le licenze comuni a tutte le piattaforme
            licenses.Add(new LicenseItem
            {
                ThirdPartyName = "NETStandard.Library",
                LicenseCopyright = "(C) .NET Foundation and Contributors",
                LicenseUrl = "https://github.com/dotnet/standard/blob/master/LICENSE.TXT",
                LicensePageType = typeof(WebViewPage),
                BindingContext = "Licenses/NETStandard.Library.htm"
            });
            licenses.Add(new LicenseItem
            {
                ThirdPartyName = "Newtonsoft.Json",
                LicenseCopyright = "(C) James Newton-King",
                LicenseUrl = "https://licenses.nuget.org/MIT",
                LicensePageType = typeof(WebViewPage),
                BindingContext = "Licenses/NewtonsoftJson.htm"
            });
            licenses.Add(new LicenseItem
            {
                ThirdPartyName = "Plugin.InAppBilling",
                LicenseCopyright = "(C) 2017 James Montemagno / Refractored LLC",
                LicenseUrl = "https://github.com/jamesmontemagno/InAppBillingPlugin/blob/master/LICENSE",
                LicensePageType = typeof(WebViewPage),
                BindingContext = "Licenses/PluginInAppBilling.htm"
            });
            licenses.Add(new LicenseItem
            {
                ThirdPartyName = "SkiaSharp.Views.Forms",
                LicenseCopyright = "(C) 2015-2016 Xamarin, Inc.\n(C) 2017 - 2018 Microsoft Corporation",
                LicenseUrl = "https://github.com/mono/SkiaSharp/blob/master/LICENSE.md",
                LicensePageType = typeof(WebViewPage),
                BindingContext = "Licenses/SkiaSharpViewsForms.htm"
            });
            licenses.Add(new LicenseItem
            {
                ThirdPartyName = "System.IO.Compression.ZipFile",
                LicenseCopyright = "(C) Microsoft",
                LicenseUrl = "https://dotnet.microsoft.com/en/dotnet_library_license.htm",
                LicensePageType = typeof(WebViewPage),
                BindingContext = "Licenses/Microsoft.htm"
            });
            licenses.Add(new LicenseItem
            {
                ThirdPartyName = "Xam.Forms.QRCode",
                LicenseCopyright = "(C) 2018 Aloïs Deniel",
                LicenseUrl = "https://github.com/dotnet-ad/Xam.Forms.QRCode/blob/master/LICENSE",
                LicensePageType = typeof(WebViewPage),
                BindingContext = "Licenses/XamFormsQRCode.htm"
            });
            licenses.Add(new LicenseItem
            {
                ThirdPartyName = "Xamarin.Auth",
                LicenseCopyright = "(C) Microsoft",
                LicenseUrl = "https://github.com/xamarin/Xamarin.Auth/blob/master/License.md",
                LicensePageType = typeof(WebViewPage),
                BindingContext = "Licenses/XamarinAuth.htm"
            });
            licenses.Add(new LicenseItem
            {
                ThirdPartyName = "Xamarin.Essentials",
                LicenseCopyright = "(C) Microsoft",
                LicenseUrl = "https://www.nuget.org/packages/Xamarin.Essentials/1.5.2/license",
                LicensePageType = typeof(WebViewPage),
                BindingContext = "Licenses/XamarinEssentials.htm"
            });
            licenses.Add(new LicenseItem
            {
                ThirdPartyName = "Xamarin.FFImageLoading.Svg.Forms",
                LicenseCopyright = "(C) 2015 Daniel Luberda & Fabien Molinet",
                LicenseUrl = "https://raw.githubusercontent.com/luberda-molinet/FFImageLoading/master/LICENSE.md",
                LicensePageType = typeof(WebViewPage),
                BindingContext = "Licenses/XamarinFFImageLoadingSvgForms.htm"
            });
            licenses.Add(new LicenseItem
            {
                ThirdPartyName = "Xamarin.Forms",
                LicenseCopyright = "(C) Microsoft",
                LicenseUrl = "https://licenses.nuget.org/MIT",
                LicensePageType = typeof(WebViewPage),
                BindingContext = "Licenses/XamarinForms.htm"
            });
            licenses.Add(new LicenseItem
            {
                ThirdPartyName = "Xamarin.Plugin.FilePicker",
                LicenseCopyright = "(C) 2018 jfversluis - Xamarin & .NET",
                LicenseUrl = "https://github.com/jfversluis/FilePicker-Plugin-for-Xamarin-and-Windows/blob/master/LICENSE",
                LicensePageType = typeof(WebViewPage),
                BindingContext = "Licenses/XamarinPluginFilePicker.htm"
            });
            licenses.Add(new LicenseItem
            {
                ThirdPartyName = "Xamarin.Plugins.Clipboard",
                LicenseCopyright = "(C) 2017 AkiraGTX",
                LicenseUrl = "https://github.com/stavroskasidis/XamarinClipboardPlugin/blob/master/LICENSE",
                LicensePageType = typeof(WebViewPage),
                BindingContext = "Licenses/XamarinPluginsClipboard.htm"
            });
            licenses.Add(new LicenseItem
            {
                ThirdPartyName = "zxcvbn-core",
                LicenseCopyright = "(C) 2012 Dropbox, Inc.",
                LicenseUrl = "https://github.com/trichards57/zxcvbn-cs/blob/master/LICENSE",
                LicensePageType = typeof(WebViewPage),
                BindingContext = "Licenses/zxcvbncore.htm"
            });
            licenses.Add(new LicenseItem
            {
                ThirdPartyName = "Font Awesome",
                LicenseCopyright = "(C) Fonticons, Inc.",
                LicenseUrl = "https://creativecommons.org/licenses/by/4.0/",
                LicensePageType = typeof(WebViewPage),
                BindingContext = "Licenses/fontawesome.htm"
            });
            
            listViewLicenses.ItemsSource = licenses;
        }

        private async void ListViewLicenses_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem != null && e.SelectedItem is LicenseItem item)
            {
                if (item.LicensePageType != null)
                {
                    Page page = (Page)Activator.CreateInstance(item.LicensePageType);
                    if (page != null)
                    {
                        page.Title = item.ThirdPartyName;
                        if (item.BindingContext != null)
                            page.BindingContext = item.BindingContext;
                        await Navigation.PushAsync(page, true);
                    }
                }
                else if (!string.IsNullOrEmpty(item.LicenseUrl))
                {
                    await Xamarin.Essentials.Launcher.OpenAsync(item.LicenseUrl);
                }

                listViewLicenses.SelectedItem = null;
            }
        }
    }    
}