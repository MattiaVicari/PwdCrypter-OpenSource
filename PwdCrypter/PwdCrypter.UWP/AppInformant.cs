using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Store;
using Xamarin.Forms;

[assembly: Dependency(typeof(PwdCrypter.UWP.AppInformant))]
namespace PwdCrypter.UWP
{
    public class AppInformant : IAppInformant
    {
        private LicenseInformation GetLicenInfo()
        {
            LicenseInformation licenseInformation;
#if DEBUG
            licenseInformation = CurrentAppSimulator.LicenseInformation;
#else
            licenseInformation = CurrentApp.LicenseInformation;
#endif
            return licenseInformation;
        }

        private async Task<string> GetAppReceipt()
        {
#if DEBUG
            return await CurrentAppSimulator.GetAppReceiptAsync();
#else
            return await CurrentApp.GetAppReceiptAsync();
#endif
        }

        public async Task<bool> AppWasPurchased()
        {
            // L'App è stata pubblicata in precedenza a pagamento.
            // Verifica sse l'utente è attualmente in possesso di una licenza.

            try
            {
                LicenseInformation license = GetLicenInfo();
                if (license != null && license.IsActive)
                {
                    int numDays = 1000;
                    if (license.ExpirationDate != null)
                        numDays = (license.ExpirationDate - DateTime.Now).Days;

                    // Molto probabilmente IsTrial sarà a true per l'App free...
                    if (license.IsTrial || numDays <= 30)
                        return false;

                    // Se così non fosse, posso guardare la ricevuta

                    // Nella ricevuta dovrei trovare le informazioni sull'acquisto.
                    // Se non ho la ricevuta, significa che l'App non è stata acquistata
                    string receipt = await GetAppReceipt();
                    if (receipt.Length == 0)
                        return false;

                    XDocument xmlReceipt = XDocument.Parse(receipt);
                    if (xmlReceipt.Root != null && xmlReceipt.Root.HasElements)
                    {
                        XElement elemAppReceipt = xmlReceipt.Root.Element("AppReceipt");
                        if (elemAppReceipt != null && elemAppReceipt.HasAttributes)
                        {
                            if (elemAppReceipt.Attribute("PurchaseDate") != null
                                && elemAppReceipt.Attribute("LicenseType") != null)
                            {
                                XAttribute attLicenseType = elemAppReceipt.Attribute("LicenseType");
                                return attLicenseType.Value.ToUpper().CompareTo("FULL") == 0;
                            }
                        }
                    }

                    return false;
                }
                return false;
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("Unable to get the license information. Error: " + Ex.Message);
                return false;
            }
        }

        public string GetBuildNumber()
        {
            PackageVersion CurrentVersion = Package.Current.Id.Version;
            return string.Format("{0}", CurrentVersion.Build);
        }

        public string GetVersionNumber()
        {
            PackageVersion CurrentVersion = Package.Current.Id.Version;
            return string.Format("{0}.{1}.{2}", CurrentVersion.Major, CurrentVersion.Minor, CurrentVersion.Build);
        }

        public List<LicenseItem> GetThirdPartyLicenses()
        {
            List<LicenseItem> licenses = new List<LicenseItem>
            {
                new LicenseItem
                {
                    ThirdPartyName = "Microsoft.NETCore.UniversalWindowsPlatform",
                    LicenseCopyright = "(C) Microsoft",
                    LicenseUrl = "https://github.com/Microsoft/dotnet/blob/master/releases/UWP/LICENSE.TXT",
                    LicensePageType = typeof(WebViewPage),
                    BindingContext = "Licenses/MicrosoftUWP.htm"
                },
                new LicenseItem
                {
                    ThirdPartyName = "Otp.NET",
                    LicenseCopyright = "(C) 2017 Kyle Spearrin",
                    LicenseUrl = "https://raw.githubusercontent.com/kspearrin/Otp.NET/master/LICENSE.txt",
                    LicensePageType = typeof(WebViewPage),
                    BindingContext = "Licenses/OtpNET.htm"
                },
                new LicenseItem
                {
                    ThirdPartyName = "Wiry.Base32",
                    LicenseCopyright = "(C) Dmitry Razumikhin",
                    LicenseUrl = "https://opensource.org/licenses/MIT",
                    LicensePageType = typeof(WebViewPage),
                    BindingContext = "Licenses/WiryBase32.htm"
                }
            };
            return licenses;
        }
    }
}
