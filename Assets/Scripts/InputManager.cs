using Nova;
using System;
using UnityEngine;

namespace NovaSamples.HandMenu
{
    /// <summary>
    /// Tracks two OVRHands and calls <see cref="Interaction.Point(Sphere, uint, object, int, InputAccuracy)"/> to send hand-tracked gesture events to the Nova UI content.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
#if OCULUS

        /// <summary>
        /// A struct tracking a single OVRHand and a SphereCollider on tip of the hand's index finger
        /// </summary>
        [Serializable]
        private struct SingleHand
        {
            [Tooltip("A sphere collider on the tip of the Hand's index finger")]
            public SphereCollider Collider;
            [Tooltip("The tracked hand.")]
            public OVRHand Hand;

            [NonSerialized]
            public uint ID;

            public void Update()
            {
                if (!Hand.IsTracked)
                {
                   // Interaction.Cancel(ID);
                   // return;
                }

                Interaction.Point(Collider, ID);
            }
        }

        private const uint LeftHandID = 0;
        private const uint RightHandID = 1;

        [SerializeField]
        [Tooltip("The left hand to track.")]
        private SingleHand leftHand = new SingleHand()
        {
            ID = LeftHandID,
        };

        [SerializeField]
        [Tooltip("The right hand to track.")]
        private SingleHand rightHand = new SingleHand()
        {
            ID = RightHandID,
        };


        private void Update()
        {
            // Step OVR
            OVRInput.Update();

            // Update each hand.
            leftHand.Update();
            rightHand.Update();
        }
#endif
    }
}

