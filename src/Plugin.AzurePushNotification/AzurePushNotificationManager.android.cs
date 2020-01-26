using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Firebase.Iid;
using Firebase.Messaging;
using Java.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WindowsAzure.Messaging;

namespace Plugin.AzurePushNotification
{
    /// <summary>
    /// Implementation for Feature
    /// </summary>
    public class AzurePushNotificationManager : Java.Lang.Object, IAzurePushNotification, Android.Gms.Tasks.IOnCompleteListener
    {
      
        static NotificationHub Hub;
        static string DeviceToken { get; set; }
        static ICollection<string> _tags = Application.Context.GetSharedPreferences(KeyGroupName, FileCreationMode.Private).GetStringSet(TagsKey, new Collection<string>());
        public bool IsRegistered { get { return Application.Context.GetSharedPreferences(KeyGroupName, FileCreationMode.Private).GetBoolean(RegisteredKey, false); } }

        public string[] Tags { get { return _tags?.ToArray(); } }
        
        static NotificationResponse delayedNotificationResponse = null;
        internal const string KeyGroupName = "Plugin.AzurePushNotification";
        internal const string TagsKey = "TagsKey";
        internal static string TokenKey;
        internal static string RegisteredKey;
        internal const string AppVersionCodeKey = "AppVersionCodeKey";
        internal const string AppVersionNameKey = "AppVersionNameKey";
        internal const string AppVersionPackageNameKey = "AppVersionPackageNameKey";

        static IList<NotificationUserCategory> userNotificationCategories = new List<NotificationUserCategory>();
        public static string NotificationContentTitleKey { get; set; }
        public static string NotificationContentTextKey { get; set; }
        public static string NotificationContentDataKey { get; set; }
        public static int IconResource { get; set; }
        public static int LargeIconResource { get; set; }
        public static Android.Net.Uri SoundUri { get; set; }
        public static Color? Color { get; set; }
        public static Type NotificationActivityType { get; set; }
        public static ActivityFlags? NotificationActivityFlags { get; set; } = ActivityFlags.ClearTop | ActivityFlags.SingleTop;
        public static string DefaultNotificationChannelId { get; set; } = "AzurePushNotificationChannel";
        public static string DefaultNotificationChannelName { get; set; } = "General";

        static Context _context;

        internal static Type DefaultNotificationActivityType { get; set; } = null;

        static TaskCompletionSource<string> _tokenTcs;

        public Func<string> RetrieveSavedToken { get; set; } = InternalRetrieveSavedToken;
        public Action<string> SaveToken { get; set; } = InternalSaveToken;


        public string Token
        {
            get
            {
                return !string.IsNullOrEmpty(TokenKey)? (RetrieveSavedToken?.Invoke() ?? string.Empty): null;
            }
            internal set
            {
                if(!string.IsNullOrEmpty(TokenKey))
                {
                    SaveToken?.Invoke(value);
                }
            
            }
        }
        public static void ProcessIntent(Activity activity,Intent intent, bool enableDelayedResponse = true)
        {
            DefaultNotificationActivityType = activity.GetType();
            Bundle extras = intent?.Extras;
            if (extras != null && !extras.IsEmpty)
            {
                var parameters = new Dictionary<string, object>();
                foreach (var key in extras.KeySet())
                {
                    if (!parameters.ContainsKey(key) && extras.Get(key) != null)
                        parameters.Add(key, $"{extras.Get(key)}");
                }

                NotificationManager manager = _context.GetSystemService(Context.NotificationService) as NotificationManager;
                var notificationId = extras.GetInt(DefaultPushNotificationHandler.ActionNotificationIdKey, -1);
                if (notificationId != -1)
                {
                    var notificationTag = extras.GetString(DefaultPushNotificationHandler.ActionNotificationTagKey, string.Empty);
                    if (notificationTag == null)
                        manager.Cancel(notificationId);
                    else
                        manager.Cancel(notificationTag, notificationId);
                }


                var response = new NotificationResponse(parameters, extras.GetString(DefaultPushNotificationHandler.ActionIdentifierKey, string.Empty));

                if (_onNotificationOpened == null && enableDelayedResponse)
                    delayedNotificationResponse = response;
                else
                    _onNotificationOpened?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationResponseEventArgs(response.Data, response.Identifier, response.Type));

                CrossAzurePushNotification.Current.NotificationHandler?.OnOpened(response);
            }
        }

