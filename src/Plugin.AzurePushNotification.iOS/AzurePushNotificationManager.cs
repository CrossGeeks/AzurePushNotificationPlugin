using Foundation;
using Plugin.AzurePushNotification.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UIKit;
using UserNotifications;
using WindowsAzure.Messaging;

namespace Plugin.AzurePushNotification
{
    /// <summary>
    /// Implementation for AzurePushNotification
    /// </summary>
    public class AzurePushNotificationManager : NSObject, IAzurePushNotification, IUNUserNotificationCenterDelegate
    {
      
        static NSString TagsKey = new NSString("Tags");
        static NSString TokenKey = new NSString("Token");
        NSString RegisteredKey = new NSString("Registered");

        static NSMutableArray _tags = (NSUserDefaults.StandardUserDefaults.ValueForKey(TagsKey) as NSArray ?? new NSArray()).MutableCopy() as NSMutableArray;
        public string[] Tags
        {
            get
            {

                //Load all subscribed topics
                IList<string> topics = new List<string>();
                for (nuint i = 0; i < _tags.Count; i++)
                {
                    topics.Add(_tags.GetItem<NSString>(i));
                }
                return topics.ToArray();
            }

        }
        public string Token { get { return NSUserDefaults.StandardUserDefaults.StringForKey(TokenKey) ?? string.Empty; } }

        public bool IsRegistered { get { return NSUserDefaults.StandardUserDefaults.BoolForKey(RegisteredKey); } }

        static SBNotificationHub Hub { get; set; }
  
    
        public NSData InternalToken
        {
            get
            {
                return NSUserDefaults.StandardUserDefaults.ValueForKey(TokenKey) as NSData;
            }
            set
            {
                NSUserDefaults.StandardUserDefaults.SetValueForKey(value, TokenKey);
                NSUserDefaults.StandardUserDefaults.Synchronize();
            }
        }
        public string[] SubscribedTags => throw new NotImplementedException();
        
        public IPushNotificationHandler NotificationHandler { get; set; }

        public static UNNotificationPresentationOptions CurrentNotificationPresentationOption { get; set; } = UNNotificationPresentationOptions.None;

        static IList<NotificationUserCategory> usernNotificationCategories = new List<NotificationUserCategory>();

        static AzurePushNotificationTokenEventHandler _onTokenRefresh;
        public event AzurePushNotificationTokenEventHandler OnTokenRefresh
        {
            add
            {
                _onTokenRefresh += value;
            }
            remove
            {
                _onTokenRefresh -= value;
            }
        }

        static AzurePushNotificationErrorEventHandler _onNotificationError;
        public event AzurePushNotificationErrorEventHandler OnNotificationError
        {
            add
            {
                _onNotificationError += value;
            }
            remove
            {
                _onNotificationError -= value;
            }
        }

        static AzurePushNotificationResponseEventHandler _onNotificationOpened;
        public event AzurePushNotificationResponseEventHandler OnNotificationOpened
        {
            add
            {
                _onNotificationOpened += value;
            }
            remove
            {
                _onNotificationOpened -= value;
            }
        }


        public NotificationUserCategory[] GetUserNotificationCategories()
        {
            return usernNotificationCategories?.ToArray();
        }


        static AzurePushNotificationDataEventHandler _onNotificationReceived;
        public event AzurePushNotificationDataEventHandler OnNotificationReceived
        {
            add
            {
                _onNotificationReceived += value;
            }
            remove
            {
                _onNotificationReceived -= value;
            }
        }

        static AzurePushNotificationDataEventHandler _onNotificationDeleted;
        public event AzurePushNotificationDataEventHandler OnNotificationDeleted
        {
            add
            {
                _onNotificationDeleted += value;
            }
            remove
            {
                _onNotificationDeleted -= value;
            }
        }
        public static async Task Initialize(string notificationHubConnectionString, string notificationHubPath, NSDictionary options)
        {
           
            Hub = new SBNotificationHub(notificationHubConnectionString, notificationHubPath);

            CrossAzurePushNotification.Current.NotificationHandler = CrossAzurePushNotification.Current.NotificationHandler ?? new DefaultPushNotificationHandler();

            TaskCompletionSource<bool> permisionTask = new TaskCompletionSource<bool>();

            // Register your app for remote notifications.
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                // iOS 10 or later
                var authOptions = UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound;
                UNUserNotificationCenter.Current.RequestAuthorization(authOptions, (granted, error) =>
                {
                    if (error != null)
                        _onNotificationError?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationErrorEventArgs(error.Description));
                    else
                        System.Diagnostics.Debug.WriteLine(granted);

                    permisionTask.SetResult(granted);
                });

