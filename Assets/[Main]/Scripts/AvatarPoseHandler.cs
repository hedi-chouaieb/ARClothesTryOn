using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARClothesTryOn
{
    public class AvatarPoseHandler : MonoBehaviour
    {
        [System.Serializable]
        public struct AvatarBodyLink
        {
            public Transform bodyPart;
            public Transform avatarPart;

            public void SetRotation()
            {
                if (avatarPart == null || bodyPart == null)
                    return;
                var angle = bodyPart.rotation.eulerAngles;
                // angle.x = avatarPart.rotation.eulerAngles.x;
                avatarPart.rotation = Quaternion.Euler(angle);
            }
        }

        [SerializeField] private AvatarBodyLink[] avatarBodyLinks;

        private Quaternion[] startingRotationBodyParts;
        private Quaternion[] startingRotationAvatarParts;

        private void Start()
        {
            startingRotationBodyParts = new Quaternion[avatarBodyLinks.Length];
            startingRotationAvatarParts = new Quaternion[avatarBodyLinks.Length];
            for (int i = 0; i < avatarBodyLinks.Length; i++)
            {
                if (avatarBodyLinks[i].bodyPart)
                    startingRotationBodyParts[i] = avatarBodyLinks[i].bodyPart.rotation;
                if (avatarBodyLinks[i].avatarPart)
                    startingRotationAvatarParts[i] = avatarBodyLinks[i].avatarPart.rotation;
            }
        }

        private void Update()
        {
            UpdateLinks();
        }

        public void UpdateLinks()
        {
            for (int i = 0; i < avatarBodyLinks.Length; i++)
            {
                var avatarPart = avatarBodyLinks[i].avatarPart;
                var bodyPart = avatarBodyLinks[i].bodyPart;

                if (avatarPart == null || bodyPart == null)
                    continue;

                avatarPart.rotation = Quaternion.Euler(startingRotationAvatarParts[i].eulerAngles + (bodyPart.rotation.eulerAngles - startingRotationBodyParts[i].eulerAngles));
            }
        }

    }
}
