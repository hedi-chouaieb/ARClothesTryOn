using System.Collections.Generic;
using UnityEngine;
using Mediapipe.Unity;
using Mediapipe;
using Mediapipe.Unity.CoordinateSystem;

namespace ARClothesTryOn
{
    public class BodyPoseTracker : MonoBehaviour
    {
        [SerializeField] private Transform shirt, pant, head;
        [SerializeField] private Transform[] shoes;
        [SerializeField] private float zMultiplier;
        [SerializeField] private Transform bodyPartPrefab;
        [SerializeField] private Transform handLandmark;
        [SerializeField] private Transform rightHand, rightFinger, rightWrist;
        [SerializeField] private Transform leftHand, leftFinger, leftWrist;
        [SerializeField] private Transform rightHandZReference;
        [SerializeField] private Transform leftHandZReference;
        [SerializeField] private Transform[] bodyParts;
        [SerializeField] private Animator body;
        [SerializeField] private float scaleMultiplier;
        [SerializeField] private Transform[] bodyPartMasks;
        private Transform[] landmarksReferences;
        [SerializeField] private Transform[] leftHandsLandmarksReferences;
        [SerializeField] private Transform[] rightHandsLandmarksReferences;
        public enum PoseLandmark
        {
            NOSE = 0,
            LEFT_EYE_INNER = 1,
            LEFT_EYE = 2,
            LEFT_EYE_OUTER = 3,
            RIGHT_EYE_INNER = 4,
            RIGHT_EYE = 5,
            RIGHT_EYE_OUTER = 6,
            LEFT_EAR = 7,
            RIGHT_EAR = 8,
            MOUTH_LEFT = 9,
            MOUTH_RIGHT = 10,
            LEFT_SHOULDER = 11,
            RIGHT_SHOULDER = 12,
            LEFT_ELBOW = 13,
            RIGHT_ELBOW = 14,
            LEFT_WRIST = 15,
            RIGHT_WRIST = 16,
            LEFT_PINKY = 17,
            RIGHT_PINKY = 18,
            LEFT_INDEX = 19,
            RIGHT_INDEX = 20,
            LEFT_THUMB = 21,
            RIGHT_THUMB = 22,
            LEFT_HIP = 23,
            RIGHT_HIP = 24,
            LEFT_KNEE = 25,
            RIGHT_KNEE = 26,
            LEFT_ANKLE = 27,
            RIGHT_ANKLE = 28,
            LEFT_HEEL = 29,
            RIGHT_HEEL = 30,
            LEFT_FOOT_INDEX = 31,
            RIGHT_FOOT_INDEX = 32
        }

        void Start()
        {
            _camera = Camera.main;
        }
        // public void UpdateShirt()
        // {
        //     shirt.position = new Vector3((leftShoulder.position.x + rightShoulder.position.x) / 2.0f,
        //                                   (leftShoulder.position.y + rightShoulder.position.y) / 2.0f,
        //                                 (leftShoulder.position.z + rightShoulder.position.z) / 2.0f);

        //     shirt.localRotation = Quaternion.LookRotation(leftShoulderWorld.position - rightShoulderWorld.position);
        //     shirt.localScale = Vector3.one * Vector3.Distance(scaleReference1.position, scaleReference2.position);
        // }

        Transform leftEar, rightEar;
        public void UpdateClothes(IList<Landmark> poseWorldLandmarks, IList<NormalizedLandmark> poseLandmarks)
        {
            // BodyMask(poseWorldLandmarks, poseLandmarks);
            // UpdateShirt(poseWorldLandmarks, poseLandmarks);
            // UpdateAvatar(poseLandmarks);
            // UpdateAvatar(poseWorldLandmarks);

            // Get the lists of pose landmarks in image space and world space
            // Get the lists of pose landmarks in image space and world space
            var landmarks = poseLandmarks;
            var worldLandmarks = poseWorldLandmarks;

            if (landmarks == null || worldLandmarks == null || landmarks.Count == 0 || worldLandmarks.Count == 0)
            {
                pant.gameObject.SetActive(false);
                shirt.gameObject.SetActive(false);
                isLandmarksReferencesUpdated = false;
                return;
            }
            isInverted = CameraCoordinate.IsInverted(RotationAngle);
            isXReversed = CameraCoordinate.IsXReversed(RotationAngle, !IsMirrored);
            isYReversed = CameraCoordinate.IsYReversed(RotationAngle, !IsMirrored);

            var x = Draw(annotationLayer.rect, poseLandmarks[(int)PoseLandmark.LEFT_EAR], true);
            var y = Draw(annotationLayer.rect, poseLandmarks[(int)PoseLandmark.RIGHT_EAR], true);
            if (!leftEar)
            {
                leftEar = (new GameObject("LeftEar")).transform;
                leftEar.parent = this.transform;
                leftEar.localPosition = Vector3.zero;
                leftEar.localRotation = Quaternion.identity;
                leftEar.localScale = Vector3.one;
            }
            if (!rightEar)
            {
                rightEar = (new GameObject("RightEar")).transform;
                rightEar.parent = this.transform;
                rightEar.localPosition = Vector3.zero;
                rightEar.localRotation = Quaternion.identity;
                rightEar.localScale = Vector3.one;
            }
            leftEar.localPosition = x;
            rightEar.localPosition = y;

            if (landmarksReferences == null || landmarksReferences.Length != landmarks.Count)
            {
                landmarksReferences = new Transform[landmarks.Count];
                var parent = new GameObject("landmarksReferences").transform;
                parent.parent = this.transform;
                parent.localPosition = Vector3.zero;
                parent.localRotation = Quaternion.identity;
                parent.localScale = Vector3.one;
                parent.gameObject.SetActive(false);
                for (int i = 0; i < landmarks.Count; i++)
                {
                    landmarksReferences[i] = Instantiate(bodyPartPrefab, parent);
                    landmarksReferences[i].name = i.ToString();
                }
            }
            // Calculate the camera's intrinsic parameters
            var focalLength = _camera.projectionMatrix[0, 0];
            var principalPoint = new Vector2(_camera.projectionMatrix[0, 2], _camera.projectionMatrix[1, 2]);

            // Project the PoseWorldLandmarks onto the 2D image plane
            var projectedLandmarks = new List<Vector2>();
            foreach (var worldLandmark in worldLandmarks)
            {
                var worldPos = new Vector3(worldLandmark.X, worldLandmark.Y, worldLandmark.Z);
                var screenPos = _camera.WorldToScreenPoint(worldPos);
                screenPos.x -= principalPoint.x; // Apply the principal point offset in x direction
                screenPos.y -= principalPoint.y; // Apply the principal point offset in y direction
                projectedLandmarks.Add(screenPos);
            }

            // Calculate the Z-axis for each landmark in image space
            for (int i = 0; i < landmarks.Count; i++)
            {
                var landmark = landmarks[i];
                var projectedLandmark = projectedLandmarks[i];
                var worldLandmark = worldLandmarks[i];

                // Calculate the difference in depth between the projected landmark and the world landmark
                var deltaZ = worldLandmark.Z - _camera.transform.InverseTransformPoint(projectedLandmark).z;

                // Assign the Z-axis value to the PoseLandmark
                landmark.Z = deltaZ / focalLength;
            }

            // Update the PoseLandmarks
            // poseLandmarks = landmarks;
            var reference = Vector3.Lerp(Draw(annotationLayer.rect, landmarks[(int)PoseLandmark.LEFT_HIP], true), Draw(annotationLayer.rect, landmarks[(int)PoseLandmark.RIGHT_HIP], true), 0.5f);
            for (int i = 0; i < landmarks.Count; i++)
            {
                var position = Draw(annotationLayer.rect, landmarks[i], true);
                position.z -= reference.z;
                position.z /= 2f;
                landmarksReferences[i].transform.localPosition = position;
                landmarksReferences[i].transform.localScale = Vector3.one * 50;
            }
            BodyMask();
            UpdateShirt();
            // UpdateShoe();
            UpdatePant();
            // UpdateHead();
            pant.gameObject.SetActive(true);
            shirt.gameObject.SetActive(true);
            isLandmarksReferencesUpdated = true;
        }
        bool isLandmarksReferencesUpdated = false;

