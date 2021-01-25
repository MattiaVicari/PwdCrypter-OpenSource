using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace PwdCrypter.UWP
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            try
            {
                this.InitializeComponent();

                LoadApplication(new PwdCrypter.App());
            }
            catch(Exception e)
            {
                PwdCrypter.App.Logger.Error("App loading failed. Error: " + e.Message);
                throw;
            }
        }
    }
}
