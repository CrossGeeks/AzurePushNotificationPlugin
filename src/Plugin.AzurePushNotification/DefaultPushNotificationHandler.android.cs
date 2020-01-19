﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using Java.Util;

namespace Plugin.AzurePushNotification
{
    public class DefaultPushNotificationHandler : IPushNotificationHandler
    {

        public const string DomainTag = "DefaultPushNotificationHandler";

        /// <summary>
        /// Title
        /// </summary>
        public const string TitleKey = "title";
        /// <summary>
        /// Text
        /// </summary>
        public const string TextKey = "text";
        /// <summary>
        /// Subtitle
        /// </summary>
        public const string SubtitleKey = "subtitle";
        /// <summary>
        /// Message
        /// </summary>
        public const string MessageKey = "message";
        /// <summary>
        /// Message
        /// </summary>
        public const string BodyKey = "body";
        /// <summary>
        /// Alert
        /// </summary>
        public const string AlertKey = "alert";

        /// <summary>
        /// Id
        /// </summary>
        public const string IdKey = "id";

        /// <summary>
        /// Tag
        /// </summary>
        public const string TagKey = "tag";

        /// <summary>
        /// Action Click
        /// </summary>
        public const string ActionKey = "click_action";

        /// <summary>
        /// Category
        /// </summary>
        public const string CategoryKey = "category";

        /// <summary>
        /// Silent
        /// </summary>
        public const string SilentKey = "silent";

        /// <summary>
        /// ActionNotificationId
        /// </summary>
        public const string ActionNotificationIdKey = "action_notification_id";

        /// <summary>
        /// ActionNotificationTag
        /// </summary>
        public const string ActionNotificationTagKey = "action_notification_tag";

        /// <summary>
        /// ActionIdentifeir
        /// </summary>
        public const string ActionIdentifierKey = "action_identifier";

        /// <summary>
        /// Color
        /// </summary>
        public const string ColorKey = "color";

        /// <summary>
        /// Icon
        /// </summary>
        public const string IconKey = "icon";

        /// <summary>
        /// Large Icon
        /// </summary>
        public const string LargeIconKey = "large_icon";

        /// <summary>
        /// Sound
        /// </summary>
        public const string SoundKey = "sound";

        /// <summary>
        /// Priority
        /// </summary>
        public const string PriorityKey = "priority";

        /// <summary>
        /// Channel id
        /// </summary>
        public const string ChannelIdKey = "channel_id";

        public virtual void OnOpened(NotificationResponse response)
        {
            System.Diagnostics.Debug.WriteLine($"{DomainTag} - OnOpened");
        }