        public void UpdateHead()
        {
            var leftEarPosition = landmarksReferences[(int)PoseLandmark.LEFT_EAR].localPosition;
            var rightEarPosition = landmarksReferences[(int)PoseLandmark.RIGHT_EAR].localPosition;
            var nosePosition = landmarksReferences[(int)PoseLandmark.NOSE].localPosition;

            var midEarPosition = Vector3.Lerp(leftEarPosition, rightEarPosition, 0.5f);
            var headRotation = Quaternion.LookRotation(midEarPosition - nosePosition);
            var earToEarRotation = Quaternion.LookRotation(rightEarPosition - leftEarPosition);
            var headZRotation = Quaternion.FromToRotation(Vector3.right, earToEarRotation * Vector3.forward).eulerAngles.z;

            // var x = Draw(annotationLayer.rect, poseLandmarks[(int)PoseLandmark.LEFT_EAR], true);
            // var y = Draw(annotationLayer.rect, poseLandmarks[(int)PoseLandmark.RIGHT_EAR], true);
            // var xy = Vector3.Lerp(x, y, 0.5f);
            // midEarPosition.z = 0;
            // head.localPosition = xy;

            // head.localScale = Vector3.one * scaleMultiplier / (head.position.z * 2 - Camera.main.transform.position.z);
            var distanceToCamera = (Vector3.Distance(leftEar.position, this.transform.position) + Vector3.Distance(rightEar.position, this.transform.position)) / 2.0f;
            head.localScale = Vector3.one * scaleMultiplier * distanceToCamera;
            midEarPosition.z = 0;
            head.localPosition = midEarPosition;
            head.localRotation = Quaternion.Euler(headRotation.eulerAngles.x, headRotation.eulerAngles.y, Mathf.Sign(Mathf.Cos(headRotation.eulerAngles.y * Mathf.Deg2Rad)) * headZRotation);
            // head.localScale = Vector3.one * Vector3.Distance(leftEarPosition, rightEarPosition);
        }
        public void UpdatePant()
        {
            var leftHipPosition = landmarksReferences[(int)PoseLandmark.LEFT_HIP].localPosition;
            var rightHipPosition = landmarksReferences[(int)PoseLandmark.RIGHT_HIP].localPosition;
            var leftKneePosition = landmarksReferences[(int)PoseLandmark.LEFT_KNEE].localPosition;
            var rightKneePosition = landmarksReferences[(int)PoseLandmark.RIGHT_KNEE].localPosition;

            var midHipPosition = Vector3.Lerp(leftHipPosition, rightHipPosition, 0.5f);
            // var hipToKneeRotation = Quaternion.LookRotation(GetDirection(leftKneePosition, leftHipPosition));
            // var pantZRotation = Quaternion.FromToRotation(GetDirection(Vector3.zero, Vector3.up), hipToKneeRotation * Vector3.forward).eulerAngles.z;
            // var pantRotation = Quaternion.LookRotation(GetDirection(rightHipPosition, leftHipPosition));
            Quaternion pantRotation = Quaternion.LookRotation(
                                         leftHipPosition - rightHipPosition,
                                         leftHipPosition - leftKneePosition
                                    ) * Quaternion.AngleAxis(IsMirrored ? -90 : 90, Vector3.up);
            pant.localRotation = pantRotation;
            // pant.localRotation = Quaternion.Euler(pantRotation.eulerAngles.x, pantRotation.eulerAngles.y, Mathf.Sign(Mathf.Cos(pantRotation.eulerAngles.y * Mathf.Deg2Rad)) * pantZRotation);
            pant.localScale = Vector3.one *
                Mathf.Max(Vector2.Distance(rightHipPosition, leftHipPosition),
                ((rightHipPosition.z < leftHipPosition.z) ?
                Vector2.Distance(rightHipPosition, rightKneePosition) :
                Vector2.Distance(leftHipPosition, leftKneePosition)));
            midHipPosition.z = 0;
            pant.localPosition = midHipPosition;
        }
        private Vector3 GetDirection(Vector3 from, Vector3 to)
        {
            var xDiff = to.x - from.x;
            var yDiff = to.y - from.y;
            return IsMirrored ? new Vector3(xDiff, yDiff, to.z - from.z) : new Vector3(-xDiff, -yDiff, (from.z - to.z));
            var (xDir, yDir) = isInverted ? (yDiff, xDiff) : (xDiff, yDiff);
            // convert from right-handed to left-handed
            return new Vector3(isXReversed ? -xDir : xDir, isYReversed ? -yDir : yDir, -from.z + to.z);
        }
        [SerializeField] private float _scaleZ = 1.0f;
        [SerializeField] private bool visualizeZShoe;
        public void UpdateShoe(FrameAnnotation liftedObjects)
        {
            Draw(liftedObjects?.Annotations, focalLength, principalPoint, _scaleZ, visualizeZShoe);
        }
        public void Draw(IList<ObjectAnnotation> targets, Vector2 focalLength, Vector2 principalPoint, float scale, bool visualizeZ = true)
        {
            int indexShoe = 0;
            if (targets != null && targets.Count > 0)
            {
                for (; indexShoe < targets.Count && indexShoe < shoes.Length; indexShoe++)
                {
                    Draw(targets[indexShoe], shoes[indexShoe], focalLength, principalPoint, scale, visualizeZ);
                    shoes[indexShoe].gameObject.SetActive(true);
                }
            }
            for (; indexShoe < shoes.Length; indexShoe++)
            {
                shoes[indexShoe].gameObject.SetActive(false);
            }
        }

