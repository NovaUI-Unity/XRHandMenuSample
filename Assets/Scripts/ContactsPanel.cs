using Nova;
using System.Collections.Generic;
using UnityEngine;

namespace NovaSamples.HandMenu
{
    /// <summary>
    /// The set of components used to visually represent a 
    /// <see cref="Contact"/> in the <see cref="ContactsPanel"/>.
    /// </summary>
    public class ContactVisuals : ItemVisuals
    {
        [Header("Profile Details")]
        [Tooltip("Will display the contact's profile picture.")]
        public UIBlock2D ProfileImage = null;
        [Tooltip("Will display the contact's name.")]
        public TextBlock Name = null;

        [Header("Match Profile Color")]
        [Tooltip("The background visual to match to the contact's profile color.")]
        public UIBlock Background;
        [Tooltip("The visual whose body/gradient colors will be adjusted to accentuate the Background visual.")]
        public UIBlock2D Accent;
    }

    /// <summary>
    /// A simplified struct holding information
    /// relevant to a specific user contact.
    /// </summary>
    public struct Contact
    {
        /// <summary>
        /// A color associated with a given contact's profile.
        /// </summary>
        public Color ProfileColor;

        /// <summary>
        /// The contact's profile picture.
        /// </summary>
        public Texture2D ProfileImage;

        /// <summary>
        /// The contact's name, first and last.
        /// </summary>
        public string Name;
    }

    /// <summary>
    /// The UI panel responsible for managing a scrollable, 3D carousel of user contact cards.
    /// </summary>
    public class ContactsPanel : Panel
    {
        [Header("List")]
        [SerializeField]
        [Tooltip("The scrollable list to display user contacts.")]
        private ListView listView = null;
        [SerializeField]
        [Tooltip("The set of contacts will be randomly generated, so this is the number of list elements to generate.")]
        private int numberOfContacts = 25;

        [Header("Profile Pictures")]
        [SerializeField]
        [Tooltip("The set of \"profile pictures\" to pull from when generating the list of contacts.")]
        private List<Texture2D> profilePicturePool = null;

        [Header("Animation")]
        [SerializeField]
        [Tooltip("One of two animations to run as the list of contacts is scrolled. Only need to assign the Target UIBlock2D. The animation color will change dynamically.")]
        private BodyGradientAnimation backgroundPortalAnimation;
        [Tooltip("Two of two animations to run as the list of contacts is scrolled. Only need to assign the Target UIBlock2D. The animation color will change dynamically.")]
        [SerializeField]
        private ShadowColorAnimation portalFrameAnimation;
        [SerializeField]
        [Tooltip("The duration, in seconds, to run the Background Portal Animation.")]
        private float duration = 0.15f;

        [Header("Style")]
        [Tooltip("We'll apply an accent color to the user profile. This is the brightness value of that accent color in HSV space.")]
        private float profileGradientBrightness = 0.4f;

        /// <summary>
        /// The <see cref="listView"/>'s data source containing a randomly generated set of <see cref="Contact"/>s.
        /// </summary>
        private List<Contact> contacts = new List<Contact>();

        /// <summary>
        /// The animation handle tracking the active "glow" animation
        /// </summary>
        private AnimationHandle backgroundAnimationHandle;

        /// <summary>
        /// The index into <see cref="contacts"/> of the object closest
        /// to the center of this panel. As the user scrolls the list of
        /// contacts, this value will change, and we'll animate panel's background
        /// color as a new "contact card" is centered in view.
        /// </summary>
        /// <remarks>Default to -1 to indicate an "invalid" index.</remarks>
        private int centerIndex = -1;

        /// <summary>
        /// The min scale applied to list items towards the edges of the carousel
        /// </summary>
        private const float MinCarouselScale = 0.75f;

        /// <summary>
        /// The scalar applied to our Y rotation values in the carousel.
        /// The default range is [-90, 90] degrees, so a value of 0.25f here
        /// would lead to a range of [-90 * 0.25, 90 * 0.25], which is [-22.5, 22.5].
        /// </summary>
        private const float CarouselRotationScalar = 0.25f;

