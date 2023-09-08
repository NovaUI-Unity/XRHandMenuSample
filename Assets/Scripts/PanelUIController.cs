using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace NovaSamples.HandMenu
{
    /// <summary>
    /// The component responsible for positioning a hand-tracked menu and opening/closing various UI panels triggered by the hand menu.
    /// </summary>
    public class PanelUIController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The camera attached to the user's head.")]
        private Camera headTrackedCamera = null;

        [Header("Hand Tracking")]
        [SerializeField]
        [Tooltip("Unity's articulated hand-tracking event component.")]
        private XRHandTrackingEvents hand = null;

        [SerializeField]
        [Tooltip("A transform at the center of the hand-tracked palm.")]
        private Transform palmTransform = null;
        [SerializeField]
        [Tooltip("The threshold, in degrees, between the user's head and palm to activate/deactivate the Hand Launcher UI")]
        private float showLauncherThreshold = 35f;

        [Header("Panel Launching")]
        [SerializeField]
        [Tooltip("The controller responsible for displaying a list of buttons that, when selected, will launch a given Panel UI.")]
        private HandLauncher handLauncher = null;
        [SerializeField]
        [Tooltip("The Transform used to position/rotate the Hand Launcher UI.")]
        private Transform handLauncherPivot = null;
        [SerializeField]
        [Tooltip("The Transform providing a world location to pop up a Panel UI.")]
        private Transform panelPopupLocation = null;
        [SerializeField]
        [Tooltip("The list of panels which can be launched from the Hand Launcher UI.")]
        private List<Panel> Panels = null;

        [Header("Lights")]
        [SerializeField]
        [Tooltip("The primary direction light in the scene.")]
        private Light directionLight = null;
        [SerializeField]
        [Tooltip("A point light on one of the OVRHands index fingers.")]
        private Light fingerTipPointLight = null;

        /// <summary>
        /// Is the hand launcher UI enabled? 
        /// </summary>
        private bool handLauncherActive = false;

        /// <summary>
        /// Is the selected panel UI enabled? 
        /// </summary>
        private bool selectedPanelActive = false;

        /// <summary>
        /// Is the user looking at their palm?
        /// </summary>
        private bool HandLauncherShouldBeActive
        {
            get
            {
                float angleBetweenHeadAndPalm = Vector3.Angle(-palmTransform.up, headTrackedCamera.transform.forward);

                return Mathf.Abs(angleBetweenHeadAndPalm) < showLauncherThreshold;
            }
        }

        private void Awake()
        {
            // Subscribe to panel open events
            handLauncher.OnPanelSelected += HandlePanelSelected;

            // Subscribe to panel close events
            for (int i = 0; i < Panels.Count; i++)
            {
                Panels[i].OnClosed += HandleSelectedPanelClosed;
                Panels[i].gameObject.SetActive(false);
            }

            // Start with the hand launcher inactive
            handLauncher.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (selectedPanelActive)
            {
                // Don't show hand launcher if a panel is active.
                return;
            }

            if (!hand.handIsTracked)
            {
                // Hand isn't tracked, ensure hand launcher is hidden.
                HideHandLauncher();
                return;
            }

            if (handLauncherActive) // Currently active
            {
                if (!HandLauncherShouldBeActive) // Should be inactive
                {
                    // Hide
                    HideHandLauncher();
                }
                else // Should be active
                {
                    // Update position
                    RepositionMenu();
                }
            }
            else if (HandLauncherShouldBeActive) // Not active, but it should be
            {
                // Open
                ShowHandLauncher();

                // Update position
                RepositionMenu();
            }
        }

        /// <summary>
        /// Close the <see cref="handLauncher"/> UI.
        /// </summary>
        private void HideHandLauncher()
        {
            if (!handLauncherActive)
            {
                return;
            }

            handLauncherActive = false;
            handLauncher.Hide();
        }

        /// <summary>
        /// Open the <see cref="handLauncher"/> UI.
        /// </summary>
        private void ShowHandLauncher()
        {
            if (handLauncherActive)
            {
                return;
            }

            handLauncherActive = true;
            handLauncher.Show();
        }

        /// <summary>
        /// Handle panel closed event.
        /// </summary>
        private void HandleSelectedPanelClosed()
        {
            selectedPanelActive = false;

            directionLight.enabled = true;
            fingerTipPointLight.enabled = false;
        }

        /// <summary>
        /// Open a given panel UI when it's selected from the <see cref="handLauncher"/>.
        /// </summary>
        /// <param name="item">The item clicked in the <see cref="handLauncher"/> UI.</param>
        private void HandlePanelSelected(PanelItem item)
        {
            if (item.Panel == null)
            {
                // Nothing to open
                return;
            }

            // Get the index of the selected panel.
            int index = Panels.IndexOf(item.Panel);

            if (index == -1)
            {
                // Not found, nothing to open.
                return;
            }

            Panel panel = Panels[index];

            // Panel requested Torch Pointer, so
            // enable the point light and disable
            // the primary direction light.
            if (panel.UseTorchPointer)
            {
                directionLight.enabled = false;
                fingerTipPointLight.enabled = true;
            }

            // Open the panel at the popup location
            panel.Open(panelPopupLocation.position, panelPopupLocation.rotation);
            
            // Indicate a panel is active, so we don't activate the handLauncher
            selectedPanelActive = true;

            // Close
            HideHandLauncher();
        }

        /// <summary>
        /// Reposition the <see cref="handLauncher"/> to a fixed offset from the user's hand.
        /// </summary>
        private void RepositionMenu()
        {
            handLauncher.transform.SetPositionAndRotation(handLauncherPivot.position, handLauncherPivot.rotation);
        }
    }
}