        [SerializeField] private float shoeScale = 5000.0f;
        public void Draw(ObjectAnnotation target, Transform shoe, Vector2 focalLength, Vector2 principalPoint, float zScale, bool visualizeZ = true)
        {
            var position = Draw(target.Keypoints, focalLength, principalPoint, zScale, visualizeZ);
            shoe.localPosition = position;
            shoe.localRotation = CameraCoordinate.GetApproximateQuaternion(target, RotationAngle, IsMirrored);
            shoe.localScale = Vector3.one * shoeScale * (target.Scale[0] + target.Scale[1] + target.Scale[2]) / 3.0f;

        }

        public Vector3 Draw(IList<AnnotatedKeyPoint> targets, Vector2 focalLength, Vector2 principalPoint, float zScale, bool visualizeZ = true)
        {
            if (targets == null || targets.Count == 0) return Vector3.zero;
            return Draw(targets[0], focalLength, principalPoint, zScale, visualizeZ);
        }

        public Vector3 Draw(AnnotatedKeyPoint target, Vector2 focalLength, Vector2 principalPoint, float zScale, bool visualizeZ = true)
        {
            if (visualizeZ)
            {
                return Draw(target?.Point3D, focalLength, principalPoint, zScale, true);
            }
            else
            {
                return Draw(target?.Point2D);
            }
        }

        public Vector3 Draw(NormalizedPoint2D target)
        {
            var position = annotationLayer.rect.GetPoint(target, RotationAngle, IsMirrored);
            return position;
        }

