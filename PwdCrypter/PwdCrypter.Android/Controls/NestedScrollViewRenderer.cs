using Android.Content;
using PwdCrypter.Controls;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(NestedScrollView), typeof(PwdCrypter.Droid.Controls.NestedScrollViewRenderer))]
namespace PwdCrypter.Droid.Controls
{
    /// <summary>
    /// Permette di raggigere il problema della scroll bar verticale non visibile se all'interno c'è
    /// una webview o lista.
    /// </summary>
    public class NestedScrollViewRenderer : ViewRenderer<NestedScrollView, Android.Widget.ScrollView>
    {
        public NestedScrollViewRenderer(Context context) : base(context)
        {
        }
    }
}