        public static void Initialize(Context context, string notificationHubConnectionString, string notificationHubPath, bool resetToken, bool createDefaultNotificationChannel = true, bool autoRegistration = true)
        {
            TokenKey = $"{notificationHubPath}_Token";
            RegisteredKey = $"{notificationHubPath}_PushRegistered";

            Hub = new NotificationHub(notificationHubPath, notificationHubConnectionString, Android.App.Application.Context);
      
            _context = context;

            CrossAzurePushNotification.Current.NotificationHandler = CrossAzurePushNotification.Current.NotificationHandler ?? new DefaultPushNotificationHandler();
            FirebaseMessaging.Instance.AutoInitEnabled = autoRegistration;
            if (autoRegistration)
            {
                ThreadPool.QueueUserWorkItem(state =>
                {

                    var packageName = Application.Context.PackageManager.GetPackageInfo(Application.Context.PackageName, PackageInfoFlags.MetaData).PackageName;
                    var versionCode = Application.Context.PackageManager.GetPackageInfo(Application.Context.PackageName, PackageInfoFlags.MetaData).VersionCode;
                    var versionName = Application.Context.PackageManager.GetPackageInfo(Application.Context.PackageName, PackageInfoFlags.MetaData).VersionName;
                    var prefs = Android.App.Application.Context.GetSharedPreferences(AzurePushNotificationManager.KeyGroupName, FileCreationMode.Private);

                    try
                    {

                        var storedVersionName = prefs.GetString(AzurePushNotificationManager.AppVersionNameKey, string.Empty);
                        var storedVersionCode = prefs.GetString(AzurePushNotificationManager.AppVersionCodeKey, string.Empty);
                        var storedPackageName = prefs.GetString(AzurePushNotificationManager.AppVersionPackageNameKey, string.Empty);


                        if (resetToken || (!string.IsNullOrEmpty(storedPackageName) && (!storedPackageName.Equals(packageName, StringComparison.CurrentCultureIgnoreCase) || !storedVersionName.Equals(versionName, StringComparison.CurrentCultureIgnoreCase) || !storedVersionCode.Equals($"{versionCode}", StringComparison.CurrentCultureIgnoreCase))))
                        {
                            ((AzurePushNotificationManager)CrossAzurePushNotification.Current).CleanUp(false);

                        }

                    }
                    catch (Exception ex)
                    {
                        _onNotificationError?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationErrorEventArgs(AzurePushNotificationErrorType.UnregistrationFailed, ex.ToString()));
                    }
                    finally
                    {
                        var editor = prefs.Edit();
                        editor.PutString(AzurePushNotificationManager.AppVersionNameKey, $"{versionName}");
                        editor.PutString(AzurePushNotificationManager.AppVersionCodeKey, $"{versionCode}");
                        editor.PutString(AzurePushNotificationManager.AppVersionPackageNameKey, $"{packageName}");
                        editor.Commit();
                    }


                    CrossAzurePushNotification.Current.RegisterForPushNotifications();


                });
            }
           

            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O && createDefaultNotificationChannel)
            {
                // Create channel to show notifications.
                string channelId = DefaultNotificationChannelId;
                string channelName = DefaultNotificationChannelName;
                NotificationManager notificationManager = (NotificationManager)context.GetSystemService(Context.NotificationService);

                notificationManager.CreateNotificationChannel(new NotificationChannel(channelId,
                    channelName, NotificationImportance.Default));
            }


            System.Diagnostics.Debug.WriteLine(CrossAzurePushNotification.Current.Token);
        }

        async Task<string> GetTokenAsync()
        {
            _tokenTcs = new TaskCompletionSource<string>();
            FirebaseInstanceId.Instance.GetInstanceId().AddOnCompleteListener(this);

            string retVal = null;

            try
            {
                retVal = await _tokenTcs.Task;
            }
            catch (Exception ex)
            {
                _onNotificationError?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationErrorEventArgs(AzurePushNotificationErrorType.RegistrationFailed, $"{ex}"));
            }

            return retVal;
        }
        public static void Initialize(Context context, string notificationHubConnectionString, string notificationHubPath, NotificationUserCategory[] notificationCategories, bool resetToken, bool createDefaultNotificationChannel = true,bool autoRegistration = true)
        {

            Initialize(context, notificationHubConnectionString, notificationHubPath, resetToken, createDefaultNotificationChannel, autoRegistration);
            RegisterUserNotificationCategories(notificationCategories);

        }


        public void RegisterForPushNotifications()
        {
            FirebaseMessaging.Instance.AutoInitEnabled = true;
            System.Threading.Tasks.Task.Run(async () =>
            {
                var token = await GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    Token = token;
                }
            });

        }
        public void UnregisterForPushNotifications()
        {
            FirebaseMessaging.Instance.AutoInitEnabled = false;
            Reset();
        }

        public void Reset()
        {
            try
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    CleanUp();
                });
            }
            catch (Exception ex)
            {
                _onNotificationError?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationErrorEventArgs(AzurePushNotificationErrorType.UnregistrationFailed,ex.ToString()));
            }


        }

        void CleanUp(bool clearAll = true)
        {
            if(clearAll)
            {
                CrossAzurePushNotification.Current.UnregisterAsync();
            }
            else
            {
                FirebaseInstanceId.Instance.DeleteInstanceId();
                Token = string.Empty;
            }
           
        }


        public static void Initialize(Context context, string notificationHubConnectionString, string notificationHubPath, IPushNotificationHandler pushNotificationHandler, bool resetToken, bool createDefaultNotificationChannel = true,bool autoRegistration = true)
        {
            CrossAzurePushNotification.Current.NotificationHandler = pushNotificationHandler;
            Initialize(context,notificationHubConnectionString,notificationHubPath, resetToken, createDefaultNotificationChannel, autoRegistration);
        }

        public static void ClearUserNotificationCategories()
        {
            userNotificationCategories.Clear();
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



        public IPushNotificationHandler NotificationHandler { get; set; }

        public bool IsEnabled => FirebaseMessaging.Instance.AutoInitEnabled;

        public NotificationUserCategory[] GetUserNotificationCategories()
        {
            return userNotificationCategories?.ToArray();
        }
        public static void RegisterUserNotificationCategories(NotificationUserCategory[] notificationCategories)
        {
            if (notificationCategories != null && notificationCategories.Length > 0)
            {
                ClearUserNotificationCategories();

                foreach (var userCat in notificationCategories)
                {
                    userNotificationCategories.Add(userCat);
                }

            }
            else
            {
                ClearUserNotificationCategories();
            }
        }
     

        public async Task RegisterAsync(string[] tags)
        {
            if (Hub != null)
            {
                _tags = tags;
                await Task.Run(() =>
                {
                    if (!string.IsNullOrEmpty(DeviceToken))
                    {
                        if(IsRegistered && !string.IsNullOrEmpty(Token))
                        {
                            try
                            {
                                Hub.UnregisterAll(Token);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"AzurePushNotification - Unregister- Error - {ex.Message}");

                                _onNotificationError?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationErrorEventArgs(AzurePushNotificationErrorType.NotificationHubUnregistrationFailed, ex.Message));
                            }
                        }
                    

                        try
                        {
                            Registration hubRegistration = null;
                     

                            if(tags !=null && tags.Length > 0)
                            {
                                hubRegistration = Hub.Register(DeviceToken,tags);
                            }
                            else
                            {
                                hubRegistration = Hub.Register(DeviceToken);
                            }

                            var editor = Application.Context.GetSharedPreferences(KeyGroupName, FileCreationMode.Private).Edit();
                            editor.PutBoolean(RegisteredKey, true);
                            editor.PutStringSet(TagsKey, _tags);
                            editor.Commit();

                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"AzurePushNotification - Register - Error - {ex.Message}");
                            _onNotificationError?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationErrorEventArgs(AzurePushNotificationErrorType.NotificationHubRegistrationFailed, ex.Message));
                        }
                    }


                });
            }
        }


        public async Task UnregisterAsync()
        {
            await Task.Run(() =>
            {
                if (Hub != null && !string.IsNullOrEmpty(Token))
                {
                    try
                    {
                        Hub.UnregisterAll(Token);
                       
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"AzurePushNotification - Error - {ex.Message}");

                        _onNotificationError?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationErrorEventArgs(AzurePushNotificationErrorType.NotificationHubUnregistrationFailed,ex.Message));
                    }
                    finally
                    {
                        FirebaseInstanceId.Instance.DeleteInstanceId();
                        Token = string.Empty;

                        _tags = new Collection<string>();
                        var editor = Application.Context.GetSharedPreferences(KeyGroupName, FileCreationMode.Private).Edit();
                        editor.PutBoolean(RegisteredKey, false);
                        editor.PutStringSet(TagsKey, _tags);
                        editor.Commit();
                    }
                }
            });
        }

        public void ClearAllNotifications()
        {
            NotificationManager manager = Application.Context.GetSystemService(Context.NotificationService) as NotificationManager;
            manager.CancelAll();
        }

        public void RemoveNotification(int id)
        {
            NotificationManager manager = Application.Context.GetSystemService(Context.NotificationService) as NotificationManager;
            manager.Cancel(id);
        }

        public void RemoveNotification(string tag, int id)
        {
            if (string.IsNullOrEmpty(tag))
            {
                RemoveNotification(id);
            }
            else
            {
                NotificationManager manager = Application.Context.GetSystemService(Context.NotificationService) as NotificationManager;
                manager.Cancel(tag, id);
            }

        }

        public void OnComplete(Android.Gms.Tasks.Task task)
        {
            try
            {
                if (task.IsSuccessful)
                {
                    string token = task.Result.JavaCast<IInstanceIdResult>().Token;
                    _tokenTcs?.TrySetResult(token);
                }
                else
                {
                    _tokenTcs?.TrySetException(task.Exception);
                }

            }
            catch (Exception ex)
            {
                _tokenTcs?.TrySetException(ex);
            }
        }

        #region internal methods

        internal static string InternalRetrieveSavedToken()
        {
            return Application.Context.GetSharedPreferences(KeyGroupName, FileCreationMode.Private).GetString(TokenKey, string.Empty);
        }

        internal static void InternalSaveToken(string token)
        {
            var editor = Application.Context.GetSharedPreferences(KeyGroupName, FileCreationMode.Private).Edit();
            editor.PutString(TokenKey, token);
            editor.Commit();
        }
        //Raises event for push notification token refresh
        internal static async void RegisterToken(string token)
        {
            DeviceToken = token;
            _onTokenRefresh?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationTokenEventArgs(token));
            await CrossAzurePushNotification.Current.RegisterAsync(_tags?.ToArray());
            CrossAzurePushNotification.Current.SaveToken?.Invoke(token);

        }
        internal static void RegisterData(IDictionary<string, object> data)
        {
            _onNotificationReceived?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationDataEventArgs(data));
        }
        internal static void RegisterDelete(IDictionary<string, object> data)
        {
            _onNotificationDeleted?.Invoke(CrossAzurePushNotification.Current, new AzurePushNotificationDataEventArgs(data));
        }

        #endregion
    }
}