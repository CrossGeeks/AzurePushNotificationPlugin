using System.Collections.Generic;
using System.Linq;

using Android.App;
using Android.Content;
using Firebase.Messaging;

namespace Plugin.AzurePushNotification
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class PNMessagingService : FirebaseMessagingService
    {
        public override void OnMessageReceived(RemoteMessage message)
        {
            var parameters = new Dictionary<string, object>();
            var notification = message.GetNotification();
            if (notification != null)
            {
                if (!string.IsNullOrEmpty(notification.Body))
                    parameters.Add("body", notification.Body);

                if (!string.IsNullOrEmpty(notification.BodyLocalizationKey))
                    parameters.Add("body_loc_key", notification.BodyLocalizationKey);

                var bodyLocArgs = notification.GetBodyLocalizationArgs();
                if (bodyLocArgs != null && bodyLocArgs.Any())
                    parameters.Add("body_loc_args", bodyLocArgs);

                if (!string.IsNullOrEmpty(notification.Title))
                    parameters.Add("title", notification.Title);

                if (!string.IsNullOrEmpty(notification.TitleLocalizationKey))
                    parameters.Add("title_loc_key", notification.TitleLocalizationKey);

                var titleLocArgs = notification.GetTitleLocalizationArgs();
                if (titleLocArgs != null && titleLocArgs.Any())
                    parameters.Add("title_loc_args", titleLocArgs);

                if (!string.IsNullOrEmpty(notification.Tag))
                    parameters.Add("tag", notification.Tag);

                if (!string.IsNullOrEmpty(notification.Sound))
                    parameters.Add("sound", notification.Sound);

                if (!string.IsNullOrEmpty(notification.Icon))
                    parameters.Add("icon", notification.Icon);

                if (notification.Link != null)
                    parameters.Add("link_path", notification.Link.Path);

                if (!string.IsNullOrEmpty(notification.ClickAction))
                    parameters.Add("click_action", notification.ClickAction);

                if (!string.IsNullOrEmpty(notification.Color))
                    parameters.Add("color", notification.Color);
            }
            foreach (var d in message.Data)
            {
                if (!parameters.ContainsKey(d.Key))
                {
                    if((d.Key.Equals("title_loc_args") || d.Key.Equals("body_loc_args")))
                    {
                        if(d.Value.StartsWith("[") && d.Value.EndsWith("]") && d.Value.Length > 2)
                        {
                            var arrayValues = d.Value.Substring(1, d.Value.Length - 2);
                            parameters.Add(d.Key, arrayValues.Split(","));
                        }
                        else
                        {
                            parameters.Add(d.Key, new string[] { });
                        }
                    
                    }
                    else
                    {
                        parameters.Add(d.Key, d.Value);
                    }
                }
                  
            }

            AzurePushNotificationManager.RegisterData(parameters);
            CrossAzurePushNotification.Current.NotificationHandler?.OnReceived(parameters);
        }

        public override void OnNewToken(string p0)
        {
            AzurePushNotificationManager.RegisterToken(p0);
            System.Diagnostics.Debug.WriteLine($"REFRESHED TOKEN: {p0}");
        }
    }
}