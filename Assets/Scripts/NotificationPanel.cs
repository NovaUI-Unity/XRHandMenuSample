using Nova;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NovaSamples.HandMenu
{
    /// <summary>
    /// The set of components used to visually represent
    /// a <see cref="Notification"/> in the <see cref="NotificationPanel"/>.
    /// </summary>
    [Serializable]
    public class NotificationVisuals : ItemVisuals
    {
        [Tooltip("A visual to stylize to match the theme color of the application sending the notification.")]
        public UIBlock2D AccentBar;
        [Tooltip("A UIBlock2D to display the icon of the application sending the notification.")]
        public UIBlock2D Icon;
        [Tooltip("A text field to display the subject of the notification.")]
        public TextBlock Subject;
        [Tooltip("A text field to display the more verbose details of the notification.")]
        public TextBlock Body;
        [Tooltip("A text field to display the date/time the user received the notification.")]
        public TextBlock Date;
    }

    /// <summary>
    /// A simplified struct holding information
    /// relevant to a notification from a specific 
    /// user-level application. 
    /// 
    /// E.g.
    /// Phone, messaging, email, etc.
    /// </summary>
    public struct Notification
    {
        /// <summary>
        /// The icon associated with the application the notification came from.
        /// </summary>
        public Texture2D Icon;
        /// <summary>
        /// The "theme" color of the application the notification came from. 
        /// </summary>
        public Color AppColor;
        /// <summary>
        /// The subject of the notification.
        /// </summary>
        public string Subject;
        /// <summary>
        /// The longer text details of the notification.
        /// </summary>
        public string Body;
        /// <summary>
        /// The time the notification was received.
        /// </summary>
        public DateTime Time;
    }

    public class NotificationPanel : Panel
    {
        [Header("List")]
        [SerializeField]
        [Tooltip("The scrollable list to display app notifications.")]
        private ListView listView = null;
        [SerializeField]
        [Tooltip("The set of notifications will be randomly generated, so this is the number of list elements to generate.")]
        private int notificationCount = 25;
        [SerializeField]
        [Tooltip("The app database from which to populate the list.")]
        private AppDatabase appDatabase = null;

        /// <summary>
        /// The <see cref="listView"/>'s data source containing a randomly generated set of <see cref="Notification"/>s.
        /// </summary>
        private List<Notification> notifications = new List<Notification>();

        private void Start()
        {
            EnsureDataSourceInitialized();

            // Subscribe to bind events in the listView.
            listView.AddDataBinder<Notification, NotificationVisuals>(Bind);

            // Set the list of apps as the listView's data source.
            // If the listView is enabled, this will start populating
            // the list with list items, which is why we must subscribe
            // to the bind events before assigning a data source.
            listView.SetDataSource(notifications);
        }

        /// <summary>
        /// Populate a <see cref="NotificationVisuals"/> object in the list
        /// with  the information from its corresponding <see cref="Notification"/>
        /// object in the data source.
        /// </summary>
        private void Bind(Data.OnBind<Notification> evt, NotificationVisuals target, int index)
        {
            // The UserData on this bind event is the same value stored
            // at the given `index` into the list of notifications.
            //
            // I.e.
            // evt.UserData == notifications[index]
            Notification notification = evt.UserData;

            // Assign header text to the header of the notification
            target.Subject.Text = notification.Subject;

            // Assign the icon's image to the notification's icon
            target.Icon.SetImage(notification.Icon);

            // Change the accent bar color to match the notification's AppColor
            target.AccentBar.Color = notification.AppColor;
        }

        /// <summary>
        /// Generate a random list of <see cref="Notification"/>s, which 
        /// we'll use as the <see cref="listView"/>'s data source.
        /// </summary>
        private void EnsureDataSourceInitialized()
        {
            if (notifications.Count > 0)
            {
                // Already initialized, don't need to repopulate.
                return;
            }

            for (int i = 0; i < notificationCount; i++)
            {
                AppInfo info = appDatabase.GetRandomApp();

                // Create and add a new notification, pulling
                // from the list of apps available to create
                // notifications
                notifications.Add(new Notification()
                {
                    Subject = info.AppName,
                    Icon = info.AppIcon,
                    AppColor = info.AppColor,
                });
            }
        }
    }
}
