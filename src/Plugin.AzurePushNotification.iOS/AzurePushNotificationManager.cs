using Plugin.AzurePushNotification.Abstractions;
using System;


namespace Plugin.AzurePushNotification
{
    /// <summary>
    /// Implementation for AzurePushNotification
    /// </summary>
    public class AzurePushNotificationManager : IAzurePushNotification
    {
        public string[] Subscribedtags => throw new NotImplementedException();

        public IPushNotificationHandler NotificationHandler { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string Token => throw new NotImplementedException();

        public event AzurePushNotificationTokenEventHandler OnTokenRefresh;
        public event AzurePushNotificationResponseEventHandler OnNotificationOpened;
        public event AzurePushNotificationDataEventHandler OnNotificationReceived;
        public event AzurePushNotificationErrorEventHandler OnNotificationError;

        public NotificationUserCategory[] GetUserNotificationCategories()
        {
            throw new NotImplementedException();
        }

        public void Register(string[] tags)
        {
            throw new NotImplementedException();
        }

        public void Register(string tag)
        {
            throw new NotImplementedException();
        }

        public void Unregister(string tag)
        {
            throw new NotImplementedException();
        }

        public void Unregister(string[] tags)
        {
            throw new NotImplementedException();
        }

        public void UnregisterAll()
        {
            throw new NotImplementedException();
        }
    }
}