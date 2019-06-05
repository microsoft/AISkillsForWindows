// Copyright (C) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;

namespace FrameSourceHelper_UWP
{
    /// <summary>
    /// MediaFrameReader backed FrameSource
    /// </summary>
    public sealed class FrameReaderFrameSource : IFrameSource, IDisposable
    {
        private MediaCapture m_mediaCapture;
        private MediaCaptureInitializationSettings m_mediaCaptureInitializationSettings;
        private MediaFrameReader m_frameReader;
        private MediaFrameSource m_frameSource;
        private EventHandler<string> m_failureHandler;
        private readonly object m_lock = new object();

        public uint FrameHeight { get; private set; }
        public uint FrameWidth { get; private set; }
        public FrameSourceType FrameSourceType => FrameSourceType.Camera;

        public event EventHandler<VideoFrame> FrameArrived;

        /// <summary>
        /// Start frame playback
        /// </summary>
        public async Task StartAsync()
        {
            await m_frameReader.StartAsync();
        }

        /// <summary>
        /// Dispose method implementation
        /// </summary>
        public void Dispose()
        {
            lock (m_lock)
            {
                if (m_frameReader != null)
                {
                    m_frameReader.FrameArrived -= FrameReader_FrameArrived;
                    m_frameReader.Dispose();
                    m_frameReader = null;
                }
                m_mediaCapture?.Dispose();
                m_mediaCapture = null;
            }
        }

        /// <summary>
        /// Static factory method
        /// </summary>
        /// <param name="mediaCapture"></param>
        /// <returns></returns>
        public static async Task<FrameReaderFrameSource> CreateFromVideoDeviceInformationAsync(
            DeviceInformation videoDeviceInformation, 
            EventHandler<string> failureHandler)
        {
            var result = new FrameReaderFrameSource();

            // Create new MediaCapture connected to our device
            result.m_mediaCapture = new MediaCapture();
            result.m_failureHandler = failureHandler;
            result.m_mediaCapture.Failed += result.MediaCapture_Failed;

            result.m_mediaCaptureInitializationSettings = new MediaCaptureInitializationSettings
            {
                SharingMode = MediaCaptureSharingMode.ExclusiveControl,
                MemoryPreference = MediaCaptureMemoryPreference.Auto,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                VideoDeviceId = videoDeviceInformation.Id,
            };
            await result.IntializeFrameSourceAsync();

            return result;
        }

        /// <summary>
        /// Initialize the frame source and frame reader
        /// </summary>
        /// <returns></returns>
        private async Task IntializeFrameSourceAsync()
        {
            await m_mediaCapture.InitializeAsync(m_mediaCaptureInitializationSettings);
            await InitializeMediaFrameSourceAsync();
            await InitializeFrameReaderAsync();
        }

        /// <summary>
        /// Handle the MediaCapture failure event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="errorEventArgs"></param>
        private async void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            // if we failed to initialize MediaCapture ExclusiveControl with MF_E_HW_MFT_FAILED_START_STREAMING,
            // let's retry in SharedReadOnly mode since this points to a camera already in use
            if (m_mediaCaptureInitializationSettings.SharingMode == MediaCaptureSharingMode.ExclusiveControl 
                && errorEventArgs.Code == 0xc00d3704) // if device is already in use
            {
                Dispose();
                m_mediaCapture = new MediaCapture();
                m_mediaCapture.Failed += MediaCapture_Failed;
                m_mediaCaptureInitializationSettings.SharingMode = MediaCaptureSharingMode.SharedReadOnly;

                await IntializeFrameSourceAsync();
                await StartAsync();
            }
            else
            {
                m_failureHandler?.Invoke(this, $"Error {errorEventArgs.Code} : {errorEventArgs.Message}");
            }
        }