        public Vector3 Draw(Point3D target, Vector2 focalLength, Vector2 principalPoint, float zScale, bool visualizeZ = true)
        {
            var position = annotationLayer.rect.GetPoint(target, focalLength, principalPoint, zScale, RotationAngle, IsMirrored);
            if (!visualizeZ)
            {
                position.z = 0.0f;
            }
            // transform.localPosition = position;
            return position;
        }
        public InferenceMode inferenceMode;
        public Vector2 focalLength
        {
            get
            {
                if (inferenceMode == InferenceMode.GPU)
                {
                    return new Vector2(2.0975f, 1.5731f);  // magic numbers MediaPipe uses internally
                }
                return Vector2.one;
            }
        }
        public Vector2 principalPoint => Vector2.zero;
        public void UpdateShoe()
        {
            for (int i = 0; i < 2; i++)
            {
                var footIndexPosition = landmarksReferences[(int)PoseLandmark.LEFT_FOOT_INDEX + i].localPosition;
                var heelPosition = landmarksReferences[(int)PoseLandmark.LEFT_HEEL + i].localPosition;
                var anklePosition = landmarksReferences[(int)PoseLandmark.LEFT_ANKLE + i].localPosition;
                var shoeRotation = Quaternion.LookRotation(GetDirection(footIndexPosition, heelPosition));
                shoes[i].localScale = Vector3.one * Vector3.Distance(footIndexPosition, heelPosition);
                heelPosition.z = 0;
                shoes[i].localPosition = heelPosition;
                shoes[i].localRotation = shoeRotation;
            }


        }
        public void UpdateShirt()
        {

            var leftShoulderPosition = landmarksReferences[(int)PoseLandmark.LEFT_SHOULDER].localPosition;
            var rightShoulderPosition = landmarksReferences[(int)PoseLandmark.RIGHT_SHOULDER].localPosition;
            var lefHipPosition = landmarksReferences[(int)PoseLandmark.LEFT_HIP].localPosition;
            var rightHipPosition = landmarksReferences[(int)PoseLandmark.RIGHT_HIP].localPosition;

            var midShoulderPosition = Vector3.Lerp(leftShoulderPosition, rightShoulderPosition, 0.5f);
            var midHipPosition = Vector3.Lerp(lefHipPosition, rightHipPosition, 0.5f);
            // var shoulderToHipWorldRotation = Quaternion.LookRotation(GetDirection(midHipPosition, midShoulderPosition));
            // var shoulderRotation = Quaternion.LookRotation(leftShoulderWorldPosition - rightShoulderWorldPosition).eulerAngles;

            // var ratio = Vector3.Distance(leftShoulderPosition, lefHipPosition) / Vector3.Distance(leftShoulderWorldPosition, lefHipWorldPosition);
            // var midShoulderPosition = Vector3.Lerp(leftShoulderPosition, rightShoulderPosition, 0.5f);
            // var shirtRotation = Quaternion.LookRotation(GetDirection(rightShoulderPosition, leftShoulderPosition));
            // var shirtZRotation = Quaternion.FromToRotation(GetDirection(Vector3.zero, Vector3.up), shoulderToHipWorldRotation * Vector3.forward).eulerAngles.z;
            Quaternion shirtRotation = Quaternion.LookRotation(
                             leftShoulderPosition - rightShoulderPosition,
                             midShoulderPosition - midHipPosition
                        ) * Quaternion.AngleAxis(IsMirrored ? -90 : 90, Vector3.up);
            shirt.localRotation = shirtRotation;
            shirt.localScale = Vector3.one * Mathf.Max(Vector2.Distance(midShoulderPosition, midHipPosition), Vector2.Distance(leftShoulderPosition, rightShoulderPosition));
            midShoulderPosition.z = 0;
            shirt.localPosition = midShoulderPosition;
            // shirt.localRotation = Quaternion.Euler(shirtRotation.eulerAngles.x, shirtRotation.eulerAngles.y, Mathf.Sign(Mathf.Cos(shirtRotation.eulerAngles.y * Mathf.Deg2Rad)) * shirtZRotation);


        }
        public void UpdateShirt(IList<Landmark> poseWorldLandmarks, IList<NormalizedLandmark> poseLandmarks)
        {
            if (poseWorldLandmarks == null)
            {
                return;
            }

            if (poseLandmarks == null)
            {
                return;
            }

            var leftShoulderPosition = Draw(annotationLayer.rect, poseLandmarks[(int)PoseLandmark.LEFT_SHOULDER], true);
            var rightShoulderPosition = Draw(annotationLayer.rect, poseLandmarks[(int)PoseLandmark.RIGHT_SHOULDER], true);
            var lefHipPosition = Draw(annotationLayer.rect, poseLandmarks[(int)PoseLandmark.LEFT_HIP], true);

            var leftShoulderWorldPosition = Draw(annotationWorldLayer.rect, poseWorldLandmarks[(int)PoseLandmark.LEFT_SHOULDER], _scale);
            var rightShoulderWorldPosition = Draw(annotationWorldLayer.rect, poseWorldLandmarks[(int)PoseLandmark.RIGHT_SHOULDER], _scale);
            var lefHipWorldPosition = Draw(annotationWorldLayer.rect, poseWorldLandmarks[(int)PoseLandmark.LEFT_HIP], _scale);
            var rightHipWorldPosition = Draw(annotationWorldLayer.rect, poseWorldLandmarks[(int)PoseLandmark.RIGHT_HIP], _scale);

            var midShoulderWorldPosition = Vector3.Lerp(leftShoulderWorldPosition, rightShoulderWorldPosition, 0.5f);
            var midHipWorldPosition = Vector3.Lerp(lefHipWorldPosition, rightHipWorldPosition, 0.5f);

            var shoulderToHipWorldRotation = Quaternion.LookRotation(midShoulderWorldPosition - midHipWorldPosition);
            // var shoulderRotation = Quaternion.LookRotation(leftShoulderWorldPosition - rightShoulderWorldPosition).eulerAngles;

            // var ratio = Vector3.Distance(leftShoulderPosition, lefHipPosition) / Vector3.Distance(leftShoulderWorldPosition, lefHipWorldPosition);
            var midShoulderPosition = Vector3.Lerp(leftShoulderPosition, rightShoulderPosition, 0.5f);
            var shirtRotation = Quaternion.LookRotation(leftShoulderWorldPosition - rightShoulderWorldPosition);
            var shirtZRotation = Quaternion.FromToRotation(Vector3.up, shoulderToHipWorldRotation * Vector3.forward).eulerAngles.z;

            midShoulderPosition.z = 0;
            shirt.localPosition = midShoulderPosition;
            shirt.localRotation = Quaternion.Euler(shirtRotation.eulerAngles.x, shirtRotation.eulerAngles.y, Mathf.Sign(Mathf.Cos(shirtRotation.eulerAngles.y * Mathf.Deg2Rad)) * shirtZRotation);

            shirt.localScale = Vector3.one * Vector3.Distance(leftShoulderPosition, lefHipPosition);


            // var leftElbowPosition = Draw(annotationLayer.rect, poseLandmarks[(int)PoseLandmark.LEFT_ELBOW], false);
            // var rightElbowPosition = Draw(annotationLayer.rect, poseLandmarks[(int)PoseLandmark.RIGHT_ELBOW], false);

            // BodyPartMask(leftHand, leftShoulderPosition, leftElbowPosition, leftHandZReference);
            // BodyPartMask(rightHand, rightShoulderPosition, rightElbowPosition, rightHandZReference);
        }
        [System.Serializable] public struct BodyPartsLink { public PoseLandmark from, to; public Transform reference; }
        [SerializeField]
        private BodyPartsLink[] bodyPartsLinks = new BodyPartsLink[] {
            new BodyPartsLink {from=PoseLandmark.RIGHT_SHOULDER,to=PoseLandmark.RIGHT_ELBOW},
            new BodyPartsLink {from=PoseLandmark.RIGHT_ELBOW,to=PoseLandmark.RIGHT_WRIST},
            new BodyPartsLink {from=PoseLandmark.RIGHT_WRIST,to=PoseLandmark.RIGHT_INDEX},
            new BodyPartsLink {from=PoseLandmark.LEFT_SHOULDER,to=PoseLandmark.LEFT_ELBOW},
            new BodyPartsLink {from=PoseLandmark.LEFT_ELBOW,to=PoseLandmark.LEFT_WRIST},
            new BodyPartsLink {from=PoseLandmark.LEFT_WRIST,to=PoseLandmark.LEFT_INDEX},
            new BodyPartsLink {from=PoseLandmark.RIGHT_SHOULDER,to=PoseLandmark.LEFT_SHOULDER},
            new BodyPartsLink {from=PoseLandmark.RIGHT_SHOULDER,to=PoseLandmark.RIGHT_HIP},
            new BodyPartsLink {from=PoseLandmark.LEFT_SHOULDER,to=PoseLandmark.LEFT_HIP},
            new BodyPartsLink {from=PoseLandmark.RIGHT_HIP,to=PoseLandmark.LEFT_HIP},
            new BodyPartsLink {from=PoseLandmark.RIGHT_HIP,to=PoseLandmark.RIGHT_KNEE},
            new BodyPartsLink {from=PoseLandmark.RIGHT_KNEE,to=PoseLandmark.RIGHT_ANKLE},
            new BodyPartsLink {from=PoseLandmark.RIGHT_ANKLE,to=PoseLandmark.RIGHT_HEEL},
            new BodyPartsLink {from=PoseLandmark.RIGHT_HEEL,to=PoseLandmark.RIGHT_FOOT_INDEX},
            new BodyPartsLink {from=PoseLandmark.LEFT_HIP,to=PoseLandmark.LEFT_KNEE},
            new BodyPartsLink {from=PoseLandmark.LEFT_KNEE,to=PoseLandmark.LEFT_ANKLE},
            new BodyPartsLink {from=PoseLandmark.LEFT_ANKLE,to=PoseLandmark.LEFT_HEEL},
            new BodyPartsLink {from=PoseLandmark.LEFT_HEEL,to=PoseLandmark.LEFT_FOOT_INDEX}
        };


