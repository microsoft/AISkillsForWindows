// Copyright (C) Microsoft Corporation. All rights reserved.

using System;
using System.Threading.Tasks;
using Windows.Media;

namespace FrameSourceHelper_UWP
{
    /// <summary>
    /// Standard FrameSource interface to wrap Video, Camera, and any other media streams in
    /// </summary>
    public interface IFrameSource
    {
        FrameSourceType FrameSourceType { get; }
        uint FrameHeight { get; }
        uint FrameWidth { get; }

        event EventHandler<VideoFrame> FrameArrived;
        Task StartAsync();
    }

    public enum FrameSourceType
    {
        Photo,
        Video,
        Camera
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
        public static async Task<IFrameSource> CreateFrameSourceAsync(object source, EventHandler<string> failureHandler)
        {
            try
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
                        return await MediaPlayerFrameSource.CreateFromStorageFileAsyncTask(sourceFile, failureHandler);
                    }
                    else
                    {
                        throw new ArgumentException("Invalid file type received: " + sourceFile.ContentType);
                    }
                }
                else if (source is Windows.Devices.Enumeration.DeviceInformation)
                {
                    return await FrameReaderFrameSource.CreateFromVideoDeviceInformationAsync(source as Windows.Devices.Enumeration.DeviceInformation, failureHandler);
                }
                else
                {
                    throw new ArgumentException();
                }
            }
            catch (Exception ex)
            {
                failureHandler(null, ex.Message);
            }
            return null;
        }
    }
}