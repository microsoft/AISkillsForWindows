// Copyright (C) Microsoft Corporation. All rights reserved.

using Microsoft.AI.Skills.SkillInterfacePreview;
using Microsoft.AI.Skills.Vision.ObjectDetectorPreview;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Graphics.Imaging;
using Windows.Graphics.DirectX;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;

namespace ObjectDetectorSkill_SampleApp
{
    /// <summary>
    /// The main page
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private FrameSource.IFrameSource m_frameSource = null;

        // Vision Skills
        private ObjectDetectorDescriptor m_descriptor = null;
        private ObjectDetectorBinding m_binding = null;
        private ObjectDetectorSkill m_skill = null;
        private IReadOnlyList<ISkillExecutionDevice> m_availableExecutionDevices = null;

        // Misc
        private BoundingBoxRenderer m_bboxRenderer = null;
        private HashSet<ObjectKind> m_objectKinds = null;

        // Frames
        private VideoFrame m_cachedFrameForProcessing = null;
        private SoftwareBitmapSource m_processedBitmapSource = new SoftwareBitmapSource();

        // Performance metrics
        private Stopwatch m_evalStopwatch = new Stopwatch();
        private Stopwatch m_renderStopwatch = new Stopwatch();

        // Locks
        private SemaphoreSlim m_skillLock = new SemaphoreSlim(1);

        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Called when page is loaded
        /// Initialize app assets such as skills
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // Disable buttons while we initialize
            await UpdateMediaSourceButtonsAsync(false);

            // Reset bitmap rendering component
            m_processedBitmapSource = new SoftwareBitmapSource();
            ProcessedPreview.Source = m_processedBitmapSource;
            m_bboxRenderer = new BoundingBoxRenderer(OverlayCanvas);

            m_skillLock.Wait();
            {
                NotifyUser("Initializing skill...");
                m_descriptor = new ObjectDetectorDescriptor();
                m_availableExecutionDevices = await m_descriptor.GetSupportedExecutionDevicesAsync();

                await InitializeObjectDetectorAsync();
                await UpdateSkillUIAsync();
            }
            m_skillLock.Release();