        public void BodyMask()
        {
            if (landmarksReferences == null)
            {
                return;
            }
            if (bodyPartMasks == null || bodyPartMasks.Length != bodyPartsLinks.Length)
            {
                bodyPartMasks = new Transform[bodyPartsLinks.Length];
                var parent = new GameObject("bodyPartMasks").transform;
                parent.parent = this.transform;
                parent.localPosition = Vector3.zero;
                parent.localRotation = Quaternion.identity;
                parent.localScale = Vector3.one;

                for (int i = 0; i < bodyPartsLinks.Length; i++)
                {
                    bodyPartMasks[i] = Instantiate(bodyPartPrefab, parent);
                    bodyPartMasks[i].name = $"{bodyPartsLinks[i].from} to {bodyPartsLinks[i].to}";
                    bodyPartMasks[i].gameObject.SetActive(false);
                }
            }
            var center = annotationWorldLayer.position;
            for (int i = 0; i < bodyPartsLinks.Length; i++)
            {
                var from = landmarksReferences[(int)bodyPartsLinks[i].from].localPosition;
                var to = landmarksReferences[(int)bodyPartsLinks[i].to].localPosition;

                BodyPartMask(bodyPartMasks[i], from, to);
            }
        }
        public void BodyMask(IList<Landmark> poseWorldLandmarks, IList<NormalizedLandmark> poseLandmarks)
        {
            if (poseLandmarks == null)
            {
                return;
            }
            if (bodyPartMasks == null || bodyPartMasks.Length != bodyPartsLinks.Length)
            {
                bodyPartMasks = new Transform[bodyPartsLinks.Length];

                for (int i = 0; i < bodyPartsLinks.Length; i++)
                {
                    bodyPartMasks[i] = Instantiate(bodyPartPrefab, this.transform);
                    bodyPartMasks[i].name = $"{bodyPartsLinks[i].from} to {bodyPartsLinks[i].to}";
                }
            }
            var center = annotationWorldLayer.position;
            for (int i = 0; i < bodyPartsLinks.Length; i++)
            {
                var from = Draw(annotationLayer.rect, poseLandmarks[(int)bodyPartsLinks[i].from], true);
                var to = Draw(annotationLayer.rect, poseLandmarks[(int)bodyPartsLinks[i].to], true);
                var fromWorld = Draw(annotationWorldLayer.rect, poseWorldLandmarks[(int)bodyPartsLinks[i].from], _scale);
                var toWorld = Draw(annotationWorldLayer.rect, poseWorldLandmarks[(int)bodyPartsLinks[i].to], _scale);
                BodyPartMask(bodyPartMasks[i], from, to, fromWorld, toWorld, center);
            }
        }

        public void BodyPartMask(Transform part, Vector3 from, Vector3 to, Vector3 fromWorld, Vector3 toWorld, Vector3 center)
        {
            from.z = ((fromWorld.z) / fromWorld.x) * from.x;
            part.localPosition = from;
            // part.position = !usePositionReference ? new Vector3(part.position.x, part.position.y, zReference ? zReference.position.z : part.position.z) : new Vector3(zReference.position.x, zReference.position.y, zReference.position.z);

            var landmarkPosition = new Vector3(from.x, from.y, from.z);
            var cameraRelativePosition = Camera.main.transform.TransformPoint(landmarkPosition);
            var depth = cameraRelativePosition.z;
            part.position = new Vector3(part.position.x, part.position.y, depth);
            float distance = Vector3.Distance(new Vector3(from.x, from.y, from.z), new Vector3(to.x, to.y, to.z));
            part.localScale = new Vector3(distance / 4f, distance / 2f, distance / 4f);
            Vector3 direction = GetDirection(fromWorld, toWorld);
            part.localRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f);
        }
        public void BodyPartMask(Transform part, Vector3 from, Vector3 to, Transform zReference = null, bool usePositionReference = false)
        {
            if (!zReference && usePositionReference)
            {
                Debug.LogError($"Can not use {nameof(usePositionReference)} and {nameof(zReference)} is null.");
                return;
            }
            // from.z = 0;
            part.localPosition = from;
            // part.position = !usePositionReference ? new Vector3(part.position.x, part.position.y, zReference ? zReference.position.z : part.position.z) : new Vector3(zReference.position.x, zReference.position.y, zReference.position.z);


            float distance = Vector3.Distance(from, to);
            part.localScale = new Vector3(distance / 4f, distance / 2f, distance / 4f);
            Vector3 direction = GetDirection(from, to);
            part.localRotation = Quaternion.LookRotation(direction, GetDirection(Vector3.zero, Vector3.up)) * Quaternion.Euler(90f, 0f, 0f);
        }
        public void BodyPartMask2D(Transform part, Vector3 from, Vector3 to, Transform zReference = null, bool usePositionReference = false)
        {
            if (!zReference && usePositionReference)
            {
                Debug.LogError($"Can not use {nameof(usePositionReference)} and {nameof(zReference)} is null.");
                return;
            }
            float distance = Vector2.Distance(from, to);
            from.z = 0;
            to.z = 0;
            part.localPosition = from;
            // part.position = !usePositionReference ? new Vector3(part.position.x, part.position.y, zReference ? zReference.position.z : part.position.z) : new Vector3(zReference.position.x, zReference.position.y, zReference.position.z);


            part.localScale = new Vector3(distance / 4f, distance / 2f, distance / 4f);
            Vector3 direction = GetDirection(from, to);
            part.localRotation = Quaternion.LookRotation(direction, GetDirection(Vector3.zero, Vector3.up)) * Quaternion.Euler(90f, 0f, 0f);
        }
        Camera _camera;

