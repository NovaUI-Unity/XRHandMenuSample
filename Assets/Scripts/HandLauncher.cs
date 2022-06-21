using Nova;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NovaSamples.HandMenu
{
    /// <summary>
    /// A set of components used to display the details of a <see cref="PanelItem"/>.
    /// </summary>
    [Serializable]
    public class PanelItemVisuals : ItemVisuals
    {
        [Tooltip("Will display the icon of the panel to open when selected.")]
        public UIBlock2D Icon;
        [Tooltip("The background visual to style to the theme of the panel.")]
        public UIBlock2D Background;
    }

    /// <summary>
    /// Data associated with a particular <see cref="Panel"/>.
    /// </summary>
    [Serializable]
    public struct PanelItem
    {
        [Tooltip("The panel itself.")]
        public Panel Panel;
        [Tooltip("The icon associated with the given panel.")]
        public Texture2D Icon;
        [Tooltip("The primary panel \"theme\" color.")]
        public Color PrimaryColor;
        [Tooltip("The secondary panel \"theme\" color.")]
        public Color SecondaryColor;
    }

    /// <summary>
    /// A component attached to the users hand which will, when enabled, display a scrollable list of buttons capable of launching different Panel UIs.
    /// </summary>
    public class HandLauncher : MonoBehaviour
    {
        /// <summary>
        /// The event fired when a given <see cref="PanelItem"/> is selected in the list.
        /// </summary>
        public event Action<PanelItem> OnPanelSelected;

        [Header("List")]
        [SerializeField]
        [Tooltip("The scrollable List View to display the set of buttons which each, when clicked, launch a different Panel UI.")]
        private ListView listView = null;
        [SerializeField]
        [Tooltip("The set of Panels to display in the List View.")]
        private List<PanelItem> panels = null;

        [Header("Animations")]
        [SerializeField]
        [Tooltip("The animation to run to fade in the menu.")]
        private ClipMaskTintAnimation fadeInAnimation = default;
        [SerializeField]
        [Tooltip("The animation to run to fade out the menu.")]
        private ClipMaskTintAnimation fadeOutAnimation = default;
        [SerializeField]
        [Tooltip("The animation to chain with a given fade animation to enable/disable this game object.")]
        private ActivateGameObjectAnimation activateAnimation;
        [SerializeField]
        [Tooltip("The duration, in seconds, of a given fade in/out animation.")]
        private float fadeAnimationDuration = .15f;

        /// <summary>
        /// The handle tracking any active fade in/out animations
        /// </summary>
        private AnimationHandle fadeAnimationHandle = default;

        private void Start()
        {
            // Ensure faded out on start
            fadeOutAnimation.Run(0).Complete();

            // Subscribe to PanelItem <-> PanelItemVisuals bind events.
            listView.AddDataBinder<PanelItem, PanelItemVisuals>(Bind);

            // Subscribe to PanelItemVisuals click events
            listView.AddGestureHandler<Gesture.OnClick, PanelItemVisuals>(Click);

            // Set the list of panels as the listView's data source.
            // If the listView is enabled, this will start populating
            // the list with list items, which is why we must subscribe
            // to the bind events before assigning a data source.
            listView.SetDataSource(panels);

            // Subscribe to scroll events on the listView's UIBlock.
            // We subscribe on the UIBlock here, as opposed to the listView
            // directly, because it's not a "list item" that's going to be scrolled
            // but rather the listView itself.
            listView.UIBlock.AddGestureHandler<Gesture.OnScroll>(Scrolled);
        }

        /// <summary>
        /// Animate in this hand launcher UI.
        /// </summary>
        public void Show()
        {
            fadeAnimationHandle.Cancel();

            activateAnimation.TargetActive = true;
            fadeAnimationHandle = activateAnimation.Run(0f).Chain(fadeInAnimation, fadeAnimationDuration);
        }

        /// <summary>
        /// Animate out this hand launcher UI.
        /// </summary>
        public void Hide()
        {
            fadeAnimationHandle.Cancel();

            activateAnimation.TargetActive = false;
            fadeAnimationHandle = fadeOutAnimation.Run(fadeAnimationDuration).Chain(activateAnimation, 0f);
        }

        /// <summary>
        /// On click, fire a corresponding OnPanelSelected event.
        /// </summary>
        private void Click(Gesture.OnClick evt, PanelItemVisuals target, int index)
        {
            OnPanelSelected?.Invoke(panels[index]);
        }

        /// <summary>
        /// Populate a <see cref="PanelItemVisuals"/> object in the <see cref="listView"/> with 
        /// the information from its corresponding <see cref="PanelItem"/>
        /// object in the data source.
        /// </summary>
        private void Bind(Data.OnBind<PanelItem> evt, PanelItemVisuals target, int index)
        {
            // The UserData on this bind event is the same value stored
            // at the given `index` into the list of panels.
            //
            // I.e.
            // evt.UserData == panels[index]
            PanelItem panel = evt.UserData;

            // Assign the icon and theme the visuals with
            // the primary/secondary panel colors.
            target.Icon.SetImage(panel.Icon);
            target.Icon.Gradient.Color = panel.PrimaryColor;

            target.Background.Border.Color = panel.PrimaryColor;
            target.Background.Shadow.Color = panel.SecondaryColor;

            // Because the list item is just now being bound into view
            // its layout properties are likely stale or uncalculated, since the
            // Nova Engine update won't run until the end of the current frame.
            //
            // Explicitly call CalculateLayout() here to ensure the size/position
            // of this list item have non-zero calculated values before we try to
            // use them to determine the radial adjustment.
            target.View.UIBlock.CalculateLayout();

            // Adjust the X offset along the scrolling arc.
            ApplyRadialAdjustment(target.View);
        }

        /// <summary>
        /// On scroll, adjust the z position of the contact cards in view to give the visual effect
        /// that they are scrolling along an arc where the most centered item is closer to the user.
        /// </summary>
        private void Scrolled(Gesture.OnScroll evt)
        {
            // Get the min/max range of items in view
            int minIndex = listView.MinLoadedIndex;
            int maxIndex = listView.MaxLoadedIndex;

            for (int i = minIndex; i <= maxIndex; ++i)
            {
                // Get the list item visually representing panels[i] 
                if (!listView.TryGetItemView(i, out ItemView listItem))
                {
                    // We won't hit this in this sample (assuming no modifications),
                    // but it's good practice to validate the item hasn't been
                    // destroyed or detatched.
                    continue;
                }

                // Apply X offsets to scroll along an arc.
                ApplyRadialAdjustment(listItem);
            }
        }

        /// <summary>
        /// A visual effect applied to the items in the <see cref="listView"/>,
        /// which will adjust their x position to appear as if the objects are scrolling
        /// along an arc.
        /// </summary>
        private void ApplyRadialAdjustment(ItemView listItem)
        {
            UIBlock uiBlock = listItem.UIBlock;

            // The effective "radius" of the arc determined
            // by the height of the viewport and list item
            float radius = listView.UIBlock.PaddedSize.y * 0.5f + uiBlock.CalculatedSize.Y.Value;

            // The center position of the item in view
            float yPos = uiBlock.transform.localPosition.y;

            // Convert the y position into a sine value between [-1, 1]
            float sinTheta = Mathf.Clamp(yPos / radius, -1, 1);

            // Get the angle, theta, from the calculated sine
            float theta = Mathf.Asin(sinTheta);

            // And get the corresponding cosine value of theta. This is effectively
            // our "normalized" x position.
            //
            // More centered => closer to 1
            // towards edges => closer to 0
            float cosTheta = Mathf.Cos(theta);

            // Ensure the items are aligned to the left of listView bounds
            uiBlock.Alignment.X = HorizontalAlignment.Left;

            // Here we offset from the left edge of the list by the
            // normalizedXPosition - normalizedXSize - normalizedLeftMargin.
            // This will lead to center-most items aligned to the right edge
            // and items towards the top/bottom will lie on the left edge.
            uiBlock.Position.X.Percent = cosTheta - uiBlock.CalculatedSize.X.Percent - uiBlock.CalculatedMargin.Left.Percent;
        }
    }
}

