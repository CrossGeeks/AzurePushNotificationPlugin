using Plugin.AzurePushNotification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace AzurePushNotificationSample
{

	public partial class App : Application
	{

        public App ()
		{
			InitializeComponent();

			MainPage = new AzurePushNotificationSample.MainPage();
		}

        protected override async void OnStart()
        {
           
           // Handle when your app starts
           CrossAzurePushNotification.Current.OnTokenRefresh += (s, p) =>
            {
                System.Diagnostics.Debug.WriteLine($"TOKEN : {p.Token}");
            };


            CrossAzurePushNotification.Current.OnNotificationOpened += (s, p) =>
            {
                System.Diagnostics.Debug.WriteLine("Opened");
                foreach (var data in p.Data)
                {
                    System.Diagnostics.Debug.WriteLine($"{data.Key} : {data.Value}");
                }

                if (!string.IsNullOrEmpty(p.Identifier))
                {
                    System.Diagnostics.Debug.WriteLine($"ActionId: {p.Identifier}");
                }

            };

            CrossAzurePushNotification.Current.OnNotificationError += (s, p) =>
            {
                System.Diagnostics.Debug.WriteLine($"ERROR : {p.Message}");
            };

            await CrossAzurePushNotification.Current.RegisterAsync(new string[] { "crossgeeks", "general" });
        }

        protected override void OnSleep()
        {
         
        }

        protected override void OnResume()
        {

        }
    }
}
