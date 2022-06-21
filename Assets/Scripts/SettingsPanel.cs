using Nova;
using UnityEngine;

namespace NovaSamples.HandMenu
{
    /// <summary>
    /// The UI panel responsible for managing a set of interactive toggles/buttons/sliders to adjust certain system settings (e.g. volume).
    /// </summary>
    public class SettingsPanel : Panel
    {
        [Header("Quick Toggles")]
        [SerializeField]
        [Tooltip("The UIBlock parent of all the quick toggles.")]
        private UIBlock quickToggleRoot = null;

        [Header("Volume")]
        [SerializeField]
        [Tooltip("The ItemView with a ToggleVisuals representing a \"mute\" toggle button.")]
        private ItemView muteButtonView = null;
        [SerializeField]
        [Tooltip("The Interactable root of a volume slider control.")]
        private Interactable volumeSlider = null;
        [SerializeField]
        [Tooltip("The UIBlock to make bigger as volume increases.")]
        private UIBlock volumeFillBar = null;
        [SerializeField]
        [Tooltip("The UIBlock to make smaller as volume increases.")]
        private UIBlock volumeUnfillBar = null;

        /// <summary>
        /// The <see cref="ToggleVisuals"/> attached to the <see cref="muteButtonView"/>. 
        /// </summary>
        private ToggleVisuals MuteButtonVisuals => muteButtonView.Visuals as ToggleVisuals;

        /// <summary>
        /// The current volume "muted" state.
        /// </summary>
        private bool IsMuted => !MuteButtonVisuals.ToggledOn;

        /// <summary>
        /// The current volume level. 1 == 100%.
        /// </summary>
        private float volumePercent = 0.5f;

        private void OnEnable()
        {
            // Subscribe to toggle click events on the quick toggle root
            quickToggleRoot.AddGestureHandler<Gesture.OnClick, ToggleVisuals>(HandleQuickToggleClicked);

            // Subscribe to mute button toggle events
            muteButtonView.UIBlock.AddGestureHandler<Gesture.OnClick, ToggleVisuals>(HandleMuteToggled);

            // Subscribe to drag events on the volume slider
            volumeSlider.UIBlock.AddGestureHandler<Gesture.OnDrag>(HandleVolumeSlider);
        }

        private void OnDisable()
        {
            // Unsubscribe from the gesture events previously subscribed to in OnEnable
            quickToggleRoot.RemoveGestureHandler<Gesture.OnClick, ToggleVisuals>(HandleQuickToggleClicked);
            muteButtonView.UIBlock.RemoveGestureHandler<Gesture.OnClick, ToggleVisuals>(HandleMuteToggled);
            volumeSlider.UIBlock.RemoveGestureHandler<Gesture.OnDrag>(HandleVolumeSlider);
        }

        /// <summary>
        /// Toggle the volume mute state on click.
        /// </summary>
        private void HandleMuteToggled(Gesture.OnClick evt, ToggleVisuals target)
        {
            // In this event handler, target == MuteButtonVisuals.

            if (!target.ToggledOn)
            {
                // If it was muted, bump it back to at least 25%
                volumePercent = Mathf.Max(0.25f, volumePercent);
            }

            // Flip the toggled state
            target.Toggle();

            // Making a simple assumption that the slider is draggable
            // either only on the X axis or only on the Y axis.
            int axis = volumeSlider.Draggable.X ? 0 : 1;

            // Sync the slider visuals to the newly toggled mute state
            UpdateVolumeVisuals(axis);
        }

        /// <summary>
        /// Adjust the volume slider based on the current drag pointer location
        /// </summary>
        /// <param name="evt"></param>
        private void HandleVolumeSlider(Gesture.OnDrag evt)
        {
            // Convert the current drag position into local space
            Vector3 pointerPositionLocalSpace = volumeSlider.transform.InverseTransformPoint(evt.PointerPositions.Current);

            // Check which axis is designated as draggable.
            // Making a simple assumption that the slider is draggable
            // either only on the X axis or only on the Y axis.
            int axis = evt.DraggableAxes.X ? 0 : 1;

            // This is the max size of draggable "space" the visuals
            // will be resized relative to this value.
            float size = volumeSlider.UIBlock.CalculatedSize[axis].Value;

            // The min edge position along the draggable/slidable axis
            float minEdge = -0.5f * size;

            // The distance between the min edge (e.g. left or bottom)
            // and the current pointer drag position.
            float distanceFromEdge = pointerPositionLocalSpace[axis] - minEdge;
            
            // Convert the pointer position into a percentage
            // between the min edge, 0, and the max edge, 1.
            float newVolume = Mathf.Clamp01(distanceFromEdge / size);

            // Update the volume value
            volumePercent = newVolume;

            // If the slider no longer matches the mute
            // toggle state, then update the mute toggle.
            if (IsMuted == volumePercent > 0)
            {
                MuteButtonVisuals.Toggle();
            }

            // Sync the slider visuals to the newly adjusted volume level.
            UpdateVolumeVisuals(axis);
        }

        /// <summary>
        /// Adjust the size of the volume fill bars to match the mute/volume state.
        /// </summary>
        /// <param name="axis"></param>
        private void UpdateVolumeVisuals(int axis)
        {
            // If we're muted, set to 0, otherwise use the cached volume percent
            float percent = IsMuted ? 0 : volumePercent;

            // Resize the visuals to match the volume setting.
            volumeFillBar.Size[axis] = Length.Percentage(percent);
            volumeUnfillBar.Size[axis] = Length.Percentage(1 - percent);
        }

        /// <summary>
        /// Toggle the visual state on click.
        /// </summary>
        private void HandleQuickToggleClicked(Gesture.OnClick evt, ToggleVisuals target)
        {
            target.Toggle();
        }
    }
}

