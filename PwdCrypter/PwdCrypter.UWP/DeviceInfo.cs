using System;
using Windows.Storage.Streams;
using Xamarin.Forms;

[assembly: Dependency(typeof(PwdCrypter.UWP.DeviceInfo))]
namespace PwdCrypter.UWP
{
    class DeviceInfo : IDeviceInfo
    {
        public string GetDeviceID()
        {
            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.System.Profile.HardwareIdentification"))
            {
                Windows.System.Profile.HardwareToken token = Windows.System.Profile.HardwareIdentification.GetPackageSpecificToken(null);
                IBuffer hardwareId = token.Id;
                DataReader hdIdReader = DataReader.FromBuffer(hardwareId);

                byte[] bytes = new byte[hardwareId.Length];
                hdIdReader.ReadBytes(bytes);

                return BitConverter.ToString(bytes);
            }
            return "";
        }
    }
}