        private Transform[] references;
        private Transform hip;
        public void UpdateAvatar(IList<NormalizedLandmark> poseLandmarks)
        {
            if (poseLandmarks == null)
            {
                return;
            }
            if (references == null || references.Length == 0)
            {
                references = new Transform[poseLandmarks.Count];
                hip = (new GameObject("hip")).transform;
                hip.parent = this.transform;
                for (int i = 0; i < poseLandmarks.Count; i++)
                {
                    references[i] = (new GameObject(i.ToString())).transform;
                    references[i].parent = this.transform;
                }
            }
            body.transform.localScale = Vector3.one * scaleMultiplier * Vector3.Distance(Draw(annotationLayer.rect, poseLandmarks[(int)PoseLandmark.LEFT_SHOULDER], false), Draw(annotationLayer.rect, poseLandmarks[(int)PoseLandmark.LEFT_HIP], false));
            hip.transform.localPosition = Vector3.Lerp(Draw(annotationLayer.rect, poseLandmarks[(int)PoseLandmark.LEFT_HIP], false), Draw(annotationLayer.rect, poseLandmarks[(int)PoseLandmark.RIGHT_HIP], false), 0.5f);
            body.transform.position = hip.position;
            for (int i = 0; i < bodyParts.Length; i++)
            {
                references[i].localPosition = Draw(annotationLayer.rect, poseLandmarks[i], false);
                if (!bodyParts[i]) continue;
                bodyParts[i].position = references[i].position;
            }
        }
        public void UpdateAvatar(IList<Landmark> poseLandmarks)
        {
            if (poseLandmarks == null)
            {
                return;
            }
            if (references == null || references.Length == 0)
            {
                references = new Transform[poseLandmarks.Count];
                hip = (new GameObject("hip")).transform;
                hip.parent = this.transform;
                for (int i = poseLandmarks.Count - 1; i >= 0; i--)
                {
                    references[i] = (new GameObject(i.ToString())).transform;
                    references[i].parent = this.transform;
                }
            }
            body.transform.localScale = Vector3.one * scaleMultiplier * Vector3.Distance(Draw(annotationLayer.rect, poseLandmarks[(int)PoseLandmark.LEFT_SHOULDER], _scale), Draw(annotationLayer.rect, poseLandmarks[(int)PoseLandmark.LEFT_HIP], _scale));
            hip.localPosition = Vector3.Lerp(Draw(annotationLayer.rect, poseLandmarks[(int)PoseLandmark.LEFT_HIP], _scale), Draw(annotationLayer.rect, poseLandmarks[(int)PoseLandmark.RIGHT_HIP], _scale), 0.5f);
            body.transform.position = hip.position;
            // body.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
            references[(int)PoseLandmark.RIGHT_WRIST].localPosition = Draw(annotationLayer.rect, poseLandmarks[(int)PoseLandmark.RIGHT_WRIST], _scale);
            references[(int)PoseLandmark.LEFT_WRIST].localPosition = Draw(annotationLayer.rect, poseLandmarks[(int)PoseLandmark.LEFT_WRIST], _scale);
            // body.SetIKRotation(AvatarIKGoal.RightHand, rightHandObj.localRotation);
            // for (int i = 0; i < bodyParts.Length; i++)
            // {
            //     references[i].localPosition = Draw(annotationLayer.rect, poseLandmarks[i], _scale);
            //     if (!bodyParts[i]) continue;
            //     bodyParts[i].position = references[i].position;
            // }
        }

        void OnAnimatorIK()
        {
            if (references == null || references.Length == 0)
            {
                return;
            }
            body.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            body.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            body.SetIKPosition(AvatarIKGoal.RightHand, references[(int)PoseLandmark.RIGHT_WRIST].position);
            body.SetIKPosition(AvatarIKGoal.LeftHand, references[(int)PoseLandmark.LEFT_WRIST].position);
        }

        [SerializeField] private Vector3 _scale = new Vector3(50, 50, 50);

        public Vector3 Draw(UnityEngine.Rect screenRect, Landmark target, Vector3 scale, bool visualizeZ = true)
        {

            var position = screenRect.GetPoint(target, scale, RotationAngle, IsMirrored);
            if (!visualizeZ)
            {
                position.z = 0.0f;
            }
            return position;

        }

        public Vector3 Draw(UnityEngine.Rect screenRect, NormalizedLandmark target, bool visualizeZ = true)
        {
            var position = screenRect.GetPoint(target, RotationAngle, IsMirrored);
            if (!visualizeZ)
            {
                position.z = 0.0f;
            }
            return position;
        }


        [SerializeField] private RectTransform annotationLayer;
        [SerializeField] private RectTransform annotationWorldLayer;
        private bool isInverted;
        private bool isXReversed;
        private bool isYReversed;

        public void SetupAnnotationController(ImageSource imageSource, InferenceMode inferenceMode, bool expectedToBeMirrored = false)
        {
            IsMirrored = expectedToBeMirrored ^ imageSource.isHorizontallyFlipped ^ imageSource.isFrontFacing;
            RotationAngle = imageSource.rotation.Reverse();
            this.inferenceMode = inferenceMode;
        }


        private bool isMirrored;
        private RotationAngle rotationAngle = RotationAngle.Rotation0;

        // public bool IsMirrored
        // {
        //     get => isMirrored;
        //     set
        //     {
        //         if (isMirrored != value)
        //         {
        //             isMirrored = value;
        //         }
        //     }
        // }