        public virtual void OnReceived(IDictionary<string, object> parameters)
        {
            System.Diagnostics.Debug.WriteLine($"{DomainTag} - OnReceived");

            if (parameters.TryGetValue(SilentKey, out object silent) && (silent.ToString() == "true" || silent.ToString() == "1"))
                return;

            Context context = Application.Context;

            int notifyId = 0;
            string title = context.ApplicationInfo.LoadLabel(context.PackageManager);
            var message = string.Empty;
            var tag = string.Empty;

            if (!string.IsNullOrEmpty(AzurePushNotificationManager.NotificationContentTextKey) && parameters.TryGetValue(AzurePushNotificationManager.NotificationContentTextKey, out object notificationContentText))
                message = notificationContentText.ToString();
            else if (parameters.TryGetValue(AlertKey, out object alert))
                message = $"{alert}";
            else if (parameters.TryGetValue(BodyKey, out object body))
                message = $"{body}";
            else if (parameters.TryGetValue(MessageKey, out object messageContent))
                message = $"{messageContent}";
            else if (parameters.TryGetValue(SubtitleKey, out object subtitle))
                message = $"{subtitle}";
            else if (parameters.TryGetValue(TextKey, out object text))
                message = $"{text}";

            if (!string.IsNullOrEmpty(AzurePushNotificationManager.NotificationContentTitleKey) && parameters.TryGetValue(AzurePushNotificationManager.NotificationContentTitleKey, out object notificationContentTitle))
                title = notificationContentTitle.ToString();
            else if (parameters.TryGetValue(TitleKey, out object titleContent))
            {
                if (!string.IsNullOrEmpty(message))
                    title = $"{titleContent}";
                else
                    message = $"{titleContent}";
            }

            if (parameters.TryGetValue(IdKey, out object id))
            {
                try
                {
                    notifyId = Convert.ToInt32(id);
                }
                catch (Exception ex)
                {
                    // Keep the default value of zero for the notify_id, but log the conversion problem.
                    System.Diagnostics.Debug.WriteLine($"Failed to convert {id} to an integer {ex}");
                }
            }

            if (parameters.TryGetValue(TagKey, out object tagContent))
                tag = tagContent.ToString();


            try
            {
                if (parameters.TryGetValue(SoundKey, out object sound))
                {
                    var soundName = sound.ToString();

                    int soundResId = context.Resources.GetIdentifier(soundName, "raw", context.PackageName);
                    if (soundResId == 0 && soundName.IndexOf('.') != -1)
                    {
                        soundName = soundName.Substring(0, soundName.LastIndexOf('.'));
                        soundResId = context.Resources.GetIdentifier(soundName, "raw", context.PackageName);
                    }

                    AzurePushNotificationManager.SoundUri = new Android.Net.Uri.Builder()
                              .Scheme(ContentResolver.SchemeAndroidResource)
                              .Path($"{context.PackageName}/{soundResId}")
                              .Build();

                }
            }
            catch (Resources.NotFoundException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }


            if (AzurePushNotificationManager.SoundUri == null)
                AzurePushNotificationManager.SoundUri = RingtoneManager.GetDefaultUri(RingtoneType.Notification);

            try
            {
                if (parameters.TryGetValue(IconKey, out object icon) && icon != null)
                {
                    try
                    {
                        AzurePushNotificationManager.IconResource = context.Resources.GetIdentifier($"{icon}", "drawable", Application.Context.PackageName);
                        if (AzurePushNotificationManager.IconResource == 0)
                        {
                            AzurePushNotificationManager.IconResource = context.Resources.GetIdentifier($"{icon}", "mipmap", Application.Context.PackageName);
                        }
                    }
                    catch (Resources.NotFoundException ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                    }
                }

                if (AzurePushNotificationManager.IconResource == 0)
                    AzurePushNotificationManager.IconResource = context.ApplicationInfo.Icon;
                else
                {
                    string name = context.Resources.GetResourceName(AzurePushNotificationManager.IconResource);
                    if (name == null)
                        AzurePushNotificationManager.IconResource = context.ApplicationInfo.Icon;
                }
            }
            catch (Resources.NotFoundException ex)
            {
                AzurePushNotificationManager.IconResource = context.ApplicationInfo.Icon;
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

            try
            {
                if (parameters.TryGetValue(LargeIconKey, out object largeIcon) && largeIcon != null)
                {
                    AzurePushNotificationManager.LargeIconResource = context.Resources.GetIdentifier($"{largeIcon}", "drawable", Application.Context.PackageName);
                    if (AzurePushNotificationManager.LargeIconResource == 0)
                    {
                        AzurePushNotificationManager.LargeIconResource = context.Resources.GetIdentifier($"{largeIcon}", "mipmap", Application.Context.PackageName);
                    }
                }

                if (AzurePushNotificationManager.LargeIconResource != 0)
                {
                    string name = context.Resources.GetResourceName(AzurePushNotificationManager.LargeIconResource);
                    if (name == null)
                        AzurePushNotificationManager.LargeIconResource = 0;
                }
            }
            catch (Resources.NotFoundException ex)
            {
                AzurePushNotificationManager.LargeIconResource = 0;
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

            if (parameters.TryGetValue(ColorKey, out object color) && color != null)
            {
                try
                {
                    AzurePushNotificationManager.Color = Color.ParseColor(color.ToString());
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"{DomainTag} - Failed to parse color {ex}");
                }
            }

            Intent resultIntent = typeof(Activity).IsAssignableFrom(AzurePushNotificationManager.NotificationActivityType) ? new Intent(Application.Context, AzurePushNotificationManager.NotificationActivityType) : (AzurePushNotificationManager.DefaultNotificationActivityType == null ? context.PackageManager.GetLaunchIntentForPackage(context.PackageName) : new Intent(Application.Context, AzurePushNotificationManager.DefaultNotificationActivityType));

            Bundle extras = new Bundle();
            foreach (var p in parameters)
                extras.PutString(p.Key, p.Value.ToString());

            if (extras != null)
            {
                extras.PutInt(ActionNotificationIdKey, notifyId);
                extras.PutString(ActionNotificationTagKey, tag);
                resultIntent.PutExtras(extras);
            }

            if (AzurePushNotificationManager.NotificationActivityFlags != null)
            {
                resultIntent.SetFlags(AzurePushNotificationManager.NotificationActivityFlags.Value);
            }
            int requestCode = new Java.Util.Random().NextInt();
            var pendingIntent = PendingIntent.GetActivity(context, requestCode, resultIntent,PendingIntentFlags.UpdateCurrent);

            var chanId = AzurePushNotificationManager.DefaultNotificationChannelId;
            if (parameters.TryGetValue(ChannelIdKey, out object channelId) && channelId != null)
            {
                chanId = $"{channelId}";
            }

            var notificationBuilder = new NotificationCompat.Builder(context, chanId)
                .SetSmallIcon(AzurePushNotificationManager.IconResource)
                .SetContentTitle(title)
                .SetContentText(message)
                .SetAutoCancel(true)
                .SetContentIntent(pendingIntent);

            if(AzurePushNotificationManager.LargeIconResource != 0)
            {
                Bitmap largeIconBitmap = BitmapFactory.DecodeResource(context.Resources, AzurePushNotificationManager.LargeIconResource);
                notificationBuilder.SetLargeIcon(largeIconBitmap);
            }
            var deleteIntent = new Intent(context,typeof(PushNotificationDeletedReceiver));
            var pendingDeleteIntent = PendingIntent.GetBroadcast(context, requestCode, deleteIntent,PendingIntentFlags.CancelCurrent);
            notificationBuilder.SetDeleteIntent(pendingDeleteIntent);

            if (Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.O)
            {
                if (parameters.TryGetValue(PriorityKey, out object priority) && priority != null)
                {
                    var priorityValue = $"{priority}";
                    if (!string.IsNullOrEmpty(priorityValue))
                    {
                        switch (priorityValue.ToLower())
                        {
                            case "max":
                                notificationBuilder.SetPriority((int)Android.App.NotificationPriority.Max);
                                notificationBuilder.SetVibrate(new long[] { 1000, 1000, 1000, 1000, 1000 });
                                break;
                            case "high":
                                notificationBuilder.SetPriority((int)Android.App.NotificationPriority.High);
                                notificationBuilder.SetVibrate(new long[] { 1000, 1000, 1000, 1000, 1000 });
                                break;
                            case "default":
                                notificationBuilder.SetPriority((int)Android.App.NotificationPriority.Default);
                                notificationBuilder.SetVibrate(new long[] { 1000, 1000, 1000, 1000, 1000 });
                                break;
                            case "low":
                                notificationBuilder.SetPriority((int)Android.App.NotificationPriority.Low);
                                break;
                            case "min":
                                notificationBuilder.SetPriority((int)Android.App.NotificationPriority.Min);
                                break;
                            default:
                                notificationBuilder.SetPriority((int)Android.App.NotificationPriority.Default);
                                notificationBuilder.SetVibrate(new long[] { 1000, 1000, 1000, 1000, 1000 });
                                break;
                        }

                    }
                    else
                    {
                        notificationBuilder.SetVibrate(new long[] { 1000, 1000, 1000, 1000, 1000 });
                    }

                }
                else
                {
                    notificationBuilder.SetVibrate(new long[] { 1000, 1000, 1000, 1000, 1000 });
                }


                try
                {

                    notificationBuilder.SetSound(AzurePushNotificationManager.SoundUri);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"{DomainTag} - Failed to set sound {ex}");
                }
            }

            // Try to resolve (and apply) localized parameters
            ResolveLocalizedParameters(notificationBuilder, parameters);

            if (AzurePushNotificationManager.Color != null)
                notificationBuilder.SetColor(AzurePushNotificationManager.Color.Value);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.JellyBean)
            {
                // Using BigText notification style to support long message
                var style = new NotificationCompat.BigTextStyle();
                style.BigText(message);
                notificationBuilder.SetStyle(style);
            }

