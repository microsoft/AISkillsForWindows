// Copyright (C) Microsoft Corporation. All rights reserved.

using System;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Streams;

namespace FrameSourceHelper_UWP
{
    public sealed class ImageFileFrameSource : IFrameSource
    {
        public uint FrameHeight { get; private set; }
        public uint FrameWidth { get; private set; }

        public FrameSourceType FrameSourceType => FrameSourceType.Photo;

        public event EventHandler<VideoFrame> FrameArrived;
        VideoFrame m_videoFrame;

        /// <summary>
        /// Static factory
        /// </summary>
        /// <param name="storageFile"></param>
        /// <returns></returns>
        public static async Task<ImageFileFrameSource> CreateFromStorageFileAsyncTask(
            StorageFile storageFile)
        {
            var result = new ImageFileFrameSource();
            await result.GetFrameFromFileAsync(storageFile);
            return result;
        }

        /// <summary>
        /// Retrieve the corresponding VideoFrame via FrameArrived event
        /// </summary>
        public Task StartAsync()
        {
            FrameArrived?.Invoke(this, m_videoFrame);

            // Async not needed, return success
            return Task.FromResult(true);
        }

        /// <summary>
        /// Private constructor called by static factory
        /// </summary>
        private ImageFileFrameSource()
        {
        }

        /// <summary>
        /// Decode an image file into a VideoFrame
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private async Task GetFrameFromFileAsync(StorageFile file)
        {
            // Decoding image file content into a SoftwareBitmap, and wrap into VideoFrame
            SoftwareBitmap softwareBitmap = null;
            using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
            {
                // Create the decoder from the stream 
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                // Get the SoftwareBitmap representation of the file in BGRA8 format
                softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                // Convert to friendly format for UI display purpose and encapsulate the image in a VideoFrame instance
                m_videoFrame = VideoFrame.CreateWithSoftwareBitmap(SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied));
            }

            // Extract frame dimensions
            FrameWidth = (uint)softwareBitmap.PixelWidth;
            FrameHeight = (uint)softwareBitmap.PixelHeight;
        }
    }
}