        private void Start()
        {
            EnsureDataSourceInitialized();

            // Subscribe to bind events in the listView.
            listView.AddDataBinder<Contact, ContactVisuals>(Bind);

            // Subscribe to scroll events on the listView's UIBlock.
            // We subscribe on the UIBlock here, as opposed to the listView
            // directly, because it's not a "list item" that's going to be scrolled
            // but rather the listView itself.
            listView.UIBlock.AddGestureHandler<Gesture.OnScroll>(Scrolled);

            // Set the list of apps as the listView's data source.
            // If the listView is enabled, this will start populating
            // the list with list items, which is why we must subscribe
            // to the bind events before assigning a data source.
            listView.SetDataSource(contacts);

            // Update the panel's background color
            // now that we assigned a data source
            // and will have a "contact card" in view.
            UpdateBackgroundColor();
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
                // Get the "contact card" visually representing contacts[i] 
                if (!listView.TryGetItemView(i, out ItemView contactCard))
                {
                    // We won't hit this in this sample (assuming no modifications),
                    // but it's good practice to validate the item hasn't been
                    // destroyed or anything.
                    continue;
                }

                // Apply Z offsets and scale adjustments to
                // scroll along an arc.
                ApplyCarouselAdjustment(contactCard);
            }

            // Update the panel's background color to the
            // center-most "contact card" in view.
            UpdateBackgroundColor();
        }

        /// <summary>
        /// Find the center-most "contact card" in view and
        /// animate this panel's background color to match
        /// the centered contact's profile color.
        /// </summary>
        private void UpdateBackgroundColor()
        {
            // Get the min/max range of items in view
            int minIndex = listView.MinLoadedIndex;
            int maxIndex = listView.MaxLoadedIndex;

            // Cache this so we can check if it has changed
            int center = centerIndex;

            for (int i = minIndex; i <= maxIndex; ++i)
            {
                // Get the "contact card" visually representing contacts[i] 
                if (!listView.TryGetItemView(i, out ItemView contactCard))
                {
                    // We won't hit this in this sample (assuming no modifications),
                    // but it's good practice to validate the item hasn't been
                    // destroyed or anything.
                    continue;
                }

                UIBlock uiBlock = contactCard.UIBlock;

                // If the contact card root overlaps the center of the listView, that's our center-most item.
                if (Mathf.Abs(uiBlock.transform.localPosition.x) <= uiBlock.CalculatedSize.X.Value * 0.5f)
                {
                    // Grab the index, and exit the loop
                    center = i;
                    break;
                }
            }

            if (center == centerIndex)
            {
                // centerIndex didn't change, so we don't
                // need to update/animate anything.
                return;
            }

            // Update the centerIndex
            centerIndex = center;

            // Stop any actively running animation
            backgroundAnimationHandle.Cancel();

            // Animate this panel's background color to the center-most contact's profile color.
            backgroundPortalAnimation.GradientTargetColor = contacts[center].ProfileColor;
            portalFrameAnimation.TargetColor = contacts[center].ProfileColor;
            backgroundAnimationHandle = backgroundPortalAnimation.Run(duration).Include(portalFrameAnimation);
        }