        /// <summary>
        /// Initializes MediaCapture's frame source with a compatible format, if possible.
        /// Throws Exception if no compatible stream(s) available
        /// </summary>
        /// <returns></returns>
        private async Task InitializeMediaFrameSourceAsync()
        {
            if (m_mediaCapture == null)
            {
                return;
            }

            // Get preview or record stream as source
            Func<KeyValuePair<string, MediaFrameSource>, MediaStreamType, bool> filterFrameSources = (source, type) =>
            {
                return (source.Value.Info.MediaStreamType == type && source.Value.Info.SourceKind == MediaFrameSourceKind.Color);
            };
            m_frameSource = m_mediaCapture.FrameSources.FirstOrDefault(source => filterFrameSources(source, MediaStreamType.VideoPreview)).Value
                            ?? m_mediaCapture.FrameSources.FirstOrDefault(source => filterFrameSources(source, MediaStreamType.VideoRecord)).Value;

            // if no preview stream is available, bail
            if (m_frameSource == null)
            {
                throw new Exception("No preview or record stream available");
            }

            // If we can, let's attempt to change the format set on the source to our preferences
            if (m_mediaCaptureInitializationSettings.SharingMode == MediaCaptureSharingMode.ExclusiveControl)
            {
                // Filter MediaType given resolution and framerate preference, and filter out non-compatible subtypes
                // Prefer a BGRA8 format and defer to other supported subtypes if none is supported on the source
                var selectedFormat = m_frameSource.SupportedFormats.Where(format =>
                        format.FrameRate.Numerator / format.FrameRate.Denominator > 15
                        && string.Compare(format.Subtype, MediaEncodingSubtypes.Bgra8, true) == 0
                        )?.OrderBy(format => Math.Abs((int)(format.VideoFormat.Width * format.VideoFormat.Height) - (1920 * 1080))).FirstOrDefault();

                if (selectedFormat == null)
                {
                    selectedFormat = m_frameSource.SupportedFormats.Where(format =>
                        format.FrameRate.Numerator / format.FrameRate.Denominator > 15
                        && (string.Compare(format.Subtype, MediaEncodingSubtypes.Nv12, true) == 0
                            || string.Compare(format.Subtype, MediaEncodingSubtypes.Yuy2, true) == 0
                            || string.Compare(format.Subtype, MediaEncodingSubtypes.Rgb32, true) == 0)
                        )?.OrderBy(format => Math.Abs((int)(format.VideoFormat.Width * format.VideoFormat.Height) - (1920 * 1080))).FirstOrDefault();
                }
                if (selectedFormat == null)
                {
                    throw new Exception("No compatible formats available");
                }

                await m_frameSource.SetFormatAsync(selectedFormat);
            }
            FrameWidth = m_frameSource.CurrentFormat.VideoFormat.Width;
            FrameHeight = m_frameSource.CurrentFormat.VideoFormat.Height;
        }

        /// <summary>
        /// Initializes MediaFrameReader and registers for MediaCapture callback
        /// </summary>
        /// <returns></returns>
        private async Task InitializeFrameReaderAsync()
        {
            if (m_frameSource == null)
            {
                return;
            }
            
            // Create Bgra8 encoded FrameReader stream
            m_frameReader = await m_mediaCapture.CreateFrameReaderAsync(m_frameSource, MediaEncodingSubtypes.Bgra8);
            m_frameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;
            m_frameReader.FrameArrived += FrameReader_FrameArrived;
        }

        /// <summary>
        /// Private constructor called by CreateAsync (factory)
        /// </summary>
        private FrameReaderFrameSource()
        {
        }

        /// <summary>
        /// MediaFrameReader.FrameArrived callback. Extracts VideoFrame and timestamp and forwards event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void FrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            MediaFrameReference frame = null;
            lock (m_lock)
            {
                try
                {
                    frame = sender.TryAcquireLatestFrame();
                }
                catch (System.ObjectDisposedException)
                {
                    frame = null;
                }
            }
            if (frame != null)
            {
                VideoFrame videoFrame = frame.VideoMediaFrame.GetVideoFrame();
                videoFrame.SystemRelativeTime = frame.SystemRelativeTime;
                FrameArrived?.Invoke(this, videoFrame);
            }
        }
    }
}