        // public RotationAngle RotationAngle
        // {
        //     get => rotationAngle;
        //     set
        //     {
        //         if (rotationAngle != value)
        //         {
        //             rotationAngle = value;
        //         }
        //     }
        // }

        public bool IsMirrored { get => isMirrored; set => isMirrored = value; }
        public RotationAngle RotationAngle { get => rotationAngle; set => rotationAngle = value; }

        public enum HandLandmark
        {
            WRIST = 0,
            THUMB_CMC = 1,
            THUMB_MCP = 2,
            THUMB_IP = 3,
            THUMB_TIP = 4,
            INDEX_FINGER_MCP = 5,
            INDEX_FINGER_PIP = 6,
            INDEX_FINGER_DIP = 7,
            INDEX_FINGER_TIP = 8,
            MIDDLE_FINGER_MCP = 9,
            MIDDLE_FINGER_PIP = 10,
            MIDDLE_FINGER_DIP = 11,
            MIDDLE_FINGER_TIP = 12,
            RING_FINGER_MCP = 13,
            RING_FINGER_PIP = 14,
            RING_FINGER_DIP = 15,
            RING_FINGER_TIP = 16,
            PINKY_MCP = 17,
            PINKY_PIP = 18,
            PINKY_DIP = 19,
            PINKY_TIP = 20
        }

        public void UpdateHands(NormalizedLandmarkList leftHandLandmarks, NormalizedLandmarkList rightHandLandmarks)
        {


            if (leftHandLandmarks == null || leftHandLandmarks.Landmark.Count == 0)
            {
                leftHand.gameObject.SetActive(false);
                leftFinger.gameObject.SetActive(false);
            }
            else
            {
                Transform elbow = null;
                Transform wrist = null;
                if ((!isLandmarksReferencesUpdated || landmarksReferences == null || landmarksReferences.Length == 0))
                {
                    leftWrist.localRotation = Quaternion.Euler(25, 180, IsMirrored ? 30 : -30);
                }
                else
                {
                    elbow = landmarksReferences[(int)PoseLandmark.LEFT_ELBOW];
                    wrist = landmarksReferences[(int)PoseLandmark.LEFT_WRIST];
                    leftWrist.localRotation = Quaternion.Euler(0, 180, 90);
                }
                UpdateHandLandmarks(leftHandLandmarks?.Landmark, ref leftHandsLandmarksReferences, wrist);
                UpdateHand(leftHandLandmarks?.Landmark, leftHand, elbow);
                UpdateFinger(leftHandLandmarks?.Landmark, leftFinger);

            }

            if (rightHandLandmarks == null || rightHandLandmarks.Landmark.Count == 0)
            {
                rightHand.gameObject.SetActive(false);
                rightFinger.gameObject.SetActive(false);
            }
            else
            {
                Transform elbow = null;
                Transform wrist = null;
                if ((!isLandmarksReferencesUpdated || landmarksReferences == null || landmarksReferences.Length == 0))
                {
                    rightWrist.localRotation = Quaternion.Euler(25, 0, IsMirrored ? -30 : 30);
                }
                else
                {
                    elbow = landmarksReferences[(int)PoseLandmark.RIGHT_ELBOW];
                    wrist = landmarksReferences[(int)PoseLandmark.RIGHT_WRIST];
                    rightWrist.localRotation = Quaternion.Euler(0, 0, 90);
                }
                UpdateHandLandmarks(rightHandLandmarks?.Landmark, ref rightHandsLandmarksReferences, wrist);
                UpdateHand(rightHandLandmarks?.Landmark, rightHand, elbow);
                UpdateFinger(rightHandLandmarks?.Landmark, rightFinger);
            }
            isLandmarksReferencesUpdated = false;
        }
        Transform[] handLandmarksReferences;
        public void UpdateWorldHands(LandmarkList leftHandWorldLandmarks, LandmarkList rightHandWorldLandmarks)
        {
            return;

            var landmarks = leftHandWorldLandmarks?.Landmark;
            if (landmarks == null) return;

            if (handLandmarksReferences == null
                    || handLandmarksReferences.Length != landmarks?.Count)
            {
                if (handLandmarksReferences?.Length != landmarks.Count)
                {
                    handLandmarksReferences = new Transform[landmarks.Count];
                    var parent = new GameObject("HandWorldLandmarksReferences").transform;
                    parent.parent = this.transform;
                    parent.localPosition = Vector3.zero;
                    parent.localRotation = Quaternion.identity;
                    parent.localScale = Vector3.one;
                    // parent.gameObject.SetActive(false);
                    for (int i = 0; i < landmarks.Count; i++)
                    {
                        handLandmarksReferences[i] = Instantiate(handLandmark, parent);
                        handLandmarksReferences[i].name = i.ToString();
                    }
                }
            }
            for (int i = 0; i < landmarks.Count; i += 1)
            {
                var position = annotationWorldLayer.rect.GetPoint(landmarks[i], Vector3.one * 100, RotationAngle, !IsMirrored);
                // var positionPres = annotationLayer.rect.GetPoint(landmarks[i > 0 ? i - 1 : i + 1], Vector3.one*50, RotationAngle, !IsMirrored);
                handLandmarksReferences[i].localScale = Vector3.one * 1;

                // position.z = 0;
                handLandmarksReferences[i].localPosition = position;
                handLandmarksReferences[i].gameObject.SetActive(true);
            }
        }
        [SerializeField] bool useScaleAsZPosition;
        private void UpdateHandLandmarks(IList<NormalizedLandmark> landmarks, ref Transform[] handLandmarksReferences, Transform wrist)
        {
            if (landmarks == null) return;

            if (handLandmarksReferences == null
                    || handLandmarksReferences.Length != landmarks.Count)
            {
                if (handLandmarksReferences?.Length != landmarks.Count)
                {
                    handLandmarksReferences = new Transform[landmarks.Count];
                    var parent = new GameObject("HandLandmarksReferences").transform;
                    parent.parent = this.transform;
                    parent.localPosition = Vector3.zero;
                    parent.localRotation = Quaternion.identity;
                    parent.localScale = Vector3.one;
                    // parent.gameObject.SetActive(false);
                    for (int i = 0; i < landmarks.Count; i++)
                    {
                        handLandmarksReferences[i] = Instantiate(handLandmark, parent);
                        handLandmarksReferences[i].name = i.ToString();
                    }
                }
            }
            for (int i = 0; i < landmarks.Count; i += 4)
            {
                var position = annotationLayer.rect.GetPoint(landmarks[i], RotationAngle, !IsMirrored);
                var positionPres = annotationLayer.rect.GetPoint(landmarks[i > 0 ? i - 1 : i + 1], RotationAngle, !IsMirrored);
                handLandmarksReferences[i].localScale = Vector3.one * 50;//Vector3.one * Vector3.Distance(position, positionPres);//

                position.z += useScaleAsZPosition ? -Vector3.Distance(position, positionPres) : wrist ? wrist.localPosition.z : 0;
                handLandmarksReferences[i].localPosition = position;
                handLandmarksReferences[i].gameObject.SetActive(true);
            }
        }

