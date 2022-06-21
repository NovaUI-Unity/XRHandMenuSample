using Nova;
using UnityEngine;

namespace NovaSamples.HandMenu
{
    /// <summary>
    /// Converts <see cref="Gesture.OnDrag"/> events along the Z axis into 
    /// <see cref="Gesture.OnClick"/> events. Animates either the Z size or the Y axis
    /// rotation of a provided <see cref="PushVisual"/> to visualize the users' press actions.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class PushButton : MonoBehaviour
    {
        private const float MinYRotation = -45;
        private const float MaxYRotation = 0;

        private enum AnimationStyle
        {
            ResizeZ,
            SwingY
        }

        [Tooltip("This is the visual that will be animated as the push-interaction is performed. This should be a different UIBlock than the one on this Game Object.")]
        public UIBlock PushVisual = null;

        [Tooltip("The percentage of the collidable volume the pointer must drag to trigger a click event. 1 == 100%.")]
        public float ClickThresholdPercent = 0.25f;

        [Tooltip("The duration, in seconds, for the button to reanimate back into its default state from a pushed state.")]
        public float UnpushAnimationDuration = 0.15f;

        [Tooltip("The style of animation effect to apply to the Push Visual.")]
        [SerializeField]
        private AnimationStyle animationStyle = AnimationStyle.ResizeZ;
        [SerializeField]
        [Tooltip("If true, the click event won't fire until after the release animation has completed. If false, click will fire immediately after release.")]
        public bool ClickAfterAnimationCompleted = false;

        /// <summary>
        /// The interactable component on this.gameObject.
        /// </summary>
        private Interactable interactable = null;

        /// <summary>
        /// The animation handle tracking the animation triggered 
        /// by a release or cancel gesture event.
        /// </summary>
        private AnimationHandle resetHandle = default;

        /// <summary>
        /// Will be true after the first drag event which
        /// surpasses the click threshold. Because this component
        /// effectively reinterprets drags to fire click events,
        /// this value signifies what this component determines to 
        /// be a "pointer enter" event.
        /// </summary>
        public bool PushedThisFrame { get; private set; }

        /// <summary>
        /// Updated per drag event. Indicates that over the course
        /// of a drag gesture, the pointer moved through the collidable
        /// volume enough to trigger a click event on release.
        /// </summary>
        private bool clickThresholdSurpassed = false;

        /// <summary>
        /// When the gesture began, did the pointer enter through the front half
        /// of the collidable volume? We don't want to trigger click events
        /// if the pointer entered through the back half of the collidable volume.
        /// </summary>
        private bool dragEnteredFromFrontFace = false;

        private void OnEnable()
        {
            // Ensure initialized
            if (interactable == null)
            {
                // Guaranteed to be there because Interactable is a required component
                interactable = GetComponent<Interactable>();

                // ensure draggable on z
                interactable.Draggable = new ThreeD<bool>(false, false, true);
            }

            // Subscribe to the necessary gesture events to track/trigger click events
            interactable.UIBlock.AddGestureHandler<Gesture.OnPress>(Press);
            interactable.UIBlock.AddGestureHandler<Gesture.OnDrag>(Drag);
            interactable.UIBlock.AddGestureHandler<Gesture.OnRelease>(Release);
            interactable.UIBlock.AddGestureHandler<Gesture.OnCancel>(Cancel);

            // Ensure starting from an expected state
            ResetPushState();
            resetHandle.Complete();
        }

        private void OnDisable()
        {
            // Unsubscribe from gesture events, so we don't trigger clicks while disabled
            interactable.UIBlock.RemoveGestureHandler<Gesture.OnPress>(Press);
            interactable.UIBlock.RemoveGestureHandler<Gesture.OnDrag>(Drag);
            interactable.UIBlock.RemoveGestureHandler<Gesture.OnRelease>(Release);
            interactable.UIBlock.RemoveGestureHandler<Gesture.OnCancel>(Cancel);

            // Wrap up any running animation
            resetHandle.Complete();
        }

        /// <summary>
        /// On press, check the entry point of the collision.
        /// </summary>
        private void Press(Gesture.OnPress evt)
        {
            // Convert the press event intersection point into local space
            Vector3 entryPointLocalSpace = evt.Receiver.transform.InverseTransformPoint(evt.PointerWorldPosition);

            // If the z value is greater than 0, the pointer entered
            // through the back side of the collidable volume.
            dragEnteredFromFrontFace = entryPointLocalSpace.z <= 0;
        }

        /// <summary>
        /// On drag, adjust the <see cref="PushVisual"/> Z size to appear as if it's being pushed.
        /// Also determine if the user has dragged "enough" for us to fire a click event on release.
        /// </summary>
        private void Drag(Gesture.OnDrag evt)
        {
            if (!dragEnteredFromFrontFace)
            {
                // If the gesture started on the wrong side of the collidable volume,
                // then we won't update any visuals or fire clicks
                return;
            }

            // Stop the current animation if it exists
            resetHandle.Complete();

            // We can still track/trigger
            // clicks without animating anything
            if (PushVisual != null)
            {
                switch (animationStyle)
                {
                    case AnimationStyle.ResizeZ:
                        // Decrease the depth of the push visual by the amount dragged since the last drag event.
                        // If the evt.DragDeltaLocalSpace.z > 0, this will increase the size as well. Since it
                        // shrinks/expends as the pointer moves through the volume, it will appear as if the button
                        // is being pushed.
                        PushVisual.Size.Z.Value = PushVisual.CalculatedSize.Z.Value - evt.DragDeltaLocalSpace.z;
                        break;
                    case AnimationStyle.SwingY:

                        // Get the current rotation
                        Vector3 rotation = PushVisual.transform.localEulerAngles;
                        // Ensure we start within our min/max range to avoid pops when we clamp below.
                        rotation.y = rotation.y > 0 ? rotation.y - 360 : rotation.y;

                        // Ensure RotateSize is enabled and Alignment is configured.
                        // This will allow us to create a "hinge" effect just by
                        // rotating the visual on the Y axis -- the layout system handles the rest.
                        // The hinge will swing from the X and Z alignment points (i.e. Front and Left).
                        PushVisual.RotateSize = true;
                        PushVisual.Alignment = Alignment.CenterLeftFront;

                        // Convert the drag dimensions into a rotation about the Y axis.
                        // We want an angle between [-90, 0] such that when drag distance is 0,
                        // The object isn't rotated (lies on X/Y plane) and when the drag distance is 100% of
                        // the collider depth, the object is parallel to the Y/Z plane. atan(-dragDelta / width) gives us the
                        // rotation delta to apply this frame.
                        float thetaInDegrees = Mathf.Atan(-evt.DragDeltaLocalSpace.z / PushVisual.CalculatedSize.X.Value) * Mathf.Rad2Deg;

                        // -90 degrees is too far rotated, clamp within a -45 degree rotation. This is for aesthetics/legibility.
                        rotation.y = Mathf.Clamp(rotation.y + thetaInDegrees, MinYRotation, MaxYRotation);

                        // Apply our new rotation
                        PushVisual.transform.localEulerAngles = rotation;
                        break;
                }
            }

            // Cache this so we can check if it changed this frame
            bool wasPushed = clickThresholdSurpassed;

            // Convert the amount translated along z into a percent of 
            // percent of the collidable volume depth. 1 == 100%.
            float percentTranslatedThroughVolume = Mathf.Abs(evt.RawTranslationLocalSpace.z) / interactable.UIBlock.CalculatedSize.Z.Value;

            // Once the click threshold is surpassed, we don't want to unset it until 
            // release or cancel, so here we're `OR`ing the existing state with the new state.
            clickThresholdSurpassed |= percentTranslatedThroughVolume >= ClickThresholdPercent;

            // Was the threshold surpassed this frame?
            PushedThisFrame = !wasPushed && clickThresholdSurpassed;
        }

        /// <summary>
        /// On release, fire a click event if the click threshold has been met. Also reset the tracked push state.
        /// </summary>
        private void Release(Gesture.OnRelease evt)
        {
            // If this release event followed a drag event which,
            // surpassed our configured click threshold, fire a click event.
            // Reading it here because we're about to reset clickThresholdSurpassed.
            bool fireClickEvent = evt.WasDragged && clickThresholdSurpassed;

            // Reset the push visuals to the unpushed state
            ResetPushState();

            if (fireClickEvent)
            {
                Gesture.OnClick click = new Gesture.OnClick() { Interaction = evt.Interaction };

                if (ClickAfterAnimationCompleted)
                {
                    // Create a new ClickAnimationEvent and schedule it to fire
                    // after the reset animation has completed.
                    ClickAnimationEvent clickEvent = new ClickAnimationEvent()
                    {
                        Target = evt.Receiver,
                        ClickEvent = click,
                    };

                    resetHandle.Chain(clickEvent, 0);
                }
                else
                {
                    // Don't wait for the animation
                    evt.Receiver.FireGestureEvent(click);
                }
            }
        }

        /// <summary>
        /// On cancel, reset the tracked push state.
        /// </summary>
        private void Cancel(Gesture.OnCancel evt)
        {
            // Reset the push visuals to the unpushed state
            ResetPushState();
        }

        /// <summary>
        /// Clear the flags set in handling the gesture events
        /// and kick off an animation to "unpush" the "pushed" visuals.
        /// </summary>
        private void ResetPushState()
        {
            // Clear the flags that may have been set
            // over the course of the drag events we
            // received.
            dragEnteredFromFrontFace = false;
            clickThresholdSurpassed = false;
            PushedThisFrame = false;

            if (PushVisual == null)
            {
                // Nothing to animate
                return;
            }

            switch (animationStyle)
            {
                case AnimationStyle.ResizeZ:
                    // Create an animation with the current
                    // state of the PushVisual
                    UnpushAnimation resetSize = new UnpushAnimation()
                    {
                        Target = PushVisual,
                        StartingSize = PushVisual.CalculatedSize.Z,
                    };

                    // Run the animation for the configured duration
                    resetHandle = resetSize.Run(UnpushAnimationDuration);
                    break;

                case AnimationStyle.SwingY:
                    // Create an animation with the current
                    // state of the PushVisual
                    RotationAnimation resetRotation = new RotationAnimation()
                    {
                        Target = PushVisual.transform,
                        TargetEulerAngles = new Vector3(0, 0, 0),
                    };

                    // Run the animation for the configured duration
                    resetHandle = resetRotation.Run(UnpushAnimationDuration);
                    break;
            }
        }

        /// <summary>
        /// An animation which will lerp the <see cref="Target"/>'s Z size
        /// from the <see cref="StartingSize"/> to 100% of its parent's Z size.
        /// </summary>
        private struct UnpushAnimation : IAnimation
        {
            public UIBlock Target;
            public Length.Calculated StartingSize;

            public void Update(float percentDone)
            {
                Target.Size.Z.Percent = Mathf.Lerp(StartingSize.Percent, 1, percentDone);
            }
        }

        /// <summary>
        /// An animation which will fire a <see cref="Gesture.OnClick"/> event.
        /// </summary>
        private struct ClickAnimationEvent : IAnimation
        {
            public UIBlock Target;
            public Gesture.OnClick ClickEvent;

            public void Update(float percentDone)
            {
                if (percentDone == 1)
                {
                    Target.FireGestureEvent(ClickEvent);
                }
            }
        }
    }
}
