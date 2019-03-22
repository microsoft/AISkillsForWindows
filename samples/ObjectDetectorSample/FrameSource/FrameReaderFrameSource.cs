// Copyright (C) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;

namespace ObjectDetectorSkill_SampleApp.FrameSource
{
    /// <summary>
    /// MediaFrameReader backed FrameSource
    /// </summary>
    public sealed class FrameReaderFrameSource : IFrameSource, IDisposable
    {
        private MediaCapture m_mediaCapture;
        private MediaFrameReader m_frameReader;
        private MediaFrameSource m_frameSource;
        private readonly object m_lock = new object();

        public uint FrameHeight { get; private set; }
        public uint FrameWidth { get; private set; }

        public event EventHandler<VideoFrame> FrameArrived;

        /// <summary>
        /// Dispose method implementation
        /// </summary>
        public void Dispose()
        {
            lock (m_lock)
            {
                m_mediaCapture.Dispose();
                m_frameReader.Dispose();
            }
        }

        /// <summary>
        /// Static factory method
        /// </summary>
        /// <param name="mediaCapture"></param>
        /// <returns></returns>
        public static async Task<FrameReaderFrameSource> CreateFromMediaCaptureAsync(MediaCapture mediaCapture)
        {
            var result = new FrameReaderFrameSource();

            result.m_mediaCapture = mediaCapture;
            await result.InitializeMediaFrameSourceAsync();
            await result.InitializeFrameReaderAsync();

            return result;
        }

        /// <summary>
        /// Initializes MediaCapture in compatible format, if possible.
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

            // if no preview stream are available, bail
            if (m_frameSource == null)
            {
                throw new Exception("No preview or record streams available");
            }

            // Filter MediaType given resolution and framerate preference, and filter out non-compatible subtypes
            var formats = m_frameSource.SupportedFormats.Where(format =>
                    format.FrameRate.Numerator / format.FrameRate.Denominator > 15
                    && (string.Compare(format.Subtype, MediaEncodingSubtypes.Nv12, true) == 0
                        || string.Compare(format.Subtype, MediaEncodingSubtypes.Bgra8, true) == 0
                        || string.Compare(format.Subtype, MediaEncodingSubtypes.Yuy2, true) == 0
                        || string.Compare(format.Subtype, MediaEncodingSubtypes.Rgb32, true) == 0)
                    )?.OrderBy(format => Math.Abs((int)(format.VideoFormat.Width * format.VideoFormat.Height) - (1920 * 1080)));
            var selectedFormat = formats.FirstOrDefault();

            if (selectedFormat != null)
            {
                await m_frameSource.SetFormatAsync(selectedFormat);
                FrameWidth = m_frameSource.CurrentFormat.VideoFormat.Width;
                FrameHeight = m_frameSource.CurrentFormat.VideoFormat.Height;
            }
            else
            {
                throw new Exception("No compatible formats available");
            }
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

            await m_frameReader.StartAsync();
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
                FrameArrived?.Invoke(sender, videoFrame);
            }
        }
    }
}