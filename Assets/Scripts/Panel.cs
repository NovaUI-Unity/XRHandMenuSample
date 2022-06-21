using Nova;
using System;
using UnityEngine;

namespace NovaSamples.HandMenu
{
    /// <summary>
    /// A base class for components managing a set of UI controls, e.g. a list/grid of items, buttons, sliders, etc.
    /// </summary>
    public abstract class Panel : MonoBehaviour
    {
        /// <summary>
        /// A flag to indicate a UI-specific pointer effect request
        /// </summary>
        public virtual bool UseTorchPointer { get; } = false; 

        /// <summary>
        /// The event which will fire when this panel is closed.
        /// </summary>
        /// <remarks>
        /// Here we're using Actions instead of
        /// the Nova event system because these
        /// panels aren't all part of a single UIBlock
        /// hierarchy, so the events won't propagate
        /// through the GameObjects without UIBlock
        /// components.
        /// </remarks>
        public event Action OnClosed;

        [SerializeField]
        [Tooltip("The UIBlock root of the exit button. This panel will close and fire an event when the given Exit Button is \"clicked\".")]
        private UIBlock exitButton = null;

        /// <summary>
        /// Initialize on Awake
        /// </summary>
        private void Awake()
        {
            // Subscribe to close button click events
            exitButton.AddGestureHandler<Gesture.OnClick>(HandleExitClicked);
        }

        /// <summary>
        /// Open this panel at the given <paramref name="worldPosition"/> and <paramref name="worldRotation"/>.
        /// </summary>
        /// <param name="worldPosition">The position to open the panel in world space.</param>
        /// <param name="worldRotation">The world rotation to apply to the panel when it opens.</param>
        public virtual void Open(Vector3 worldPosition, Quaternion worldRotation)
        {
            // Enable this panel object
            gameObject.SetActive(true);

            // Assign position and rotation
            transform.position = worldPosition;
            transform.rotation = worldRotation;
        }

        /// <summary>
        /// Handle the click event
        /// </summary>
        private void HandleExitClicked(Gesture.OnClick evt)
        {
            // Disable panel and fire that it's been closed.
            gameObject.SetActive(false);

            // Notify listeners the panel has been closed
            OnClosed?.Invoke();
        }
    }
}