            string category = string.Empty;
            if (parameters.TryGetValue(CategoryKey, out object categoryContent))
                category = categoryContent.ToString();

            if (parameters.TryGetValue(ActionKey, out object actionContent))
                category = actionContent.ToString();

            var notificationCategories = CrossAzurePushNotification.Current?.GetUserNotificationCategories();
            if (notificationCategories != null && notificationCategories.Length > 0)
            {
                IntentFilter intentFilter = null;
                foreach (var userCat in notificationCategories)
                {
                    if (userCat != null && userCat.Actions != null && userCat.Actions.Count > 0)
                    {

                        foreach (var action in userCat.Actions)
                        {
                            var aRequestCode = Guid.NewGuid().GetHashCode();
                            if (userCat.Category.Equals(category, StringComparison.CurrentCultureIgnoreCase))
                            {
                                Intent actionIntent = null;
                                PendingIntent pendingActionIntent = null;


                                if (action.Type == NotificationActionType.Foreground)
                                {
                                    actionIntent = typeof(Activity).IsAssignableFrom(AzurePushNotificationManager.NotificationActivityType) ? new Intent(Application.Context, AzurePushNotificationManager.NotificationActivityType) : (AzurePushNotificationManager.DefaultNotificationActivityType == null ? context.PackageManager.GetLaunchIntentForPackage(context.PackageName) : new Intent(Application.Context, AzurePushNotificationManager.DefaultNotificationActivityType));

                                    if (AzurePushNotificationManager.NotificationActivityFlags != null)
                                    {
                                        actionIntent.SetFlags(AzurePushNotificationManager.NotificationActivityFlags.Value);
                                    }

                                    extras.PutString(ActionIdentifierKey, action.Id);
                                    actionIntent.PutExtras(extras);
                                    pendingActionIntent = PendingIntent.GetActivity(context, aRequestCode, actionIntent, PendingIntentFlags.UpdateCurrent);

                                }
                                else
                                {
                                    actionIntent = new Intent(context, typeof(PushNotificationActionReceiver));
                                    extras.PutString(ActionIdentifierKey, action.Id);
                                    actionIntent.PutExtras(extras);
                                    pendingActionIntent = PendingIntent.GetBroadcast(context, aRequestCode, actionIntent, PendingIntentFlags.UpdateCurrent);

                                }

                                notificationBuilder.AddAction(context.Resources.GetIdentifier(action.Icon, "drawable", Application.Context.PackageName), action.Title, pendingActionIntent);
                            }
                        }
                    }
                }
                
            }

