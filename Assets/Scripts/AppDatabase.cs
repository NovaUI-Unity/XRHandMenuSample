using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NovaSamples.HandMenu
{
    /// <summary>
    /// A simplified struct holding information
    /// relevant to a specific user-level application. 
    /// E.g.
    /// Phone, messaging, email, etc.
    /// </summary>
    [Serializable]
    public struct AppInfo
    {
        [Tooltip("The name of the application.")]
        public string AppName;
        [Tooltip("The app icon.")]
        public Texture2D AppIcon;
        /// <summary>
        /// The color to associate with this application. Effectively the app \"theme\" color.
        /// </summary>
        [NonSerialized]
        public Color AppColor;
    }

    /// <summary>
    /// Provides the rest of the sample with some dummy apps.
    /// </summary>
    [CreateAssetMenu(menuName = "HandMenu/Apps")]
    public class AppDatabase : ScriptableObject
    {
        [SerializeField]
        [Tooltip("The set of \"Applications\" to choose from when randomly generating an app.\nThis list is relatively small, so there will likely be duplicates")]
        private List<AppInfo> appDetails = null;
        [SerializeField]
        [Tooltip("The set of colors to choose from when assigning app colors")]
        private List<Color> appColors = null;

        public AppInfo GetRandomApp()
        {
            // Get a random index within the available range.
            int randomIndex = UnityEngine.Random.Range(0, appDetails.Count);

            // Add the AppInfo to the list.
            // The list of provided appDetails to choose from
            // is rather small, so the list of apps here
            // will likely have a lot of duplicates.
            AppInfo app = appDetails[randomIndex];
            app.AppColor = appColors[UnityEngine.Random.Range(0, appColors.Count)];
            return app;
        }
    }
}

