using Android.Content.PM;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Dependency(typeof(PwdCrypter.Droid.AppInformant))]
namespace PwdCrypter.Droid
{
    public class AppInformant : IAppInformant
    {
        public async Task<bool> AppWasPurchased()
        {
            // Non è mai stata pubblicato per Android in precedenza
            return await Task.FromResult(false);
        }

        public string GetBuildNumber()
        {
            var context = Android.App.Application.Context;
            return context.PackageManager.GetPackageInfo(context.PackageName, PackageInfoFlags.MetaData).LongVersionCode.ToString();
        }

        public List<LicenseItem> GetThirdPartyLicenses()
        {
            List<LicenseItem> licenses = new List<LicenseItem>
            {
                new LicenseItem
                {
                    ThirdPartyName = "Com.OneSignal",
                    LicenseCopyright = "(C) 2016 OneSignal",
                    LicenseUrl = "https://github.com/OneSignal/OneSignal-Xamarin-SDK/blob/master/LICENSE",
                    LicensePageType = typeof(WebViewPage),
                    BindingContext = "Licenses/OneSignal.htm"
                },
                new LicenseItem
                {
                    ThirdPartyName = "Microsoft.Net.Http",
                    LicenseCopyright = "(C) Microsoft",
                    LicenseUrl = "https://dotnet.microsoft.com/en/dotnet_library_license.htm",
                    LicensePageType = typeof(WebViewPage),
                    BindingContext = "Licenses/Microsoft.htm"
                },
                new LicenseItem
                {
                    ThirdPartyName = "Xamarin.Android.Support.Compat",
                    LicenseCopyright = "(C) .NET Foundation Contributors",
                    LicenseUrl = "https://github.com/xamarin/AndroidSupportComponents/blob/master/LICENSE.md",
                    LicensePageType = typeof(WebViewPage),
                    BindingContext = "Licenses/NETFoundationContributors.htm"
                },
                new LicenseItem
                {
                    ThirdPartyName = "Xamarin.Android.Support.CustomTabs",
                    LicenseCopyright = "(C) .NET Foundation Contributors",
                    LicenseUrl = "https://github.com/xamarin/AndroidSupportComponents/blob/master/LICENSE.md",
                    LicensePageType = typeof(WebViewPage),
                    BindingContext = "Licenses/NETFoundationContributors.htm"
                },
                new LicenseItem
                {
                    ThirdPartyName = "Xamarin.Android.Support.Design",
                    LicenseCopyright = "(C) .NET Foundation Contributors",
                    LicenseUrl = "https://github.com/xamarin/AndroidSupportComponents/blob/master/LICENSE.md",
                    LicensePageType = typeof(WebViewPage),
                    BindingContext = "Licenses/NETFoundationContributors.htm"
                },
                new LicenseItem
                {
                    ThirdPartyName = "Xamarin.Android.Support.v4",
                    LicenseCopyright = "(C) .NET Foundation Contributors",
                    LicenseUrl = "https://github.com/xamarin/AndroidSupportComponents/blob/master/LICENSE.md",
                    LicensePageType = typeof(WebViewPage),
                    BindingContext = "Licenses/NETFoundationContributors.htm"
                },
                new LicenseItem
                {
                    ThirdPartyName = "Xamarin.Android.Support.v7.AppCompat",
                    LicenseCopyright = "(C) .NET Foundation Contributors",
                    LicenseUrl = "https://github.com/xamarin/AndroidSupportComponents/blob/master/LICENSE.md",
                    LicensePageType = typeof(WebViewPage),
                    BindingContext = "Licenses/NETFoundationContributors.htm"
                },
                new LicenseItem
                {
                    ThirdPartyName = "Xamarin.Android.Support.v7.CardView",
                    LicenseCopyright = "(C) .NET Foundation Contributors",
                    LicenseUrl = "https://github.com/xamarin/AndroidSupportComponents/blob/master/LICENSE.md",
                    LicensePageType = typeof(WebViewPage),
                    BindingContext = "Licenses/NETFoundationContributors.htm"
                },
                new LicenseItem
                {
                    ThirdPartyName = "Xamarin.Android.Support.v7.MediaRouter",
                    LicenseCopyright = "(C) .NET Foundation Contributors",
                    LicenseUrl = "https://github.com/xamarin/AndroidSupportComponents/blob/master/LICENSE.md",
                    LicensePageType = typeof(WebViewPage),
                    BindingContext = "Licenses/NETFoundationContributors.htm"
                },
                new LicenseItem
                {
                    ThirdPartyName = "Xamarin.AndroidX.MediaRouter",
                    LicenseCopyright = "(C) .NET Foundation Contributors",
                    LicenseUrl = "https://www.nuget.org/packages/Xamarin.AndroidX.MediaRouter/1.1.0/license",
                    LicensePageType = typeof(WebViewPage),
                    BindingContext = "Licenses/NETFoundationContributors.htm"
                },
                new LicenseItem
                {
                    ThirdPartyName = "Xamarin.GooglePlayServices.Location",
                    LicenseCopyright = "(C) .NET Foundation Contributors",
                    LicenseUrl = "https://github.com/xamarin/GooglePlayServicesComponents/blob/master/LICENSE.md",
                    LicensePageType = typeof(WebViewPage),
                    BindingContext = "Licenses/NETFoundationContributors.htm"
                },
                new LicenseItem
                {
                    ThirdPartyName = "Xamarin.AndroidX.Work.Runtime",
                    LicenseCopyright = "(C) .NET Foundation Contributors",
                    LicenseUrl = "https://www.nuget.org/packages/Xamarin.AndroidX.Work.Runtime/2.3.4.1/license",
                    LicensePageType = typeof(WebViewPage),
                    BindingContext = "Licenses/NETFoundationContributors.htm"
                },
                new LicenseItem
                {
                    ThirdPartyName = "Xamarin.AndroidX.AppCompat.Resources",
                    LicenseCopyright = "(C) .NET Foundation Contributors",
                    LicenseUrl = "https://www.nuget.org/packages/Xamarin.AndroidX.AppCompat.Resources/1.1.0.1/license",
                    LicensePageType = typeof(WebViewPage),
                    BindingContext = "Licenses/NETFoundationContributors.htm"
                }
            };
            return licenses;
        }

        public string GetVersionNumber()
        {
            var context = Android.App.Application.Context;
            return context.PackageManager.GetPackageInfo(context.PackageName, PackageInfoFlags.MetaData).VersionName;
        }
    }
}