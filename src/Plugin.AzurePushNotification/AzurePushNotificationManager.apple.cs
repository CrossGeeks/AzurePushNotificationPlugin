using Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        static NotificationResponse delayedNotificationResponse = null;
        static NSData DeviceToken { get; set; }
        static NSString TagsKey = new NSString("Tags");
        static NSString TokenKey;
        static string PushRegisteredKey;

        static NSMutableArray _tags = (NSUserDefaults.StandardUserDefaults.ValueForKey(TagsKey) as NSArray ?? new NSArray()).MutableCopy() as NSMutableArray;
        public string[] Tags
        {
            get
            {
                //Load all subscribed topics
                IList<string> topics = new List<string>();
                if (_tags != null)
                {
                    for (nuint i = 0; i < _tags.Count; i++)
                    {
                        topics.Add(_tags.GetItem<NSString>(i));
                    }
                }

                return topics.ToArray();
            }

        }


        public Func<string> RetrieveSavedToken { get; set; } = InternalRetrieveSavedToken;
        public Action<string> SaveToken { get; set; } = InternalSaveToken;


        public string Token
        {
            get
            {
                return RetrieveSavedToken?.Invoke() ?? string.Empty;
            }
            internal set
            {
                SaveToken?.Invoke(value);
            }
        }



        internal static string InternalRetrieveSavedToken()
        {
            return !string.IsNullOrEmpty(TokenKey)?NSUserDefaults.StandardUserDefaults.StringForKey(TokenKey):null;
        }

        internal static void InternalSaveToken(string token)
        {
            NSUserDefaults.StandardUserDefaults.SetString(token, TokenKey);
        }

        public bool IsRegistered { get { return NSUserDefaults.StandardUserDefaults.BoolForKey(PushRegisteredKey); } }

        public bool IsEnabled { get { return UIApplication.SharedApplication.IsRegisteredForRemoteNotifications; } }


        static SBNotificationHub Hub { get; set; }


        public static NSData InternalToken
        {
            get
            {
                return !string.IsNullOrEmpty(TokenKey) ? (NSUserDefaults.StandardUserDefaults.ValueForKey(TokenKey) as NSData):null;
            }
            set
            {
                if (!string.IsNullOrEmpty(TokenKey))
                {
                    NSUserDefaults.StandardUserDefaults.SetValueForKey(value, TokenKey);
                    NSUserDefaults.StandardUserDefaults.Synchronize();
                }
            }
        }

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
                var previousVal = _onNotificationOpened;
                _onNotificationOpened += value;
                if (delayedNotificationResponse != null && previousVal == null)
                {
                    var tmpParams = delayedNotificationResponse;
                    _onNotificationOpened?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationResponseEventArgs(tmpParams.Data, tmpParams.Identifier, tmpParams.Type));
                    delayedNotificationResponse = null;
                }
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
        public static void Initialize(string notificationHubConnectionString, string notificationHubPath, NSDictionary options, bool autoRegistration = true, bool enableDelayedResponse = true)
        {

            Hub = new SBNotificationHub(notificationHubConnectionString, notificationHubPath);
            TokenKey = new NSString($"{notificationHubPath}_Token");
            PushRegisteredKey = $"{notificationHubPath}_PushRegistered";
            CrossAzurePushNotification.Current.NotificationHandler = CrossAzurePushNotification.Current.NotificationHandler ?? new DefaultPushNotificationHandler();

            /*if (options?.ContainsKey(UIApplication.LaunchOptionsRemoteNotificationKey) ?? false)
            {
                var pushPayload = options[UIApplication.LaunchOptionsRemoteNotificationKey] as NSDictionary;
                if (pushPayload != null)
                {
                    var parameters = GetParameters(pushPayload);

                    var notificationResponse = new NotificationResponse(parameters, string.Empty, NotificationCategoryType.Default);

                    if (_onNotificationOpened == null && enableDelayedResponse)
                        delayedNotificationResponse = notificationResponse;
                    else
                        _onNotificationOpened?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationResponseEventArgs(notificationResponse.Data, notificationResponse.Identifier, notificationResponse.Type));

                    CrossAzurePushNotification.Current.NotificationHandler?.OnOpened(notificationResponse);
                }
            }*/

            if (autoRegistration)
            {
                CrossAzurePushNotification.Current.RegisterForPushNotifications();
            }


        }

        public static void Initialize(string notificationHubConnectionString, string notificationHubPath, NSDictionary options, IPushNotificationHandler pushNotificationHandler, bool autoRegistration = true, bool enableDelayedResponse = true)
        {
            CrossAzurePushNotification.Current.NotificationHandler = pushNotificationHandler;
            Initialize(notificationHubConnectionString, notificationHubPath, options, autoRegistration, enableDelayedResponse);
        }
        public static void Initialize(string notificationHubConnectionString, string notificationHubPath, NSDictionary options, NotificationUserCategory[] notificationUserCategories, bool autoRegistration = true, bool enableDelayedResponse = true)
        {

            Initialize(notificationHubConnectionString, notificationHubPath, options, autoRegistration, enableDelayedResponse);
            RegisterUserNotificationCategories(notificationUserCategories);

        }

        public void RegisterForPushNotifications()
        {

            // Register your app for remote notifications.
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                // iOS 10 or later
                var authOptions = UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound;

                // For iOS 10 display notification (sent via APNS)
                UNUserNotificationCenter.Current.Delegate = CrossAzurePushNotification.Current as IUNUserNotificationCenterDelegate;

                UNUserNotificationCenter.Current.RequestAuthorization(authOptions, (granted, error) =>
                {
                    if (error != null)
                    {
                        _onNotificationError?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationErrorEventArgs(AzurePushNotificationErrorType.PermissionDenied, error.Description));
                    }
                    else if (!granted)
                    {
                        _onNotificationError?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationErrorEventArgs(AzurePushNotificationErrorType.PermissionDenied, "Push notification permission not granted"));
                    }
                    else
                    {
                        this.InvokeOnMainThread(() => UIApplication.SharedApplication.RegisterForRemoteNotifications());
                    }
                });
            }
            else
            {
                // iOS 9 or before
                var allNotificationTypes = UIUserNotificationType.Alert | UIUserNotificationType.Badge | UIUserNotificationType.Sound;
                var settings = UIUserNotificationSettings.GetSettingsForTypes(allNotificationTypes, null);
                UIApplication.SharedApplication.RegisterUserNotificationSettings(settings);
                UIApplication.SharedApplication.RegisterForRemoteNotifications();
            }
        }

        public void UnregisterForPushNotifications()
        {
            UIApplication.SharedApplication.UnregisterForRemoteNotifications();
            Token = string.Empty;
            InternalToken = null;
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
            System.Diagnostics.Debug.WriteLine($"AzurePushNotification - Register - Start");
            if (Hub == null)
                return;

            if (tags != null && tags.Length > 0)
            {
                _tags = NSArray.FromStrings(tags).MutableCopy() as NSMutableArray;
            }
            else
            {
                _tags = null;
            }

            if (DeviceToken == null || DeviceToken.Length == 0)
                return;

            await Task.Run(() =>
            {
                NSError errorFirst;
                if ((InternalToken != null && InternalToken.Length > 0) && IsRegistered)
                {
                    Hub.UnregisterAll(InternalToken, out errorFirst);

                    if (errorFirst != null)
                    {
                        _onNotificationError?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationErrorEventArgs(AzurePushNotificationErrorType.NotificationHubUnregistrationFailed, errorFirst.Description));
                        System.Diagnostics.Debug.WriteLine($"AzurePushNotification - Unregister- Error - {errorFirst.Description}");

                        return;
                    }
                }

                NSSet tagSet = null;
                if (tags != null && tags.Length > 0)
                {
                    tagSet = new NSSet(tags);
                }

                NSError error;

                Hub.RegisterNative(DeviceToken, tagSet, out error);

                if (error != null)
                {
                    System.Diagnostics.Debug.WriteLine($"AzurePushNotification - Register- Error - {error.Description}");
                    _onNotificationError?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationErrorEventArgs(AzurePushNotificationErrorType.NotificationHubRegistrationFailed, error.Description));

                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"AzurePushNotification - Registered - ${_tags}");

                    NSUserDefaults.StandardUserDefaults.SetBool(true, PushRegisteredKey);
                    NSUserDefaults.StandardUserDefaults.SetValueForKey(_tags ?? new NSArray().MutableCopy(), TagsKey);
                    NSUserDefaults.StandardUserDefaults.Synchronize();
                }
            });

        }

        public async Task UnregisterAsync()
        {

            if (Hub == null || InternalToken == null || InternalToken.Length == 0)
                return;

            await Task.Run(() =>
            {
                try
                {
                    NSError error;

                    Hub.UnregisterAll(InternalToken, out error);

                    if (error != null)
                    {
                        _onNotificationError?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationErrorEventArgs(AzurePushNotificationErrorType.NotificationHubUnregistrationFailed, error.Description));
                        System.Diagnostics.Debug.WriteLine($"AzurePushNotification - Unregister- Error - {error.Description}");
                    }
                    else
                    {
                        _tags = new NSArray().MutableCopy() as NSMutableArray;
                        NSUserDefaults.StandardUserDefaults.SetBool(false, PushRegisteredKey);
                        NSUserDefaults.StandardUserDefaults.SetValueForKey(_tags, TagsKey);
                        NSUserDefaults.StandardUserDefaults.Synchronize();
                    }
                }
                catch (Exception ex)
                {
                    _onNotificationError?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationErrorEventArgs(AzurePushNotificationErrorType.NotificationHubUnregistrationFailed, ex.Message));
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
            CrossAzurePushNotification.Current.NotificationHandler?.OnReceived(parameters);

            string[] priorityKeys = new string[] { "priority", "aps.priority" };


            foreach(var pKey in priorityKeys)
            {
                if(parameters.TryGetValue(pKey, out object priority))
                {
                    var priorityValue = $"{priority}".ToLower();
                    switch(priorityValue)
                    {
                        case "max":
                        case "high":
                            if (!CurrentNotificationPresentationOption.HasFlag(UNNotificationPresentationOptions.Alert))
                            {
                                CurrentNotificationPresentationOption |= UNNotificationPresentationOptions.Alert;

                            }

                            if (!CurrentNotificationPresentationOption.HasFlag(UNNotificationPresentationOptions.Sound))
                            {
                                CurrentNotificationPresentationOption |= UNNotificationPresentationOptions.Sound;

                            }
                            break;
                        case "low":
                        case "min":
                        case "default":
                        default:
                            if (CurrentNotificationPresentationOption.HasFlag(UNNotificationPresentationOptions.Alert))
                            {
                                CurrentNotificationPresentationOption &= ~UNNotificationPresentationOptions.Alert;

                            }
                            break;
                    }

                    break;
                }
            }


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
            var length = (int)deviceToken.Length;
            if (length == 0)
            {
                return;
            }

            var hex = new StringBuilder(length * 2);
            foreach (var b in deviceToken)
            {
                hex.AppendFormat("{0:x2}", b);
            }


            var cleanedDeviceToken = hex.ToString();

            DeviceToken = deviceToken;

            _onTokenRefresh?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationTokenEventArgs(cleanedDeviceToken));

            await CrossAzurePushNotification.Current.RegisterAsync(CrossAzurePushNotification.Current.Tags);

            CrossAzurePushNotification.Current.SaveToken?.Invoke(cleanedDeviceToken);
            InternalToken = deviceToken;
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

            _onNotificationError?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationErrorEventArgs(AzurePushNotificationErrorType.RegistrationFailed, error.Description));
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
                    if (data.ValueForKey(keyAps) is NSDictionary aps)
                    {
                        foreach (var apsVal in aps)
                        {
                            if (apsVal.Value is NSDictionary)
                            {
                                if (apsVal.Key.Equals(keyAlert))
                                {
                                    foreach (var alertVal in apsVal.Value as NSDictionary)
                                    {
                                        if (alertVal.Value is NSDictionary)
                                        {
                                            var value = ((NSDictionary)alertVal.Value).ToJson();
                                            parameters.Add($"aps.alert.{alertVal.Key}", value);
                                        }
                                        else
                                        {
                                            parameters.Add($"aps.alert.{alertVal.Key}", $"{alertVal.Value}");
                                        }
                                    }
                                }
                                else
                                {
                                    var value = ((NSDictionary)apsVal.Value).ToJson();
                                    parameters.Add($"aps.{apsVal.Key}", value);
                                }
                            }
                            else
                            {
                                parameters.Add($"aps.{apsVal.Key}", $"{apsVal.Value}");
                            }
                        }
                    }
                }
                else if (val.Value is NSDictionary)
                {
                    var value = ((NSDictionary)val.Value).ToJson();
                    parameters.Add($"{val.Key}", value);
                }
                else
                {
                    parameters.Add($"{val.Key}", $"{val.Value}");
                }

            }


            return parameters;
        }

        public void ClearAllNotifications()
        {
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                UNUserNotificationCenter.Current.RemoveAllDeliveredNotifications(); // To remove all delivered notifications
            }
            else
            {
                UIApplication.SharedApplication.CancelAllLocalNotifications();
            }
        }
        public void RemoveNotification(string tag, int id)
        {
            RemoveNotification(id);
        }
        public async void RemoveNotification(int id)
        {
            string NotificationIdKey = "id";
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {

                var deliveredNotifications = await UNUserNotificationCenter.Current.GetDeliveredNotificationsAsync();
                var deliveredNotificationsMatches = deliveredNotifications.Where(u => $"{u.Request.Content.UserInfo[NotificationIdKey]}".Equals($"{id}")).Select(s => s.Request.Identifier).ToArray();
                if (deliveredNotificationsMatches.Length > 0)
                {
                    UNUserNotificationCenter.Current.RemoveDeliveredNotifications(deliveredNotificationsMatches);

                }
            }
            else
            {
                var scheduledNotifications = UIApplication.SharedApplication.ScheduledLocalNotifications.Where(u => u.UserInfo[NotificationIdKey].Equals($"{id}"));
                //  var notification = notifications.Where(n => n.UserInfo.ContainsKey(NSObject.FromObject(NotificationKey)))  
                //         .FirstOrDefault(n => n.UserInfo[NotificationKey].Equals(NSObject.FromObject(id)));
                foreach (var notification in scheduledNotifications)
                {
                    UIApplication.SharedApplication.CancelLocalNotification(notification);
                }

            }
        }

    }

    public static class HelperExtensions
    {
        public static string ToJson(this NSDictionary dictionary)
        {
            var json = NSJsonSerialization.Serialize(dictionary,
            NSJsonWritingOptions.SortedKeys, out NSError error);
            return json.ToString(NSStringEncoding.UTF8);
        }
    }
}
