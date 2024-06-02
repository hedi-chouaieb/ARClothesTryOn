// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using Mediapipe.Unity;
using UnityEngine;
using Hedi.me.Tools;

using Logger = Mediapipe.Unity.Logger;

namespace ARClothesTryOn
{
    public abstract class ImageSourceSolution<T> : Solution where T : GraphRunner
    {
        [SerializeField] protected Mediapipe.Unity.Screen screen;
        [SerializeField] protected T graphRunner;
        [SerializeField] protected TextureFramePool textureFramePool;
        [SerializeField] private bool isRunning;
        [SerializeField] private IntEntityData runningSolutions;
        [SerializeField] private IntEntityData idSolutions;

        private Coroutine _coroutine;
        private int _idSolution = 0;
        private bool firstRun = true;

        public bool IsRunning
        {
            get => isRunning;
            set
            {
                isRunning = value;
                runningSolutions.Value += (isRunning) ? 1 : -1;
                if (runningSolutions.Value < 0) runningSolutions.Value = 0;
                if (firstRun) firstRun = false;
            }
        }


        public RunningMode runningMode;

        public long timeoutMillisec
        {
            get => graphRunner.timeoutMillisec;
            set => graphRunner.timeoutMillisec = value;
        }

        private void OnEnable()
        {
            idSolutions.Value++;
            _idSolution = idSolutions.Value;
        }

        private void OnDisable()
        {
            idSolutions.Value--;
            _idSolution = 0;
        }

        public override void Play()
        {
            if (_coroutine != null)
            {
                Stop();
            }
            base.Play();
            _coroutine = StartCoroutine(Run());
        }

        public override void Pause()
        {
            base.Pause();
            ImageSourceProvider.ImageSource.Pause();
        }

        public override void Resume()
        {
            base.Resume();
            var _ = StartCoroutine(ImageSourceProvider.ImageSource.Resume());
        }

        public override void Stop()
        {
            base.Stop();
            StopCoroutine(_coroutine);
            ImageSourceProvider.ImageSource.Stop();
            graphRunner.Stop();
        }

        private IEnumerator Run()
        {
            var graphInitRequest = graphRunner.WaitForInit(runningMode);
            var imageSource = ImageSourceProvider.ImageSource;

            yield return imageSource.Play();

            if (!imageSource.isPrepared)
            {
                Logger.LogError(TAG, "Failed to start ImageSource, exiting...");
                yield break;
            }

            // Use RGBA32 as the input format.
            // TODO: When using GpuBuffer, MediaPipe assumes that the input format is BGRA, so the following code must be fixed.
            textureFramePool.ResizeTexture(imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32);
            SetupScreen(imageSource);

            yield return graphInitRequest;
            if (graphInitRequest.isError)
            {
                Logger.LogError(TAG, graphInitRequest.error);
                yield break;
            }

            OnStartRun();
            graphRunner.StartRun(imageSource);

            var waitWhilePausing = new WaitWhile(() => isPaused);
            var waitWhileNotRunning = new WaitWhile(() => !IsRunning);

            while (true)
            {
                if (isPaused)
                {
                    yield return waitWhilePausing;
                }
                // if (!IsRunning)
                // {
                //     yield return waitWhileNotRunning;
                // }
                if (IsRunning)
                {
                    if (!textureFramePool.TryGetTextureFrame(out var textureFrame))
                    {
                        yield return new WaitForEndOfFrame();
                        continue;
                    }

                    // Copy current image to TextureFrame

                    ReadFromImageSource(imageSource, textureFrame);
                    AddTextureFrameToInputStream(textureFrame);
                    yield return new WaitForEndOfFrame();

                    if (runningMode.IsSynchronous())
                    {
                        RenderCurrentFrame(textureFrame);
                        yield return WaitForNextValue();
                    }
                    if (firstRun)
                        IsRunning = false;
                }
                else if (runningSolutions.Value == 0 && _idSolution == 1)
                {
                    if (!textureFramePool.TryGetTextureFrame(out var textureFrame))
                    {
                        yield return new WaitForEndOfFrame();
                        continue;
                    }

                    ReadFromImageSource(imageSource, textureFrame);
                    RenderCurrentFrame(textureFrame);
                    textureFrame.Release();
                }
                yield return new WaitForEndOfFrame();
            }
        }

        protected virtual void SetupScreen(ImageSource imageSource)
        {
            // NOTE: The screen will be resized later, keeping the aspect ratio.
            screen.Initialize(imageSource);
        }

        protected virtual void RenderCurrentFrame(TextureFrame textureFrame)
        {
            screen.ReadSync(textureFrame);
        }

        protected abstract void OnStartRun();

        protected abstract void AddTextureFrameToInputStream(TextureFrame textureFrame);

        protected abstract IEnumerator WaitForNextValue();
    }
}
