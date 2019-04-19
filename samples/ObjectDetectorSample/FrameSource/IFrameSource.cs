// Copyright (C) Microsoft Corporation. All rights reserved.

using System;
using System.Threading.Tasks;
using Windows.Media;

namespace ObjectDetectorSkill_SampleApp.FrameSource
{
    /// <summary>
    /// Standard FrameSource interface to wrap Video, Camera, and any other media streams in
    /// </summary>
    public interface IFrameSource
    {
        uint FrameHeight { get; }
        uint FrameWidth { get; }

        event EventHandler<VideoFrame> FrameArrived;
        Task StartAsync();
    }

    /// <summary>
    /// IFrameSource factory
    /// </summary>
    public static class FrameSourceFactory
    {
        /// <summary>
        /// Create an IFrameSource from a source object. Currently supports Windows.Storage.StorageFile
        /// and Windows.Media.Capture.MediaCapture source objects.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static async Task<IFrameSource> CreateFrameSourceAsync(object source)
        {
            if (source is Windows.Storage.StorageFile)
            {
                var sourceFile = source as Windows.Storage.StorageFile;
                if (sourceFile.ContentType.StartsWith("image"))
                {
                    return await ImageFileFrameSource.CreateFromStorageFileAsyncTask(sourceFile);
                }
                else if (sourceFile.ContentType.StartsWith("video"))
                {
                    return await MediaPlayerFrameSource.CreateFromStorageFileAsyncTask(sourceFile);
                }
                else
                {
                    throw new ArgumentException("Invalid file type received: " + sourceFile.ContentType);
                }
            }
            else if (source is Windows.Media.Capture.MediaCapture)
            {
                return await FrameReaderFrameSource.CreateFromMediaCaptureAsync(source as Windows.Media.Capture.MediaCapture);
            }
            else
            {
                throw new ArgumentException();
            }
        }
    }
}