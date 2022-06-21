using Nova;
using UnityEngine;

namespace NovaSamples.HandMenu
{
    /// <summary>
    /// Visuals for a button which can toggle between two states, on and off.
    /// </summary>
    public class ToggleVisuals : ItemVisuals
    {
        [Header("Components")]
        [Tooltip("The UIBlock2D to display the icon of the current toggle state.")]
        public UIBlock2D Icon = null;

        [Header("Icons")]
        [Tooltip("The icon to display while this toggle is \"toggled on\".")]
        public Texture2D OnIcon = null;
        [Tooltip("The icon to display while this toggle is \"toggled off\".")]
        public Texture2D OffIcon = null;

        [Header("Animations")]
        [Tooltip("The animation to run when transitioning from \"off\" to \"on\".")]
        public BodyColorAnimation ToggleOnAnimation;
        [Tooltip("The animation to run when transitioning from \"on\" to \"off\".")]
        public BodyColorAnimation ToggleOffAnimation;

        [SerializeField]
        [Tooltip("The duration, in seconds, of the toggle on/off animations.")]
        private float animationDuration = .15f;

        /// <summary>
        /// The current toggle state.
        /// </summary>
        public bool ToggledOn { get; private set; } = true;

        /// <summary>
        /// The handle tracking any active toggle on/off animations. 
        /// </summary>
        private AnimationHandle animationHandle = default;

        /// <summary>
        /// Flip the current <see cref="ToggledOn"/> state, and update the visuals accordingly.
        /// </summary>
        public void Toggle()
        {
            // Cancel any running animation
            animationHandle.Cancel();

            // Flip toggled state
            ToggledOn = !ToggledOn;

            // Apply visual changes based on new toggled state
            if (ToggledOn)
            {
                Icon.SetImage(OnIcon);
                animationHandle = ToggleOnAnimation.Run(animationDuration);
            }
            else
            {
                Icon.SetImage(OffIcon);
                animationHandle = ToggleOffAnimation.Run(animationDuration);
            }
        }
    }
}

