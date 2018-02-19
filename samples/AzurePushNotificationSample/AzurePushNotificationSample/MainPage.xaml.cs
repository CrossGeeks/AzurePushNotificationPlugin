using Plugin.AzurePushNotification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace AzurePushNotificationSample
{
	public partial class MainPage : ContentPage
	{
        public string Message
        {
            get
            {
                return textLabel.Text;
            }
            set
            {
                textLabel.Text = value;
            }
        }
        public MainPage()
		{
			InitializeComponent();

            CrossAzurePushNotification.Current.OnNotificationReceived += (s, p) =>
            {
                    System.Diagnostics.Debug.WriteLine("Received");
                    if (p.Data.ContainsKey("body"))
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            textLabel.Text = $"{p.Data["body"]}";
                        });

                    }
            };
        }
	}
}
