// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using System.Collections.Generic;
using Mediapipe;
using Mediapipe.Unity;
using UnityEngine;

namespace ARClothesTryOn
{
    public class HandTrackingSolution : ImageSourceSolution<HandTrackingGraph>
    {
        [SerializeField] private DetectionListAnnotationController _palmDetectionsAnnotationController;
        [SerializeField] private NormalizedRectListAnnotationController _handRectsFromPalmDetectionsAnnotationController;
        [SerializeField] private MultiHandLandmarkListAnnotationController _handLandmarksAnnotationController;
        [SerializeField] private NormalizedRectListAnnotationController _handRectsFromLandmarksAnnotationController;
        [SerializeField] private bool activatePalmDetectionsAnnotationController;
        [SerializeField] private bool activateHandRectsFromPalmDetectionsAnnotationController;
        [SerializeField] private bool activateHandLandmarksAnnotationController;
        [SerializeField] private bool activateHandRectsFromLandmarksAnnotationController;
        [SerializeField] private BodyPoseTracker bodyPoseTracker;

        public HandTrackingGraph.ModelComplexity modelComplexity
        {
            get => graphRunner.modelComplexity;
            set => graphRunner.modelComplexity = value;
        }

        public int maxNumHands
        {
            get => graphRunner.maxNumHands;
            set => graphRunner.maxNumHands = value;
        }

        public float minDetectionConfidence
        {
            get => graphRunner.minDetectionConfidence;
            set => graphRunner.minDetectionConfidence = value;
        }

        public float minTrackingConfidence
        {
            get => graphRunner.minTrackingConfidence;
            set => graphRunner.minTrackingConfidence = value;
        }

        protected override void OnStartRun()
        {
            if (!runningMode.IsSynchronous())
            {
                graphRunner.OnPalmDetectectionsOutput += OnPalmDetectionsOutput;
                graphRunner.OnHandRectsFromPalmDetectionsOutput += OnHandRectsFromPalmDetectionsOutput;
                graphRunner.OnHandLandmarksOutput += OnHandLandmarksOutput;
                // TODO: render HandWorldLandmarks annotations
                graphRunner.OnHandRectsFromLandmarksOutput += OnHandRectsFromLandmarksOutput;
                graphRunner.OnHandednessOutput += OnHandednessOutput;
            }

            var imageSource = ImageSourceProvider.ImageSource;
            SetupAnnotationController(_palmDetectionsAnnotationController, imageSource, true);
            SetupAnnotationController(_handRectsFromPalmDetectionsAnnotationController, imageSource, true);
            SetupAnnotationController(_handLandmarksAnnotationController, imageSource, true);
            SetupAnnotationController(_handRectsFromLandmarksAnnotationController, imageSource, true);
            bodyPoseTracker.SetupAnnotationController(imageSource, graphRunner.inferenceMode);
        }

        protected override void AddTextureFrameToInputStream(TextureFrame textureFrame)
        {
            graphRunner.AddTextureFrameToInputStream(textureFrame);
        }

        protected override IEnumerator WaitForNextValue()
        {
            List<Detection> palmDetections = null;
            List<NormalizedRect> handRectsFromPalmDetections = null;
            List<NormalizedLandmarkList> handLandmarks = null;
            List<LandmarkList> handWorldLandmarks = null;
            List<NormalizedRect> handRectsFromLandmarks = null;
            List<ClassificationList> handedness = null;

            if (runningMode == RunningMode.Sync)
            {
                var _ = graphRunner.TryGetNext(out palmDetections, out handRectsFromPalmDetections, out handLandmarks, out handWorldLandmarks, out handRectsFromLandmarks, out handedness, true);
            }
            else if (runningMode == RunningMode.NonBlockingSync)
            {
                yield return new WaitUntil(() => graphRunner.TryGetNext(out palmDetections, out handRectsFromPalmDetections, out handLandmarks, out handWorldLandmarks, out handRectsFromLandmarks, out handedness, false));
            }
            if (activatePalmDetectionsAnnotationController)
                _palmDetectionsAnnotationController.DrawNow(palmDetections);
            if (activateHandRectsFromPalmDetectionsAnnotationController)
                _handRectsFromPalmDetectionsAnnotationController.DrawNow(handRectsFromPalmDetections);
            if (activateHandLandmarksAnnotationController)
                _handLandmarksAnnotationController.DrawNow(handLandmarks, handedness);
            // TODO: render HandWorldLandmarks annotations
            if (activateHandRectsFromLandmarksAnnotationController)
                _handRectsFromLandmarksAnnotationController.DrawNow(handRectsFromLandmarks);
            DrawHands(handWorldLandmarks, handLandmarks, handedness);
        }

        private void DrawHands(IList<LandmarkList> handWorldLandmarks, IList<NormalizedLandmarkList> handLandmarkLists, IList<ClassificationList> handedness)
        {
            var count = handedness == null ? 0 : handedness.Count;
            NormalizedLandmarkList leftHandLandmarks = null;
            NormalizedLandmarkList rightHandLandmarks = null;
            LandmarkList leftHandWorldLandmarks = null;
            LandmarkList rightHandWorldLandmarks = null;
            for (var i = 0; i < Mathf.Min(count, 2); i++)
            {
                if (handedness == null || handedness.Count == 0 || handedness[i].Classification[0].Label == "Left")
                {
                    leftHandLandmarks = handLandmarkLists[i];
                    leftHandWorldLandmarks = handWorldLandmarks[i];
                }
                else if (handedness[i].Classification[0].Label == "Right")
                {
                    rightHandLandmarks = handLandmarkLists[i];
                    rightHandWorldLandmarks = handWorldLandmarks[i];
                }
            }
            bodyPoseTracker.UpdateHands(leftHandLandmarks, rightHandLandmarks);
            bodyPoseTracker.UpdateWorldHands(leftHandWorldLandmarks, rightHandWorldLandmarks);
        }

        private void OnPalmDetectionsOutput(object stream, OutputEventArgs<List<Detection>> eventArgs)
        {
            _palmDetectionsAnnotationController.DrawLater(eventArgs.value);
        }

        private void OnHandRectsFromPalmDetectionsOutput(object stream, OutputEventArgs<List<NormalizedRect>> eventArgs)
        {
            _handRectsFromPalmDetectionsAnnotationController.DrawLater(eventArgs.value);
        }

        private void OnHandLandmarksOutput(object stream, OutputEventArgs<List<NormalizedLandmarkList>> eventArgs)
        {
            _handLandmarksAnnotationController.DrawLater(eventArgs.value);
        }

        private void OnHandRectsFromLandmarksOutput(object stream, OutputEventArgs<List<NormalizedRect>> eventArgs)
        {
            _handRectsFromLandmarksAnnotationController.DrawLater(eventArgs.value);
        }

        private void OnHandednessOutput(object stream, OutputEventArgs<List<ClassificationList>> eventArgs)
        {
            _handLandmarksAnnotationController.DrawLater(eventArgs.value);
        }
    }
}
