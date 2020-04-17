// Copyright (c) Microsoft Corporation. All rights reserved.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Storage;
using Windows.Storage.Streams;
using Contoso.FaceSentimentAnalyzer;
namespace FaceSentimentAnalysisConsole
{
    class Program
    {
        private static MediaFrameReader m_frameReader = null;
        private static MediaCapture m_mediaCapture = null;
        private static FaceSentimentAnalyzerSkill m_skill;
        private static FaceSentimentAnalyzerBinding m_binding;
        private static int m_lock = 0;

        /// <summary>
        /// Entry point of program
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static void Main(string[] args)
        {
            Console.WriteLine("Face Sentiment Analyzer .NetCore 3.0 Console App: Please face your camera");

            Task.Run(async () =>
            {
                try
                {
                    var sceneClassifierSkilldesc = new  FaceSentimentAnalyzerDescriptor();
                    m_skill = await sceneClassifierSkilldesc.CreateSkillAsync() as FaceSentimentAnalyzerSkill;
                    var skillDevice = m_skill.Device;
                    Console.WriteLine("Running Skill on : " + skillDevice.ExecutionDeviceKind.ToString() + ": " + skillDevice.Name);

                    m_binding = await m_skill.CreateSkillBindingAsync() as FaceSentimentAnalyzerBinding;
                    await StartMediaCaptureAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error:: " + e.Message.ToString() + e.TargetSite.ToString() + e.Source.ToString() + e.StackTrace.ToString());
                    Environment.Exit(e.HResult);
                }
            }).Wait();

            Console.WriteLine("\nPress Any key to stop\n");

            var key = Console.ReadKey();

        }


        /// <summary>
        /// Evaluates the frame for scene classification
        /// </summary>
        /// <param name="vf"></param>
        /// <returns></returns>
        private static async Task EvaluateFrameAsync(VideoFrame vf)
        {
            // Process 1 frame at a time, if busy return right away
            if (0 == Interlocked.Exchange(ref m_lock, 1))
            {
                // Update input image and run the skill against it
                await m_binding.SetInputImageAsync(vf);
                await m_skill.EvaluateAsync(m_binding);
                string outText = "";
                if (!m_binding.IsFaceFound)
                {
                    // if no face found, hide the rectangle in the UI
                     outText = "No face found";
                }
                else // Display the sentiment on the console
                {
                    outText = "Your sentiment looks like: " + m_binding.PredominantSentiments[0].ToString();

                    var folder = await StorageFolder.GetFolderFromPathAsync(System.AppContext.BaseDirectory);
                    var file = await folder.CreateFileAsync(m_binding.PredominantSentiments.ToString() + "Face.jpg", CreationCollisionOption.ReplaceExisting);
                    using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                        SoftwareBitmap sb;
                        if (vf.SoftwareBitmap == null)
                        {
                            sb = await SoftwareBitmap.CreateCopyFromSurfaceAsync(vf.Direct3DSurface);
                        }
                        else
                        {
                            sb = vf.SoftwareBitmap;
                        }

                        encoder.SetSoftwareBitmap(
                            sb.BitmapPixelFormat.Equals(BitmapPixelFormat.Bgra8) ? sb : SoftwareBitmap.Convert(sb, BitmapPixelFormat.Bgra8)
                            );
                        await encoder.FlushAsync();
                    }

                }
                Console.Write("\r" + outText + "\t\t\t\t\t");

                // Release the lock
                Interlocked.Exchange(ref m_lock, 0);
            }
        }

        /// <summary>
        /// Initializes and starts Media Capture and frame reader.
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        private static async Task StartMediaCaptureAsync()
        {
            // Initialize media capture with default settings in video-only streaming mode and in shared mode so that multiple instances can access the camera concurrently
            m_mediaCapture = new MediaCapture();
            var mediaCaptureInistializationSettings = new MediaCaptureInitializationSettings()
            {
                StreamingCaptureMode = StreamingCaptureMode.Video,
                SharingMode = MediaCaptureSharingMode.SharedReadOnly
            };

            await m_mediaCapture.InitializeAsync(mediaCaptureInistializationSettings);

            var selectedFrameSource = m_mediaCapture.FrameSources.FirstOrDefault(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoPreview
                                                                                        && source.Value.Info.SourceKind == MediaFrameSourceKind.Color).Value;
            if (selectedFrameSource == null)
            {
                selectedFrameSource = m_mediaCapture.FrameSources.FirstOrDefault(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoRecord
                                                                                           && source.Value.Info.SourceKind == MediaFrameSourceKind.Color).Value;
            }
            if(selectedFrameSource == null)
            {
                throw(new Exception("No valid video frame sources were found with source type color."));
            }

            Console.WriteLine($"{selectedFrameSource.Info.DeviceInformation?.Name} | MediaStreamType: {selectedFrameSource.Info.MediaStreamType} MediaFrameSourceKind: {selectedFrameSource.Info.SourceKind}");

            m_frameReader = await m_mediaCapture.CreateFrameReaderAsync(selectedFrameSource);
            m_frameReader.FrameArrived += FrameArrivedHandler;
            m_frameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;
            await m_frameReader.StartAsync();

        }

        /// <summary>
        /// Handles the event of frame arrived from Frame Reader
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static void FrameArrivedHandler(MediaFrameReader sender, MediaFrameArrivedEventArgs e)
        {
            using (var frame = sender.TryAcquireLatestFrame())
            {
                if (frame == null) return;

                var vmf = frame.VideoMediaFrame;
                var videoFrame = vmf.GetVideoFrame();
                Task.Run(async () =>
                {
                    await EvaluateFrameAsync(videoFrame);
                }).Wait();
                videoFrame.Dispose();
            }

        }
    }
}