                // For iOS 10 display notification (sent via APNS)
                UNUserNotificationCenter.Current.Delegate = CrossAzurePushNotification.Current as IUNUserNotificationCenterDelegate;

            }
            else
            {
                // iOS 9 or before
                var allNotificationTypes = UIUserNotificationType.Alert | UIUserNotificationType.Badge | UIUserNotificationType.Sound;
                var settings = UIUserNotificationSettings.GetSettingsForTypes(allNotificationTypes, null);
                UIApplication.SharedApplication.RegisterUserNotificationSettings(settings);
                permisionTask.SetResult(true);
            }

            var permissonGranted = await permisionTask.Task;

            if (!permissonGranted)
            {
                _onNotificationError?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationErrorEventArgs("Push notification permission not granted"));

            }

            UIApplication.SharedApplication.RegisterForRemoteNotifications();

        }

        public static async Task Initialize(string notificationHubConnectionString, string notificationHubPath, NSDictionary options, IPushNotificationHandler pushNotificationHandler)
        {
            CrossAzurePushNotification.Current.NotificationHandler = pushNotificationHandler;
            await Initialize(notificationHubConnectionString, notificationHubPath,options);
        }
        public static async Task Initialize(string notificationHubConnectionString, string notificationHubPath,NSDictionary options, NotificationUserCategory[] notificationUserCategories)
        {

            await Initialize(notificationHubConnectionString, notificationHubPath, options);
            RegisterUserNotificationCategories(notificationUserCategories);

        }

        static void RegisterUserNotificationCategories(NotificationUserCategory[] userCategories)
        {
            if (userCategories != null && userCategories.Length > 0)
            {
                usernNotificationCategories.Clear();
                IList<UNNotificationCategory> categories = new List<UNNotificationCategory>();
                foreach (var userCat in userCategories)
                {
                    IList<UNNotificationAction> actions = new List<UNNotificationAction>();

                    foreach (var action in userCat.Actions)
                    {

                        // Create action
                        var actionID = action.Id;
                        var title = action.Title;
                        var notificationActionType = UNNotificationActionOptions.None;
                        switch (action.Type)
                        {
                            case NotificationActionType.AuthenticationRequired:
                                notificationActionType = UNNotificationActionOptions.AuthenticationRequired;
                                break;
                            case NotificationActionType.Destructive:
                                notificationActionType = UNNotificationActionOptions.Destructive;
                                break;
                            case NotificationActionType.Foreground:
                                notificationActionType = UNNotificationActionOptions.Foreground;
                                break;

                        }


                        var notificationAction = UNNotificationAction.FromIdentifier(actionID, title, notificationActionType);

                        actions.Add(notificationAction);

                    }

                    // Create category
                    var categoryID = userCat.Category;
                    var notificationActions = actions.ToArray() ?? new UNNotificationAction[] { };
                    var intentIDs = new string[] { };
                    var categoryOptions = new UNNotificationCategoryOptions[] { };

                    var category = UNNotificationCategory.FromIdentifier(categoryID, notificationActions, intentIDs, userCat.Type == NotificationCategoryType.Dismiss ? UNNotificationCategoryOptions.CustomDismissAction : UNNotificationCategoryOptions.None);

                    categories.Add(category);

                    usernNotificationCategories.Add(userCat);

                }

                // Register categories
                UNUserNotificationCenter.Current.SetNotificationCategories(new NSSet<UNNotificationCategory>(categories.ToArray()));

            }

        }

        
        public async Task RegisterAsync(string[] tags)
        {
            if (Hub == null)
                return;



            await Task.Run(() =>
            {
                if (Token != null && Token.Length > 0)
                {
                    NSError errorFirst;

                    Hub.UnregisterAll(Token, out errorFirst);

                    if (errorFirst != null)
                    {
                        _onNotificationError?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationErrorEventArgs(errorFirst.Description));
                        System.Diagnostics.Debug.WriteLine($"AzurePushNotification - Unregister- Error - {errorFirst.Description}");

                        return;
                    }
                    
                    NSSet tagSet = new NSSet(tags);
                    NSError error;

                    Hub.RegisterNative(Token, tagSet.Count >0?tagSet:null, out error);

                    if (error != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"AzurePushNotification - Register- Error - {error.Description}");
                        _onNotificationError?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationErrorEventArgs(error.Description));

                    }
                    else
                    {
                        NSUserDefaults.StandardUserDefaults.SetBool(true, RegisteredKey);
                        NSUserDefaults.StandardUserDefaults.SetValueForKey(_tags, TagsKey);
                        NSUserDefaults.StandardUserDefaults.Synchronize();
                    }
                }
            });

        }

        public async Task UnregisterAsync()
        {

            if (Hub == null || string.IsNullOrEmpty(Token))
                return;

            await Task.Run(() =>
            {
                try
                {
                    NSError error;

                    Hub.UnregisterAll(Token, out error);

                    if (error != null)
                    {
                        _onNotificationError?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationErrorEventArgs(error.Description));
                        System.Diagnostics.Debug.WriteLine($"AzurePushNotification - Unregister- Error - {error.Description}");
                    }
                    else
                    {
                        NSUserDefaults.StandardUserDefaults.SetBool(false, RegisteredKey);
                        NSUserDefaults.StandardUserDefaults.SetValueForKey(new NSArray().MutableCopy(), TagsKey);
                        NSUserDefaults.StandardUserDefaults.Synchronize();
                    }
                }
                catch (Exception ex)
                {
                    _onNotificationError?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationErrorEventArgs(ex.Message));
                    System.Diagnostics.Debug.WriteLine($"AzurePushNotification - Unregister- Error - {ex.Message}");
                }

            });

        }

        // To receive notifications in foreground on iOS 10 devices.
        [Export("userNotificationCenter:willPresentNotification:withCompletionHandler:")]
        public void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
        {
            // Do your magic to handle the notification data
            System.Console.WriteLine(notification.Request.Content.UserInfo);
            System.Diagnostics.Debug.WriteLine("WillPresentNotification");
            var parameters = GetParameters(notification.Request.Content.UserInfo);
            _onNotificationReceived?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationDataEventArgs(parameters));
            //CrossPushNotification.Current.NotificationHandler?.OnReceived(parameters);
            completionHandler(CurrentNotificationPresentationOption);
        }

        [Export("userNotificationCenter:didReceiveNotificationResponse:withCompletionHandler:")]
        public void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
        {

            var parameters = GetParameters(response.Notification.Request.Content.UserInfo);

            NotificationCategoryType catType = NotificationCategoryType.Default;
            if (response.IsCustomAction)
                catType = NotificationCategoryType.Custom;
            else if (response.IsDismissAction)
                catType = NotificationCategoryType.Dismiss;

            var notificationResponse = new NotificationResponse(parameters, $"{response.ActionIdentifier}".Equals("com.apple.UNNotificationDefaultActionIdentifier", StringComparison.CurrentCultureIgnoreCase) ? string.Empty : $"{response.ActionIdentifier}", catType);
            _onNotificationOpened?.Invoke(this, new AzurePushNotificationResponseEventArgs(notificationResponse.Data, notificationResponse.Identifier, notificationResponse.Type));

            CrossAzurePushNotification.Current.NotificationHandler?.OnOpened(notificationResponse);

            // Inform caller it has been handled
            completionHandler();
        }

        public static async void DidRegisterRemoteNotifications(NSData deviceToken)
        {
            string trimmedDeviceToken = deviceToken.Description;
            if (!string.IsNullOrWhiteSpace(trimmedDeviceToken))
            {
                trimmedDeviceToken = trimmedDeviceToken.Trim('<');
                trimmedDeviceToken = trimmedDeviceToken.Trim('>');
                trimmedDeviceToken = trimmedDeviceToken.Trim();
                trimmedDeviceToken = trimmedDeviceToken.Replace(" ", "");
            }

            NSUserDefaults.StandardUserDefaults.SetString(trimmedDeviceToken,TokenKey);
            _onTokenRefresh?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationTokenEventArgs(trimmedDeviceToken));

            await CrossAzurePushNotification.Current.RegisterAsync(CrossAzurePushNotification.Current.Tags);
        }

        public static void DidReceiveMessage(NSDictionary data)
        {
            var parameters = GetParameters(data);

            _onNotificationReceived?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationDataEventArgs(parameters));

            CrossAzurePushNotification.Current.NotificationHandler?.OnReceived(parameters);
            System.Diagnostics.Debug.WriteLine("DidReceivedMessage");
        }

        public static void RemoteNotificationRegistrationFailed(NSError error)
        {
            _onNotificationError?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationErrorEventArgs(error.Description));
        }

        static IDictionary<string, object> GetParameters(NSDictionary data)
        {
            var parameters = new Dictionary<string, object>();

            var keyAps = new NSString("aps");
            var keyAlert = new NSString("alert");

            foreach (var val in data)
            {
                if (val.Key.Equals(keyAps))
                {
                    NSDictionary aps = data.ValueForKey(keyAps) as NSDictionary;

                    if (aps != null)
                    {
                        foreach (var apsVal in aps)
                        {
                            if (apsVal.Value is NSDictionary)
                            {
                                if (apsVal.Key.Equals(keyAlert))
                                {
                                    foreach (var alertVal in apsVal.Value as NSDictionary)
                                    {
                                        parameters.Add($"aps.alert.{alertVal.Key}", $"{alertVal.Value}");
                                    }
                                }
                            }
                            else
                            {
                                parameters.Add($"aps.{apsVal.Key}", $"{apsVal.Value}");
                            }

                        }
                    }
                }
                else
                {
                    parameters.Add($"{val.Key}", $"{val.Value}");
                }

            }


            return parameters;
        }


    }
}