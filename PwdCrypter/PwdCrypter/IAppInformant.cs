using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PwdCrypter
{
    /// <summary>
    /// Classe che rappresenta un elemento della lista delle licenze
    /// dei componenti di terze parti
    /// </summary>
    public class LicenseItem
    {
        public string ThirdPartyName { get; set; }
        public string LicenseCopyright { get; set; }
        public string LicenseUrl { get; set; }
        public Type LicensePageType { get; set; }
        public object BindingContext { get; set; }
    }

    public interface IAppInformant
    {
        string GetVersionNumber();
        string GetBuildNumber();
        Task<bool> AppWasPurchased();
        List<LicenseItem> GetThirdPartyLicenses();
    }
}
