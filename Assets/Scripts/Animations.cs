using Nova;
using System;
using UnityEngine;

namespace NovaSamples.HandMenu
{
    /// <summary>
    /// Animates a <see cref="UIBlock"/>'s color from its current value to <see cref="TargetColor"/>.
    /// </summary>
    [Serializable]
    public struct BodyColorAnimation : IAnimation
    {
        [Tooltip("The end body color of the animation.")]
        public Color TargetColor;
        [Tooltip("The UIBlock whose body color will be animated.")]
        public UIBlock Target;

        private Color startColor;

        public void Update(float percentDone)
        {
            if (percentDone == 0f)
            {
                startColor = Target.Color;
            }

            Target.Color = Color.Lerp(startColor, TargetColor, percentDone);
        }
    }

    /// <summary>
    /// Animates a <see cref="UIBlock2D"/>'s shadow color from its current value to <see cref="TargetColor"/>.
    /// </summary>
    [Serializable]
    public struct ShadowColorAnimation : IAnimation
    {
        [Tooltip("The end shadow color of the animation.")]
        public Color TargetColor;
        [Tooltip("The UIBlock2D whose shadow color will be animated.")]
        public UIBlock2D Target;

        private Color startColor;

        public void Update(float percentDone)
        {
            if (percentDone == 0f)
            {
                startColor = Target.Shadow.Color;
            }

            Target.Shadow.Color = Color.Lerp(startColor, TargetColor, percentDone);
        }
    }

    /// <summary>
    /// Animates a <see cref="UIBlock2D"/>'s gradient color from its current value to <see cref="GradientTargetColor"/>. 
    /// If the gradient starts as disabled, the animation will enable it and animate from transparent to the <see cref="GradientTargetColor"/>.
    /// </summary>
    /// <remarks>Will disable the body if the gradient color is transparent when the animation completes.</remarks>
    [Serializable]
    public struct BodyGradientAnimation : IAnimation
    {
        [Tooltip("The gradient end color of the animation.")]
        public Color GradientTargetColor;
        [Tooltip("The UIBlock2D whose gradient color will be animated.")]
        public UIBlock2D Target;

        private Color startColor;

        public void Update(float percentDone)
        {
            if (percentDone == 0)
            {
                Initialize();
            }

            Target.Gradient.Color = Color.Lerp(startColor, GradientTargetColor, percentDone);

            if (percentDone == 1)
            {
                Cleanup();
            }
        }

        /// <summary>
        /// Initialized start values before the animation begins.
        /// </summary>
        private void Initialize()
        {
            bool gradientVisible = Target.BodyEnabled && Target.Gradient.Enabled;

            if (gradientVisible)
            {
                // Gradient is already enabled, so start at the current value
                startColor = Target.Gradient.Color;
            }
            else
            {
                // Gradient not visible, so lerp from transparent to target
                startColor = GradientTargetColor;
                startColor.a = 0f;

                // Ensure these are both enabled
                Target.BodyEnabled = true;
                Target.Gradient.Enabled = true;
            }
        }

        /// <summary>
        /// Finalize any desired end state once the animation has completed.
        /// </summary>
        private void Cleanup()
        {
            Target.BodyEnabled = Target.Gradient.Color.a != 0;
        }
    }

    /// <summary>
    /// Animates the <see cref="ClipMask.Tint"/> of a <see cref="ClipMask"/>.
    /// </summary>
    [Serializable]
    public struct ClipMaskTintAnimation : IAnimation
    {
        [Tooltip("The tint end color of the animation.")]
        public Color TargetColor;
        [Tooltip("The ClipMask whose tint color will be animated.")]
        public ClipMask Target;

        private Color startColor;

        public void Update(float percentDone)
        {
            if (percentDone == 0f)
            {
                startColor = Target.Tint;
            }

            Target.Tint = Color.Lerp(startColor, TargetColor, percentDone);
        }
    }

    /// <summary>
    /// An animation whch disables the <see cref="Target"/> GameObject.
    /// </summary>
    [Serializable]
    public struct ActivateGameObjectAnimation : IAnimation
    {
        [Tooltip("The GameObject to enable/disable.")]
        public GameObject Target;
        [Tooltip("Should the Target be enabled (true) or disabled (false)?")]
        public bool TargetActive;

        public void Update(float percentDone)
        {
            // Don't care about lerping anything here, just set the active state
            Target.SetActive(TargetActive);
        }
    }

    /// <summary>
    /// An animation whch rotates the <see cref="Target"/> Transform.
    /// </summary>
    [Serializable]
    public struct RotationAnimation : IAnimation
    {
        [Tooltip("The Transform to rotate.")]
        public Transform Target;
        [Tooltip("The end rotation in euler angles.")]
        public Vector3 TargetEulerAngles;

        private Quaternion startRotation;
        private Quaternion endRotation;

        public void Update(float percentDone)
        {
            if (percentDone == 0)
            {
                startRotation = Target.localRotation;
                endRotation = Quaternion.Euler(TargetEulerAngles);
            }

            Target.localRotation = Quaternion.Slerp(startRotation, endRotation, percentDone);
        }
    }
}
