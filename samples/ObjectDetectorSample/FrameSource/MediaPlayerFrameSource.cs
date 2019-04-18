// Copyright (C) Microsoft Corporation. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.DirectX;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace ObjectDetectorSkill_SampleApp.FrameSource
{
    /// <summary>
    /// MediaPlayer backed FrameSource
    /// </summary>
    public sealed class MediaPlayerFrameSource : IFrameSource, IDisposable
    {
        private MediaPlayer m_mediaPlayer = null;

        private VideoFrame m_videoFrame;
        private EventWaitHandle m_frameSourceReadyEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
        public UInt32 FrameWidth { get; private set; }
        public UInt32 FrameHeight { get; private set; }

        /// <summary>
        /// Static factory
        /// </summary>
        /// <returns></returns>
        public static async Task<MediaPlayerFrameSource> CreateFromStorageFileAsyncTask(StorageFile storageFile)
        {
            var result = new MediaPlayerFrameSource();

            result.m_mediaPlayer = new MediaPlayer();
            result.m_mediaPlayer.Source = MediaSource.CreateFromStorageFile(storageFile);
            result.m_mediaPlayer.IsVideoFrameServerEnabled = true;
            result.m_mediaPlayer.RealTimePlayback = true;
            result.m_mediaPlayer.IsMuted = true;
            result.m_mediaPlayer.IsLoopingEnabled = true;
            result.m_mediaPlayer.CommandManager.IsEnabled = false;

            result.m_mediaPlayer.MediaOpened += result.mediaPlayer_MediaOpened;
            result.m_mediaPlayer.MediaEnded += result.mediaPlayer_MediaEnded;

            await Task.Run(() => result.m_frameSourceReadyEvent.WaitOne());

            return result;
        }

        /// <summary>
        /// Private constructor called by static factory
        /// </summary>
        /// <returns></returns>
        private MediaPlayerFrameSource()
        {
        }

        /// <summary>
        /// Start frame playback
        /// </summary>
        public Task StartAsync()
        {
            m_mediaPlayer.Play();

            // Async not needed, return success
            return Task.FromResult(true);
        }

        public event EventHandler<VideoFrame> FrameArrived;
        public event EventHandler StreamEnded;

        /// <summary>
        /// MediaPlayer.MediaOpened event handler. Completes frame source initialization
        /// by allocating frame buffer and registering for VideoFrameAvailable event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void mediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            // Retrieve media source resolution
            FrameWidth = m_mediaPlayer.PlaybackSession.NaturalVideoWidth;
            FrameHeight = m_mediaPlayer.PlaybackSession.NaturalVideoHeight;
            
            // Allocate and register for frames
            m_videoFrame = VideoFrame.CreateAsDirect3D11SurfaceBacked(
                            DirectXPixelFormat.B8G8R8A8UIntNormalized,
                            (int)FrameWidth,
                            (int)FrameHeight);

            m_mediaPlayer.VideoFrameAvailable += mediaPlayer_VideoFrameAvailable;

            m_frameSourceReadyEvent.Set();
        }

        /// <summary>
        /// MediaFrameReader.FrameArrived callback. Extracts VideoFrame and timestamp and forwards event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void mediaPlayer_VideoFrameAvailable(MediaPlayer sender, object args)
        {
            m_mediaPlayer.CopyFrameToVideoSurface(m_videoFrame.Direct3DSurface);
            m_videoFrame.SystemRelativeTime = m_mediaPlayer.PlaybackSession.Position;
            FrameArrived?.Invoke(this, m_videoFrame);
        }

        /// <summary>
        /// MediaPlayer.MediaEnded event callback. Forwards event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void mediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            StreamEnded?.Invoke(this, null);
        }

        /// <summary>
        /// Dispose method implementation
        /// </summary>
        public void Dispose()
        {
            m_mediaPlayer?.Pause();
            m_mediaPlayer?.Dispose();
            m_videoFrame?.Dispose();
        }
    }
}