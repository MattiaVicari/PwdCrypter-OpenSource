using PwdCrypter.Controls;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(CustomTimePicker), typeof(PwdCrypter.UWP.Controls.CustomTimePickerRenderer))]
namespace PwdCrypter.UWP.Controls
{
    class CustomTimePickerRenderer : TimePickerRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<TimePicker> e)
        {
            base.OnElementChanged(e);
            if (Control != null)
            {
                CustomTimePicker customControl = (CustomTimePicker)Element;
                Control.MinuteIncrement = customControl.MinutesInterval;
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            if (e.PropertyName == nameof(CustomTimePicker.MinutesInterval))
            {
                CustomTimePicker customControl = (CustomTimePicker)Element;
                Control.MinuteIncrement = customControl.MinutesInterval;
            }
        }
    }
}