            OnBuildNotification(notificationBuilder, parameters);

            NotificationManager notificationManager = (NotificationManager)context.GetSystemService(Context.NotificationService);
            notificationManager.Notify(tag, notifyId, notificationBuilder.Build());
        }

        /// <summary>
        /// Resolves the localized parameters using the string resources, combining the key and the passed arguments of the notification.
        /// </summary>
        /// <param name="notificationBuilder">Notification builder.</param>
        /// <param name="parameters">Parameters.</param>
        void ResolveLocalizedParameters(NotificationCompat.Builder notificationBuilder, IDictionary<string, object> parameters)
        {
            string getLocalizedString(string name, params string[] arguments)
            {
                var context = notificationBuilder.MContext;
                var resources = context.Resources;
                var identifier = resources.GetIdentifier(name, "string", context.PackageName);
                var sanitizedArgs = arguments?.Where(it => it != null).Select(it => new Java.Lang.String(it)).Cast<Java.Lang.Object>().ToArray();

                try { return resources.GetString(identifier, sanitizedArgs ?? new Java.Lang.Object[] { }); }
                catch (UnknownFormatConversionException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"{DomainTag}.ResolveLocalizedParameters - Incorrect string arguments {ex}");
                    return null;
                }
            }

            // Resolve title localization
            if (parameters.TryGetValue("title_loc_key", out object titleKey))
            {
                parameters.TryGetValue("title_loc_args", out object titleArgs);

                var localizedTitle = getLocalizedString(titleKey.ToString(), titleArgs as string[]);
                if (localizedTitle != null)
                    notificationBuilder.SetContentTitle(localizedTitle);
            }

            // Resolve body localization
            if (parameters.TryGetValue("body_loc_key", out object bodyKey))
            {
                parameters.TryGetValue("body_loc_args", out object bodyArgs);

                var localizedBody = getLocalizedString(bodyKey.ToString(), bodyArgs as string[]);
                if (localizedBody != null)
                    notificationBuilder.SetContentText(localizedBody);
            }
        }

        public virtual void OnError(string error)
        {
            System.Diagnostics.Debug.WriteLine($"{DomainTag} - OnError - {error}");
        }

        /// <summary>
        /// Override to provide customization of the notification to build.
        /// </summary>
        /// <param name="notificationBuilder">Notification builder.</param>
        /// <param name="parameters">Notification parameters.</param>
        public virtual void OnBuildNotification(NotificationCompat.Builder notificationBuilder, IDictionary<string, object> parameters) { }
    }
}
