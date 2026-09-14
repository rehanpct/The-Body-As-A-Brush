// Copyright (c) 2023 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections;
using Mediapipe.Tasks.Vision.HandLandmarker;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mediapipe.Unity.Sample.HandLandmarkDetection
{
  public class HandLandmarkerRunner : VisionTaskApiRunner<HandLandmarker>
  {
    // Sends detected hand results to other scripts.
    public event Action<HandLandmarkerResult> OnResultUpdated;

    [SerializeField]
    private HandLandmarkerResultAnnotationController
      _handLandmarkerResultAnnotationController;

    private Experimental.TextureFramePool _textureFramePool;

    public readonly HandLandmarkDetectionConfig config =
      new HandLandmarkDetectionConfig();

    [Header("Performance")]
    [SerializeField]
    private float targetInferenceFPS = 20f;

    private float inferenceInterval;

    public override void Stop()
    {
      base.Stop();

      _textureFramePool?.Dispose();
      _textureFramePool = null;
    }

    protected override IEnumerator Run()
    {
      Debug.Log("====================================");
      Debug.Log("HAND TRACKER CONFIGURATION");

      Debug.Log($"Delegate = {config.Delegate}");
      Debug.Log($"Image Read Mode = {config.ImageReadMode}");
      Debug.Log($"Running Mode = {config.RunningMode}");
      Debug.Log($"Num Hands = {config.NumHands}");
      Debug.Log(
        $"Min Hand Detection Confidence = " +
        $"{config.MinHandDetectionConfidence}"
      );
      Debug.Log(
        $"Min Hand Presence Confidence = " +
        $"{config.MinHandPresenceConfidence}"
      );
      Debug.Log(
        $"Min Tracking Confidence = " +
        $"{config.MinTrackingConfidence}"
      );

      Debug.Log("====================================");

      inferenceInterval =
        1f / Mathf.Max(1f, targetInferenceFPS);

      yield return AssetLoader.PrepareAssetAsync(
        config.ModelPath
      );

      var options =
        config.GetHandLandmarkerOptions(
          config.RunningMode ==
          Tasks.Vision.Core.RunningMode.LIVE_STREAM
            ? OnHandLandmarkDetectionOutput
            : null
        );

      taskApi =
        HandLandmarker.CreateFromOptions(
          options,
          GpuManager.GpuResources
        );

      var imageSource =
        ImageSourceProvider.ImageSource;

      yield return imageSource.Play();

      if (!imageSource.isPrepared)
      {
        Debug.LogError(
          "Failed to start ImageSource, exiting..."
        );

        yield break;
      }

      Debug.Log(
        $"Webcam Resolution = " +
        $"{imageSource.textureWidth} x " +
        $"{imageSource.textureHeight}"
      );

      _textureFramePool =
        new Experimental.TextureFramePool(
          imageSource.textureWidth,
          imageSource.textureHeight,
          TextureFormat.RGBA32,
          4
        );

      screen.Initialize(imageSource);

      /*
       * We intentionally do not continuously draw
       * MediaPipe's annotation overlay.
       *
       * Your final project hides the webcam anyway,
       * so drawing the landmark overlay is unnecessary
       * processing.
       */

      var transformationOptions =
        imageSource.GetTransformationOptions();

      var flipHorizontally =
        transformationOptions.flipHorizontally;

      var flipVertically =
        transformationOptions.flipVertically;

      var imageProcessingOptions =
        new Tasks.Vision.Core.ImageProcessingOptions(
          rotationDegrees:
            (int)transformationOptions.rotationAngle
        );

      AsyncGPUReadbackRequest req = default;

      var waitUntilReqDone =
        new WaitUntil(() => req.done);

      var waitForEndOfFrame =
        new WaitForEndOfFrame();

      var result =
        HandLandmarkerResult.Alloc(
          options.numHands
        );

      /*
       * GPU image input is not available on your
       * current Windows configuration.
       *
       * Therefore we use CPU input safely.
       */
      var canUseGpuImage =
        SystemInfo.graphicsDeviceType ==
        GraphicsDeviceType.OpenGLES3 &&
        GpuManager.GpuResources != null;

      using var glContext =
        canUseGpuImage
          ? GpuManager.GetGlContext()
          : null;

      float nextInferenceTime = 0f;

      while (true)
      {
        if (isPaused)
        {
          yield return new WaitWhile(
            () => isPaused
          );
        }

        /*
         * Limit MediaPipe processing rate.
         *
         * Webcam may provide 30 FPS,
         * but CPU hand tracking does not need
         * to process every frame.
         */
        if (
          Time.realtimeSinceStartup <
          nextInferenceTime
        )
        {
          yield return null;
          continue;
        }

        nextInferenceTime =
          Time.realtimeSinceStartup +
          inferenceInterval;

        if (
          !_textureFramePool.TryGetTextureFrame(
            out var textureFrame
          )
        )
        {
          yield return null;
          continue;
        }

        Image image;

        switch (config.ImageReadMode)
        {
          case ImageReadMode.GPU:

            if (!canUseGpuImage)
            {
              textureFrame.Release();

              Debug.LogWarning(
                "GPU image mode is not supported. " +
                "Use CPU or CPUAsync."
              );

              yield return null;
              continue;
            }

            textureFrame.ReadTextureOnGPU(
              imageSource.GetCurrentTexture(),
              flipHorizontally,
              flipVertically
            );

            image =
              textureFrame.BuildGPUImage(
                glContext
              );

            yield return waitForEndOfFrame;

            break;

          case ImageReadMode.CPU:

            yield return waitForEndOfFrame;

            textureFrame.ReadTextureOnCPU(
              imageSource.GetCurrentTexture(),
              flipHorizontally,
              flipVertically
            );

            image =
              textureFrame.BuildCPUImage();

            textureFrame.Release();

            break;

          case ImageReadMode.CPUAsync:
          default:

            req =
              textureFrame.ReadTextureAsync(
                imageSource.GetCurrentTexture(),
                flipHorizontally,
                flipVertically
              );

            yield return waitUntilReqDone;

            /*
             * A webcam frame can occasionally fail.
             * Do not kill the tracking loop.
             */
            if (req.hasError)
            {
              textureFrame.Release();

              Debug.LogWarning(
                "MediaPipe skipped a webcam frame."
              );

              yield return null;
              continue;
            }

            image =
              textureFrame.BuildCPUImage();

            textureFrame.Release();

            break;
        }

        switch (taskApi.runningMode)
        {
          case Tasks.Vision.Core.RunningMode.IMAGE:

            if (
              taskApi.TryDetect(
                image,
                imageProcessingOptions,
                ref result
              )
            )
            {
              OnHandLandmarkDetectionOutput(
                result,
                image,
                GetCurrentTimestampMillisec()
              );
            }

            break;

          case Tasks.Vision.Core.RunningMode.VIDEO:

            if (
              taskApi.TryDetectForVideo(
                image,
                GetCurrentTimestampMillisec(),
                imageProcessingOptions,
                ref result
              )
            )
            {
              OnHandLandmarkDetectionOutput(
                result,
                image,
                GetCurrentTimestampMillisec()
              );
            }

            break;

          case Tasks.Vision.Core.RunningMode.LIVE_STREAM:

            taskApi.DetectAsync(
              image,
              GetCurrentTimestampMillisec(),
              imageProcessingOptions
            );

            break;
        }
      }
    }

    private void OnHandLandmarkDetectionOutput(
      HandLandmarkerResult result,
      Image image,
      long timestamp
    )
    {
      /*
       * Do NOT call Unity APIs here.
       *
       * This callback can occur on a MediaPipe
       * worker thread.
       */

      OnResultUpdated?.Invoke(result);
    }
  }
}