            // Ready to begin, enable buttons
            NotifyUser("Skill initialized. Select a media source from the top to begin.");
            await UpdateMediaSourceButtonsAsync(true);
        }

        private async Task UpdateSkillUIAsync()
        {
            if (Dispatcher.HasThreadAccess)
            {
                // Show skill description members in UI
                UISkillName.Text = m_descriptor.Name;

                UISkillDescription.Text = $"{m_descriptor.Description}" +
                $"\n\tauthored by: {m_descriptor.Version.Author}" +
                $"\n\tpublished by: {m_descriptor.Version.Author}" +
                $"\n\tversion: {m_descriptor.Version.Major}.{m_descriptor.Version.Minor}" +
                $"\n\tunique ID: {m_descriptor.Id}";

                var inputDesc = m_descriptor.InputFeatureDescriptors[0] as SkillFeatureImageDescriptor;
                UISkillInputDescription.Text = $"\tName: {inputDesc.Name}" +
                $"\n\tDescription: {inputDesc.Description}" +
                $"\n\tType: {inputDesc.FeatureKind}" +
                $"\n\tWidth: {inputDesc.Width}" +
                $"\n\tHeight: {inputDesc.Height}" +
                $"\n\tSupportedBitmapPixelFormat: {inputDesc.SupportedBitmapPixelFormat}" +
                $"\n\tSupportedBitmapAlphaMode: {inputDesc.SupportedBitmapAlphaMode}";

                var outputDesc = m_descriptor.OutputFeatureDescriptors[0] as ObjectDetectorResultListDescriptor;
                UISkillOutputDescription1.Text = $"\tName: {outputDesc.Name}, Description: {outputDesc.Description} \n\tType: Custom";

                if (m_availableExecutionDevices.Count == 0)
                {
                    NotifyUser("No execution devices available, this skill cannot run on this device");
                }
                else
                {
                    // Display available execution devices
                    UISkillExecutionDevices.ItemsSource = m_availableExecutionDevices.Select((device) => $"{device.ExecutionDeviceKind} | {device.Name}");
                    // Set SelectedIndex to index of currently selected device
                    for (int i = 0; i < m_availableExecutionDevices.Count; i++)
                    {
                        if (m_availableExecutionDevices[i].ExecutionDeviceKind == m_binding.Device.ExecutionDeviceKind
                            && m_availableExecutionDevices[i].Name == m_binding.Device.Name)
                        {
                            UISkillExecutionDevices.SelectedIndex = i;
                            break;
                        }
                    }
                }

                // Populate ObjectKind filters list with all possible classes supported by the detector
                // Exclude Undefined label (not used by the detector) from selector list
                UIObjectKindFilters.ItemsSource = Enum.GetValues(typeof(ObjectKind)).Cast<ObjectKind>().Where(kind => kind != ObjectKind.Undefined);
            }
            else
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => await UpdateSkillUIAsync());
            }
        }

        /// <summary>
        /// Initialize the ObjectDetector skill
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        private async Task InitializeObjectDetectorAsync(ISkillExecutionDevice device = null)
        {
            if (device != null)
            {
                m_skill = await m_descriptor.CreateSkillAsync(device) as ObjectDetectorSkill;
            }
            else
            {
                m_skill = await m_descriptor.CreateSkillAsync() as ObjectDetectorSkill;
            }
            m_binding = await m_skill.CreateSkillBindingAsync() as ObjectDetectorBinding;
        }

        /// <summary>
        /// Run the ObjectDetector skill on a frame and render the results
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        private async Task ProcessFrameAsync(VideoFrame frame)
        {
            m_evalStopwatch.Restart();
            var objectDetections = await DetectObjectsAsync(frame);
            m_evalStopwatch.Stop();

            m_renderStopwatch.Restart();
            await RenderResultsAsync(frame, objectDetections);
            m_renderStopwatch.Stop();
        }

        /// <summary>
        /// Bind and evaluate the frame with the ObjectDetector skill
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        private async Task<IReadOnlyList<ObjectDetectorResult>> DetectObjectsAsync(VideoFrame frame)
        {
            // Bind
            await m_binding.SetInputImageAsync(frame);

            // Evaluate
            await m_skill.EvaluateAsync(m_binding);
            var results = m_binding.DetectedObjects;

            // Filter results if requested
            if (m_objectKinds != null && m_objectKinds.Count > 0)
            {
                results = results.Where(det => m_objectKinds.Contains(det.Kind)).ToList();
            }

            return results;
        }

        /// <summary>
        /// Render ObjectDetector skill results
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="objectDetections"></param>
        /// <returns></returns>
        private async Task RenderResultsAsync(VideoFrame frame, IReadOnlyList<ObjectDetectorResult> objectDetections)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                try
                {
                    if (frame.SoftwareBitmap != null)
                    {
                        await m_processedBitmapSource.SetBitmapAsync(frame.SoftwareBitmap);
                    }
                    else
                    {
                        var bitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Direct3DSurface, BitmapAlphaMode.Ignore);
                        await m_processedBitmapSource.SetBitmapAsync(bitmap);
                    }
                    m_bboxRenderer.Render(objectDetections);
                }
                catch (TaskCanceledException)
                {
                    // no-op: we expect this exception when we change media sources
                    // and can safely ignore/continue
                }
                catch (Exception ex)
                {
                    NotifyUser($"Exception while rendering results: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Configure an IFrameSource from a StorageFile or MediaCapture instance
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private async Task ConfigureFrameSourceAsync(object source)
        {
            await m_skillLock.WaitAsync();
            {
                // Clean up previous frame source
                if (m_frameSource != null)
                {
                    m_frameSource.FrameArrived -= frameSource_FrameAvailable;
                    var disposableFrameSource = m_frameSource as IDisposable;
                    if (disposableFrameSource != null)
                    {
                        // Lock disposal based on frame source consumers
                        disposableFrameSource.Dispose();
                    }
                }

                // Create new frame source
                m_frameSource = await FrameSource.FrameSourceFactory.CreateFrameSourceAsync(source);
                m_cachedFrameForProcessing = VideoFrame.CreateAsDirect3D11SurfaceBacked(
                                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                                (int)m_frameSource.FrameWidth,
                                (int)m_frameSource.FrameHeight);
                m_frameSource.FrameArrived += frameSource_FrameAvailable;
            }
            m_skillLock.Release();
        }

        /// <summary>
        /// Update the displayed performance text
        /// </summary>
        /// <returns></returns>
        private async Task UpdateMetricsDisplayAsync()
        {
            float evalTime = m_evalStopwatch.ElapsedTicks / (Stopwatch.Frequency / 1000F);
            float renderTime = m_renderStopwatch.ElapsedTicks / (Stopwatch.Frequency / 1000F);

            // Update UI
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                UIPerfTextBlock.Text = $"{evalTime:0.000}ms eval, {renderTime:0.000}ms render";
            });
        }

        /// <summary>
        /// Print a message to the UI
        /// </summary>
        /// <param name="message"></param>
        private void NotifyUser(String message)
        {
            if (Dispatcher.HasThreadAccess)
            {
                UIMessageTextBlock.Text = message;
            }
            else
            {
                Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => UIMessageTextBlock.Text = message).AsTask().Wait();
            }
        }

        /// <summary>
        /// Update media source buttons (top row)
        /// </summary>
        /// <param name="enableButtons"></param>
        /// <returns></returns>
        private async Task UpdateMediaSourceButtonsAsync(bool enableButtons)
        {
            if (Dispatcher.HasThreadAccess)
            {
                UICameraButton.IsEnabled = enableButtons;
                UIVideoFilePickerButton.IsEnabled = enableButtons;
            }
            else
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => await UpdateMediaSourceButtonsAsync(enableButtons));
            }
        }

        /// <summary>
        /// FrameAvailable event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="frame"></param>
        private async void frameSource_FrameAvailable(object sender, VideoFrame frame)
        {
            // Locking behavior, so only one skill execution happens at a time
            if (m_skillLock.Wait(0))
            {
                await frame.CopyToAsync(m_cachedFrameForProcessing);

#pragma warning disable CS4014
                // Purposely don't await this: want handler to exit ASAP
                // so that realtime capture doesn't wait for completion.
                // Instead, ProcessFrameAsync will internally lock such that
                // only one execution is active at a time, dropping frames/
                // aborting skill runs as necessary
                Task.Run(async () =>
                {
                    try
                    {
                        await ProcessFrameAsync(m_cachedFrameForProcessing);
                        await UpdateMetricsDisplayAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                    finally
                    {
                        m_skillLock.Release();
                    }
                });
#pragma warning restore CS4014
            }
        }

        /// <summary>
        /// Click handler for video file button. Spawns file picker UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void VideoFilePickerButton_Click(object sender, RoutedEventArgs e)
        {
            // Disable the top menu while handling the click
            await UpdateMediaSourceButtonsAsync(false);

            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            picker.FileTypeFilter.Add(".mp4");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                await ConfigureFrameSourceAsync(file);
                NotifyUser("Loading video file: " + file.Path);
            }

            // Re-enable the top menu once done handling the click
            await UpdateMediaSourceButtonsAsync(true);
        }

        /// <summary>
        /// Click handler for camera button. Spawns device picker UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CameraButton_Click(object sender, RoutedEventArgs e)
        {
            // Disable the top menu while handling the click
            await UpdateMediaSourceButtonsAsync(false);

            var devicePicker = new DevicePicker();
            devicePicker.Filter.SupportedDeviceClasses.Add(DeviceClass.VideoCapture);

            // Calculate the position to show the picker (right below the buttons)
            GeneralTransform ge = UICameraButton.TransformToVisual(null);
            Windows.Foundation.Point point = ge.TransformPoint(new Windows.Foundation.Point());
            Windows.Foundation.Rect rect = new Windows.Foundation.Rect(point, new Windows.Foundation.Point(point.X + UICameraButton.ActualWidth, point.Y + UICameraButton.ActualHeight));

            DeviceInformation di = await devicePicker.PickSingleDeviceAsync(rect);
            if (di != null)
            {
                try
                {
                    NotifyUser("Attaching to camera " + di.Name);

                    // Create new MediaCapture connected to our device
                    var mediaCapture = new MediaCapture();
                    var settings = new MediaCaptureInitializationSettings
                    {
                        MemoryPreference = MediaCaptureMemoryPreference.Auto,
                        StreamingCaptureMode = StreamingCaptureMode.Video,
                        VideoDeviceId = di.Id,
                    };
                    await mediaCapture.InitializeAsync(settings);
                    await ConfigureFrameSourceAsync(mediaCapture);
                }
                catch (Exception ex)
                {
                    NotifyUser("Error occurred while initializating MediaCapture:\n" + ex.Message);
                }
            }

            // Re-enable the top menu once done handling the click
            await UpdateMediaSourceButtonsAsync(true);
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UISkillExecutionDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedDevice = m_availableExecutionDevices[UISkillExecutionDevices.SelectedIndex];
            await m_skillLock.WaitAsync();
            {
                await InitializeObjectDetectorAsync(selectedDevice);
            }
            m_skillLock.Release();
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UIObjectKindFilters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await m_skillLock.WaitAsync();
            {
                m_objectKinds = UIObjectKindFilters.SelectedItems.Cast<ObjectKind>().ToHashSet();
            }
            m_skillLock.Release();
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessedPreview_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            m_bboxRenderer.Resize(e);
        }
    }
}