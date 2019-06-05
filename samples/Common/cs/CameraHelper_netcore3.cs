// Copyright (C) Microsoft Corporation. All rights reserved.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;

namespace CameraHelper_NetCore3
{

    /// <summary>
    /// Helper class to initialize a basic camera pipeline.
    /// </summary>
    class CameraHelper
    {
        public delegate void NewFrameArrivedHandler(VideoFrame videoFrame);
        public delegate void CameraHelperFailedHandler(string message);

        private MediaFrameReader m_frameReader = null;
        private MediaCapture m_mediaCapture = null;
        private NewFrameArrivedHandler m_frameHandler = null;
        private CameraHelperFailedHandler m_failureHandler = null;
        private MediaCaptureSharingMode m_sharingMode = MediaCaptureSharingMode.ExclusiveControl;

        /// <summary>
        /// CameraHelper private constructor
        /// </summary>
        /// <param name="handler"></param>
        private CameraHelper(CameraHelperFailedHandler failureHandler, NewFrameArrivedHandler frameHandler)
        {
            m_failureHandler = failureHandler;
            m_frameHandler = frameHandler;
        }

        /// <summary>
        /// CameraHelper factory method that intializes the camera pipeline.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static async Task<CameraHelper> CreateCameraHelperAsync(CameraHelperFailedHandler failureHandler, NewFrameArrivedHandler frameHandler)
        {
            CameraHelper instance = new CameraHelper(failureHandler, frameHandler);

            await instance.InitializeAsync();

            return instance;
        }

        /// <summary>
        /// Initialize camera pipeline resources and register a callback for when new VideoFrames become available.
        /// </summary>
        /// <returns></returns>
        private async Task InitializeAsync()
        {
            // Initialize MediaCapture with default settings in video-only streaming mode. 
            // We first try to aquire exclusive sharing mode and if we fail, we then attempt again in shared mode 
            // so that multiple instances can access the camera concurrently
            m_mediaCapture = new MediaCapture();
            var mediaCaptureInistializationSettings = new MediaCaptureInitializationSettings()
            {
                StreamingCaptureMode = StreamingCaptureMode.Video,
                SharingMode = m_sharingMode
            };

            // Register a callback in case MediaCapture fails. This can happen for example if another app is using the camera and we can't get ExclusiveControl
            m_mediaCapture.Failed += MediaCapture_Failed;

            await m_mediaCapture.InitializeAsync(mediaCaptureInistializationSettings);

            // Get a list of available Frame source and iterate through them to find a video preview or 
            // a video record source with color images (and not IR, depth or other types)
            var selectedFrameSource = m_mediaCapture.FrameSources.FirstOrDefault(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoPreview
                                                                                        && source.Value.Info.SourceKind == MediaFrameSourceKind.Color).Value;
            if (selectedFrameSource == null)
            {
                selectedFrameSource = m_mediaCapture.FrameSources.FirstOrDefault(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoRecord
                                                                                           && source.Value.Info.SourceKind == MediaFrameSourceKind.Color).Value;
            }
            if (selectedFrameSource == null)
            {
                throw new Exception("No valid video frame sources were found with source type color.");
            }

            Console.WriteLine($"{selectedFrameSource.Info.DeviceInformation?.Name} | MediaStreamType: {selectedFrameSource.Info.MediaStreamType} MediaFrameSourceKind: {selectedFrameSource.Info.SourceKind}");

            // If initializing in ExclusiveControl mode, attempt to use a 15fps+ BGRA8 format natively from the camera.
            // If not, just use whatever format is already set.
            MediaFrameFormat selectedFormat = selectedFrameSource.CurrentFormat;
            if (m_sharingMode == MediaCaptureSharingMode.ExclusiveControl)
            {
                var mediaFrameFormats = selectedFrameSource.SupportedFormats.OrderByDescending((format) => format.VideoFormat.Width * format.VideoFormat.Height);
                selectedFormat = mediaFrameFormats.Where(
                format => format.FrameRate.Numerator / format.FrameRate.Denominator >= 15 // fps
                && string.Compare(format.Subtype, MediaEncodingSubtypes.Bgra8, true) == 0).FirstOrDefault();

                // If not possible, then try to use other supported format at 15fps+
                if (selectedFormat == null)
                {
                    selectedFormat = mediaFrameFormats.Where(
                        format => format.FrameRate.Numerator / format.FrameRate.Denominator >= 15 // fps
                        && (string.Compare(format.Subtype, MediaEncodingSubtypes.Nv12, true) == 0
                            || string.Compare(format.Subtype, MediaEncodingSubtypes.Yuy2, true) == 0
                            || string.Compare(format.Subtype, MediaEncodingSubtypes.Rgb32, true) == 0)).FirstOrDefault();
                }
                if (selectedFormat == null)
                {
                    throw (new Exception("No suitable media format found on the selected source"));
                }
                await selectedFrameSource.SetFormatAsync(selectedFormat);
                selectedFormat = selectedFrameSource.CurrentFormat;
                Console.WriteLine($"Attempting to set camera source to {selectedFormat.Subtype} : " +
                    $"{selectedFormat.VideoFormat.Width}x{selectedFormat.VideoFormat.Height}" +
                    $"@{selectedFormat.FrameRate.Numerator / selectedFormat.FrameRate.Denominator}fps");
            }

            Console.WriteLine($"Frame source format: {selectedFormat.Subtype} : " +
                $"{selectedFormat.VideoFormat.Width}x{selectedFormat.VideoFormat.Height}" +
                $"@{selectedFormat.FrameRate.Numerator / selectedFormat.FrameRate.Denominator}fps");

            m_frameReader = await m_mediaCapture.CreateFrameReaderAsync(selectedFrameSource);
            m_frameReader.FrameArrived += FrameArrivedHandler;
            m_frameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;
            await m_frameReader.StartAsync();
        }

