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
    public class ObjectronSolution : ImageSourceSolution<ObjectronGraph>
    {
        [SerializeField] private FrameAnnotationController _liftedObjectsAnnotationController;
        [SerializeField] private NormalizedRectListAnnotationController _multiBoxRectsAnnotationController;
        [SerializeField] private NormalizedLandmarkListAnnotationController _multiBoxLandmarksAnnotationController;
        [SerializeField] private BodyPoseTracker bodyPoseTracker;
        [SerializeField] private bool activateLiftedObjectsAnnotationController;
        [SerializeField] private bool activateMultiBoxRectsAnnotationController;
        [SerializeField] private bool activateMultiBoxLandmarksAnnotationController;


        public ObjectronGraph.Category category
        {
            get => graphRunner.category;
            set => graphRunner.category = value;
        }

        public int maxNumObjects
        {
            get => graphRunner.maxNumObjects;
            set => graphRunner.maxNumObjects = value;
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
                graphRunner.OnLiftedObjectsOutput += OnLiftedObjectsOutput;
                graphRunner.OnMultiBoxRectsOutput += OnMultiBoxRectsOutput;
                graphRunner.OnMultiBoxLandmarksOutput += OnMultiBoxLandmarksOutput;
            }

            var imageSource = ImageSourceProvider.ImageSource;
            SetupAnnotationController(_liftedObjectsAnnotationController, imageSource);
            _liftedObjectsAnnotationController.focalLength = graphRunner.focalLength;
            _liftedObjectsAnnotationController.principalPoint = graphRunner.principalPoint;

            SetupAnnotationController(_multiBoxRectsAnnotationController, imageSource);
            SetupAnnotationController(_multiBoxLandmarksAnnotationController, imageSource);
            bodyPoseTracker.SetupAnnotationController(imageSource, graphRunner.inferenceMode);
        }

        protected override void AddTextureFrameToInputStream(TextureFrame textureFrame)
        {
            graphRunner.AddTextureFrameToInputStream(textureFrame);
        }

        protected override IEnumerator WaitForNextValue()
        {
            FrameAnnotation liftedObjects = null;
            List<NormalizedRect> multiBoxRects = null;
            List<NormalizedLandmarkList> multiBoxLandmarks = null;

            if (runningMode == RunningMode.Sync)
            {
                var _ = graphRunner.TryGetNext(out liftedObjects, out multiBoxRects, out multiBoxLandmarks, true);
            }
            else if (runningMode == RunningMode.NonBlockingSync)
            {
                yield return new WaitUntil(() => graphRunner.TryGetNext(out liftedObjects, out multiBoxRects, out multiBoxLandmarks, false));
            }
            if (activateLiftedObjectsAnnotationController)
                _liftedObjectsAnnotationController.DrawNow(liftedObjects);
            if (activateMultiBoxRectsAnnotationController)
                _multiBoxRectsAnnotationController.DrawNow(multiBoxRects);
            if (activateMultiBoxLandmarksAnnotationController)
                _multiBoxLandmarksAnnotationController.DrawNow(multiBoxLandmarks);
            bodyPoseTracker.UpdateShoe(liftedObjects);
        }

        private void OnLiftedObjectsOutput(object stream, OutputEventArgs<FrameAnnotation> eventArgs)
        {
            _liftedObjectsAnnotationController.DrawLater(eventArgs.value);
        }

        private void OnMultiBoxRectsOutput(object stream, OutputEventArgs<List<NormalizedRect>> eventArgs)
        {
            _multiBoxRectsAnnotationController.DrawLater(eventArgs.value);
        }

        private void OnMultiBoxLandmarksOutput(object stream, OutputEventArgs<List<NormalizedLandmarkList>> eventArgs)
        {
            _multiBoxLandmarksAnnotationController.DrawLater(eventArgs.value);
        }
    }
}
