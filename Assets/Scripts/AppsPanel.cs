using Nova;
using System.Collections.Generic;
using UnityEngine;

namespace NovaSamples.HandMenu
{
    /// <summary>
    /// The set of components used to visually represent
    /// an "Application" in the <see cref="AppsPanel"/>.
    /// </summary>
    public class AppVisuals : ItemVisuals
    {
        [Tooltip("The UIBlock2D to animate with a shadow/glow effect when the Push Button is pushed.")]
        public UIBlock2D Shadow;
        [Tooltip("The UIBlock2D to display the application's icon.")]
        public UIBlock2D Icon;
        [Tooltip("The Push Button respnsible for animating the effect of pushing a physical button and triggering click events.")]
        public PushButton PushButton;
    }

    /// <summary>
    /// The UI panel responsible for managing a grid of "Applications" the user could select and potentially launch into.
    /// </summary>
    public class AppsPanel : Panel
    {
        [Header("Grid")]
        [SerializeField]
        [Tooltip("The scrollable Grid View to display the available set of Applications.")]
        private GridView gridView = null;
        [SerializeField]
        [Tooltip("The set of applications will be randomly generated, so this is the number of grid elements to generate.")]
        private int appCount = 40;
        [SerializeField]
        [Tooltip("The default accent color of a given \"Application\" button when it's not actively being pressed.")]
        private Color defaultAccentColor = Color.black;
        [SerializeField]
        [Tooltip("The app database from which to populate the grid.")]
        private AppDatabase appDatabase = null;
        [Header("Animations")]
        [Tooltip("The duration of a press animation in seconds.")]
        public float PressAnimationDuration = 0.15f;

        /// <summary>
        /// Apps panel wants the pointer to emit light
        /// </summary>
        public override bool UseTorchPointer => true;

        /// <summary>
        /// The <see cref="gridView"/>'s data source containing a randomly generated set of "Applications".
        /// </summary>
        private List<AppInfo> apps = new List<AppInfo>();

        // The set of animations that, when combined,
        // make up a complete press/release animation
        private BodyColorAnimation backgroundAnimation;
        private ShadowColorAnimation backgroundShadowAnimation;

        /// <summary>
        /// The animation handle tracking the active press/release animation
        /// </summary>
        private AnimationHandle pressAnimationHandle;

        private void Start()
        {
            EnsureDataSourceInitialized();

            // Subscribe to bind events in the gridView.
            gridView.AddDataBinder<AppInfo, AppVisuals>(Bind);

            // Subscribe to gestures events in the gridView.
            gridView.AddGestureHandler<Gesture.OnPress, AppVisuals>(Press);
            gridView.AddGestureHandler<Gesture.OnRelease, AppVisuals>(Release);
            gridView.AddGestureHandler<Gesture.OnCancel, AppVisuals>(Cancel);

            // Subscribe to GridSlice configurations requests in the gridView.
            gridView.SetSliceProvider(ProvideSlice);

            // Set the list of apps as the gridView's data source.
            // If the gridView is enabled, this will start populating
            // the grid with grid items, which is why we must subscribe
            // to the bind/grid slice events before assigning a data source.
            gridView.SetDataSource(apps);
        }

        /// <summary>
        /// Populate an AppVisuals object in the grid with 
        /// the information from its corresponding AppInfo
        /// object in the data source.
        /// </summary>
        private void Bind(Data.OnBind<AppInfo> evt, AppVisuals target, int index)
        {
            // The UserData on this bind event is the same value stored
            // at the given `index` into the list of apps.
            //
            // I.e.
            // evt.UserData == apps[index]
            AppInfo app = evt.UserData;

            // Assign the icon's image to the App's icon
            target.Icon.SetImage(app.AppIcon);

            // Apply the app color to the icon
            target.Icon.Color = app.AppColor;

            // Apply the default accent color to the shadow
            target.Shadow.Color = defaultAccentColor;
            target.Shadow.Shadow.Color = defaultAccentColor;
        }

        /// <summary>
        /// Handle press canceled, likely due to the grid being scrolled.
        /// </summary>
        private void Cancel(Gesture.OnCancel evt, AppVisuals target, int index)
        {
            AnimatePressState(defaultAccentColor, target);
        }

        /// <summary>
        /// Animate the pressed visuals back to the <see cref="defaultAccentColor"/> on release.
        /// </summary>
        private void Release(Gesture.OnRelease evt, AppVisuals target, int index)
        {
            AnimatePressState(defaultAccentColor, target);
        }

        /// <summary>
        /// Animate the pressed visuals to the app's accent
        /// color, <see cref="AppInfo.AppColor"/>, on press.
        /// </summary>
        private void Press(Gesture.OnPress evt, AppVisuals target, int index)
        {
            AnimatePressState(apps[index].AppColor, target);
        }

        private void AnimatePressState(Color targetColor, AppVisuals target)
        {
            pressAnimationHandle.Complete();

            // Animate the target's background color
            backgroundAnimation.TargetColor = targetColor;
            backgroundAnimation.Target = target.Shadow;

            // Animate the target's drop shadow color
            backgroundShadowAnimation.TargetColor = targetColor;
            backgroundShadowAnimation.Target = target.Shadow;

            // Combine all animations to be tracked by the one pressAnimationHandle.
            pressAnimationHandle = backgroundAnimation.Run(PressAnimationDuration)
                                                      .Include(backgroundShadowAnimation);
        }

        /// <summary>
        /// Configure the <see cref="GridSlice"/>s to use <see cref="AutoLayout.AutoSpace"/>.
        /// </summary>
        private void ProvideSlice(int sliceIndex, GridView gridView, ref GridSlice gridSlice)
        {
            gridSlice.AutoLayout.AutoSpace = true;
        }

        /// <summary>
        /// Generate a random list of <see cref="AppInfo"/>s, which 
        /// we'll use as the <see cref="gridView"/>'s data source.
        /// </summary>
        private void EnsureDataSourceInitialized()
        {
            if (apps.Count > 0)
            {
                // Already initialized, don't need to repopulate.
                return;
            }

            for (int i = 0; i < appCount; i++)
            {
                // Get a random app from the database.
                apps.Add(appDatabase.GetRandomApp());
            }
        }
    }
}