        public void UpdateHand(IList<NormalizedLandmark> landmarks, Transform hand, Transform elbow)
        {
            if (landmarks == null || landmarks == null || landmarks.Count == 0)
            {
                hand.gameObject.SetActive(false);
                return;
            }

            var indexBase = annotationLayer.rect.GetPoint(landmarks[(int)HandLandmark.INDEX_FINGER_MCP], RotationAngle, !IsMirrored);//Draw(annotationLayer.rect, landmarks[(int)HandLandmark.INDEX_FINGER_MCP], true);
            var pinkyBase = annotationLayer.rect.GetPoint(landmarks[(int)HandLandmark.PINKY_MCP], RotationAngle, !IsMirrored);//Draw(annotationLayer.rect, landmarks[(int)HandLandmark.PINKY_MCP], true);
            var wrist = annotationLayer.rect.GetPoint(landmarks[(int)HandLandmark.WRIST], RotationAngle, !IsMirrored);//Draw(annotationLayer.rect, landmarks[(int)HandLandmark.WRIST], true);
            var middleFinger = annotationLayer.rect.GetPoint(landmarks[(int)HandLandmark.MIDDLE_FINGER_MCP], RotationAngle, !IsMirrored);//Draw(annotationLayer.rect, landmarks[(int)HandLandmark.MIDDLE_FINGER_MCP], true);
            var thumbBase = annotationLayer.rect.GetPoint(landmarks[(int)HandLandmark.THUMB_CMC], RotationAngle, !IsMirrored);
            var thumbFinger = annotationLayer.rect.GetPoint(landmarks[(int)HandLandmark.THUMB_MCP], RotationAngle, !IsMirrored);
            var ringBase = annotationLayer.rect.GetPoint(landmarks[(int)HandLandmark.RING_FINGER_MCP], RotationAngle, !IsMirrored);
            // var thumbDirection = GetDirection(pinkyBase, indexBase).normalized;
            // var palmDirection = (middleFinger - wrist).normalized;

            // var normal = Vector3.Cross(thumbDirection, palmDirection);

            hand.localScale = (Vector3.Distance(indexBase, wrist) +
                                Vector3.Distance(pinkyBase, wrist) +
                                Vector3.Distance(middleFinger, wrist) +
                                Vector3.Distance(thumbBase, wrist) +
                                Vector3.Distance(ringBase, wrist)
                                ) / 5.0f * Vector3.one;
            // elbowPosition.z = 0;

            // Vector3 xDir = thumbDirection;
            // Vector3 yDir = GetDirection(wrist, elbowPosition);
            // Vector3 zDir = Vector3.Cross(xDir, yDir).normalized;
            // yDir = Vector3.Cross(zDir, xDir).normalized;

            // Quaternion handRotation = Quaternion.LookRotation(zDir, yDir);
            // hand.localRotation = handRotation;
            Quaternion wristRotation;
            if (elbow)
            {
                wristRotation = Quaternion.LookRotation(
                                     elbow.localPosition - wrist,
                                     thumbBase - wrist
                                );
                var position = Vector3.Lerp(elbow.localPosition, wrist, 0.8f);
                position.z = 0;
                hand.localPosition = position;
            }
            else
            {
                wristRotation = Quaternion.LookRotation(
                    thumbBase - wrist,//GetDirection(wrist, thumbBase),//
                    thumbFinger - wrist//GetDirection(wrist, thumbFinger)//
               );
                wrist.z = 0;
                hand.localPosition = wrist;
            }

            hand.localRotation = wristRotation * Quaternion.AngleAxis(IsMirrored ? -90 : 90, Vector3.up);
            hand.gameObject.SetActive(true);
        }

        public void UpdateFinger(IList<NormalizedLandmark> landmarks, Transform finger)
        {
            if (landmarks == null || landmarks == null || landmarks.Count == 0)
            {
                finger.gameObject.SetActive(false);
                return;
            }
            var ringBase = annotationLayer.rect.GetPoint(landmarks[(int)HandLandmark.RING_FINGER_MCP], RotationAngle, !IsMirrored);//Draw(annotationLayer.rect, landmarks[(int)HandLandmark.RING_FINGER_MCP], true);
            var ringPip = annotationLayer.rect.GetPoint(landmarks[(int)HandLandmark.RING_FINGER_PIP], RotationAngle, !IsMirrored);//Draw(annotationLayer.rect, landmarks[(int)HandLandmark.RING_FINGER_PIP], true);

            var midRing = Vector3.Lerp(ringBase, ringPip, 0.5f);
            var rotation = Quaternion.LookRotation(GetDirection(ringBase, ringPip));
            midRing.z = 0;
            finger.localPosition = midRing;
            finger.localRotation = rotation;
            finger.localScale = Vector3.one * Vector3.Distance(ringBase, ringPip);
            finger.gameObject.SetActive(true);
        }

        public Vector3 ConvertToVector3(NormalizedLandmark landmark)
        {
            return new Vector3(landmark.X, landmark.Y, landmark.Z);
        }


    }
}