        /// <summary>
        /// A visual effect applied to the items in the <see cref="listView"/>,
        /// which will adjust their z position, scale, and y rotation, to appear
        /// as if the objects are scrolling along an arc. Center-most items will
        /// appear larger and closer to the user, while the items closer to the edges
        /// of the view will be scaled down, moved back, and rotated slightly away.
        /// </summary>
        private void ApplyCarouselAdjustment(ItemView listItem)
        {
            // The effective "radius" of the arc determined by the listView width
            float radius = listView.UIBlock.CalculatedSize.X.Value * 0.5f;

            UIBlock uiBlock = listItem.UIBlock;

            // The horizontal center position of the item in view
            float xPos = uiBlock.transform.localPosition.x;

            // Convert the x position into a cosine value between [-1, 1]
            float cosTheta = Mathf.Clamp(xPos / radius, -1, 1);

            // Get the angle, theta, from the calculated cosine
            float theta = Mathf.Acos(cosTheta);

            // And get the corresponding sine value of theta. This is effectively
            // our "normalized" z position.
            //
            // More centered => closer to 1
            // towards edges => closer to 0
            float sinTheta = Mathf.Sin(theta);

            // Ensure the items are aligned to the back of listView bounds
            uiBlock.Alignment.Z = DepthAlignment.Back;

            // Here we offset from the back plane of the list by the
            // normalizedZPosition - normalizedZSize - normalizedBackMargin.
            // This will lead to center-most items aligned to the front plane
            // and items towards the edges will lie on the back plane.
            uiBlock.Position.Z.Percent = sinTheta - uiBlock.CalculatedSize.Z.Percent - uiBlock.CalculatedMargin.Back.Percent;

            // Using MinCarouselScale as our minimum value, scale down based on distance from
            // front of view.
            //
            // I.e.
            // center-most items have a scale of 1, and items on the edge will
            // have a scale of MinCarouselScale.
            listItem.UIBlock.transform.localScale = Vector3.Lerp(Vector3.one * MinCarouselScale, Vector3.one, sinTheta);

            // The rotation behavior we want is: as objects move closer to the
            // edge of the view (not centered), they rotate outwards, towards their
            // respective edges.
            //
            // I.e.
            // Items closer to the left edge will face towards the left.
            // Items in the center will face towards the center (towards the user).
            // Items closer to the right edge will face towards the right.
            //
            // theta will be between [0, PI] radians ([0, 180] degrees). To get the behavior
            // described above, we subtract PI/2 radians (90 degrees) to give us an angle
            // range of [-PI/2, PI/2] radians ([-90, 90] degrees). Because there are only
            // a few items in view at a given time, this configuration leads to rather
            // severe rotation differences between the centered item and those directly
            // adjacent. So we apply the CarouselRotationScalar to scale down the rotation,
            // which leads to a nicer rotational arc. 
            //
            // Finally we multiply by Mathf.Rad2Deg to convert from radians to degrees
            float yRotation = (theta - (Mathf.PI * 0.5f)) * CarouselRotationScalar * Mathf.Rad2Deg;

            // Apply our new Y rotation
            listItem.UIBlock.transform.localEulerAngles = new Vector3(0, yRotation, 0);
        }

        /// <summary>
        /// Populate an <see cref="ContactVisuals"/> object in the list with 
        /// the information from its corresponding <see cref="Contact"/> object
        /// in the data source.
        /// </summary>
        private void Bind(Data.OnBind<Contact> evt, ContactVisuals target, int index)
        {
            // The UserData on this bind event is the same value stored
            // at the given `index` into the list of contacts. I.e.
            // evt.UserData == contacts[index]
            Contact contact = evt.UserData;

            // Update the visual profile picture and name
            target.Name.Text = contact.Name;
            target.ProfileImage.SetImage(contact.ProfileImage);

            Color profileColor = contact.ProfileColor;

            // Color the 'contact card' visuals to match
            // the given contact's profile color.
            target.Background.Color = profileColor;
            target.Accent.Color = profileColor;

            Color.RGBToHSV(profileColor, out float h, out float s, out float v);

            // Here we want to match the profile color, but we want to
            // adjust the brightness. Purely aesthetic.
            Color dark = Color.HSVToRGB(h, s, profileGradientBrightness);
            target.Accent.Gradient.Color = dark;

            // Because the `contact card` list item is just now being bound into view
            // its layout properties are likely stale or uncalculated, since the
            // Nova Engine update won't run until the end of the current frame.
            //
            // Explicitly call CalculateLayout() here to ensure the size/position
            // of this list item have non-zero calculated values before we try to
            // use them to determine the carousel adjustment.
            target.View.UIBlock.CalculateLayout();

            // Adjust the Z offset, scale, and rotation along the scrolling arc.
            ApplyCarouselAdjustment(target.View);
        }

        /// <summary>
        /// Generate a random list of <see cref="Contact"/>s, which 
        /// we'll use as the <see cref="listView"/>'s data source.
        /// </summary>
        private void EnsureDataSourceInitialized()
        {
            if (contacts.Count > 0)
            {
                // Already initialized, don't need to repopulate.
                return;
            }

            for (int i = 0; i < numberOfContacts; i++)
            {
                // Create and add a new contact, pulling
                // from the list of profile pictures
                Texture2D contactTexture = profilePicturePool[Random.Range(0, profilePicturePool.Count - 1)];

                contacts.Add(new Contact()
                {
                    Name = contactTexture.name,
                    ProfileImage = contactTexture,
                    ProfileColor = Color.HSVToRGB(Random.value, 1, 1),
                });
            }
        }
    }
}