        /// <summary>
        /// Handle MediaCapture failure.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="errorEventArgs"></param>
        private async void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            Console.WriteLine($"MediaCapture failed: {errorEventArgs.Message}");
            await CleanupAsync();

            // if we failed to initialize MediaCapture ExclusiveControl with MF_E_HW_MFT_FAILED_START_STREAMING,
            // let's retry in SharedReadOnly mode since this points to a camera already in use
            if (m_sharingMode == MediaCaptureSharingMode.ExclusiveControl
                && errorEventArgs.Code == 0xc00d3704)
            {
                m_sharingMode = MediaCaptureSharingMode.SharedReadOnly;

                Console.WriteLine("Retrying MediaCapture initialization");
                await InitializeAsync();
            }
            else
            {
                m_failureHandler($"Camera error:{errorEventArgs.Code} | {errorEventArgs.Message}");
            }
        }

        /// <summary>
        /// Dispose of camera pipeline resources.
        /// </summary>
        /// <returns></returns>
        public async Task CleanupAsync()
        {
            if (m_frameReader != null)
            {
                m_frameReader.FrameArrived -= FrameArrivedHandler;
                await m_frameReader.StopAsync();
                m_frameReader.Dispose();
                m_frameReader = null;
            }
            if (m_mediaCapture != null)
            {
                m_mediaCapture.Failed -= MediaCapture_Failed;
                m_mediaCapture.Dispose();
            }
            m_mediaCapture = null;
        }

        /// <summary>
        /// Function to handle the frame when it arrives from FrameReader
        /// and send it back to registered new frame handler if it is valid.
        /// </summary>
        /// <param name="FrameReader"></param>
        /// <param name="args"></param>
        private void FrameArrivedHandler(MediaFrameReader FrameReader, MediaFrameArrivedEventArgs args)
        {
            using (var frame = FrameReader.TryAcquireLatestFrame())
            {
                if (frame == null) return;
                var vmf = frame.VideoMediaFrame;
                var videoFrame = vmf.GetVideoFrame();
                if (videoFrame != null)
                {
                    m_frameHandler(videoFrame);
                }
                frame.Dispose();
            }
        }
    }
}
