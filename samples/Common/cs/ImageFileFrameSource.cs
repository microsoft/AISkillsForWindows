// Copyright (C) Microsoft Corporation. All rights reserved.

using Microsoft.AI.Skills.SkillInterfacePreview;
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
        private VideoFrame m_videoFrame;
        private ISkillFeatureImageDescriptor m_desiredImageDescriptor = null;

        /// <summary>
        /// Static factory
        /// </summary>
        /// <param name="storageFile"></param>
        /// <param name="imageDescriptor"></param>
        /// <returns></returns>
        public static async Task<ImageFileFrameSource> CreateFromStorageFileAsyncTask(
            StorageFile storageFile,
            ISkillFeatureImageDescriptor imageDescriptor)
        {
            var result = new ImageFileFrameSource()
            {
                m_desiredImageDescriptor = imageDescriptor
            };
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

                // Convert to preferred format if specified and encapsulate the image in a VideoFrame instance
                var convertedSoftwareBitmap = m_desiredImageDescriptor == null ? SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore) 
                    : SoftwareBitmap.Convert(softwareBitmap, m_desiredImageDescriptor.SupportedBitmapPixelFormat, m_desiredImageDescriptor.SupportedBitmapAlphaMode);
                
                m_videoFrame = VideoFrame.CreateWithSoftwareBitmap(convertedSoftwareBitmap);
            }

            // Extract frame dimensions
            FrameWidth = (uint)softwareBitmap.PixelWidth;
            FrameHeight = (uint)softwareBitmap.PixelHeight;
        }
    }
}