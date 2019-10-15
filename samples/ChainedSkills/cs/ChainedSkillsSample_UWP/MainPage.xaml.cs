// Copyright (C) Microsoft Corporation. All rights reserved.

using FrameSourceHelper_UWP;
using Microsoft.AI.Skills.SkillInterfacePreview;
using Microsoft.AI.Skills.Vision.ObjectDetectorPreview;
using Microsoft.AI.Skills.Vision.ObjectTrackerPreview;
using Microsoft.Toolkit.Uwp.UI.Controls;
using ObjectTrackerSkillSample;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace ChainedSkillsSample
{
    /// <summary>
    /// Application's main page
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private IFrameSource m_frameSource = null;

        // Vision Skills
        private ObjectDetectorDescriptor m_detectorDescriptor = null;
        private ObjectDetectorBinding m_detectorBinding = null;
        private ObjectDetectorSkill m_detectorSkill = null;
        private ObjectTrackerDescriptor m_trackerDescriptor = null;
        private ObjectTrackerSkill m_trackerSkill = null;
        private List<ObjectTrackerBinding> m_trackerBindings = new List<ObjectTrackerBinding>();
        private List<Queue<TrackerResult>> m_trackerHistories = new List<Queue<TrackerResult>>();
        private UInt32 m_maxNumberTrackers = 5;
        private UInt32 m_maxTrackerHistoryLength = 20;
        private IReadOnlyList<ISkillExecutionDevice> m_availableExecutionDevices = null;
        private ISkillFeatureImageDescriptor m_inputImageFeatureDescriptor = null;
        private UInt32 m_frameCounter = 0;
        private UInt32 m_detectorEvalInterval = 300;

        // Misc
        private ObjectTrackRenderer m_renderer = null;
        private HashSet<ObjectKind> m_objectKinds = null;

        // Frames
        private SoftwareBitmapSource m_processedBitmapSource;
        private VideoFrame m_renderTargetFrame = null;

        // Performance metrics
        private Stopwatch m_evalStopwatch = new Stopwatch();
        private float m_bindTime = 0;
        private float m_evalTime = 0;

        // Locks
        private SemaphoreSlim m_lock = new SemaphoreSlim(1);

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
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Disable buttons while we initialize
            await UpdateMediaSourceButtonsAsync(false);

            // Initialize helper class used to render the skill results on screen
            m_renderer = new ObjectTrackRenderer(UIOverlayCanvas);

            m_lock.Wait();
            {
                NotifyUser("Initializing skill...");
                m_detectorDescriptor = new ObjectDetectorDescriptor();
                m_availableExecutionDevices = await m_detectorDescriptor.GetSupportedExecutionDevicesAsync();

                await InitializeObjectDetectorAsync();
                await UpdateSkillUIAsync();

                // Initialize ObjectTracker. As the skill only supports CPU right now, let's not worry about execution devices etc
                m_trackerDescriptor = new ObjectTrackerDescriptor();
                m_trackerSkill = await m_trackerDescriptor.CreateSkillAsync() as ObjectTrackerSkill;
            }
            m_lock.Release();

            // Ready to begin, enable buttons
            NotifyUser("Skill initialized. Select a media source from the top to begin.");
            await UpdateMediaSourceButtonsAsync(true);
        }

        /// <summary>
        /// Populate UI with skill information and options
        /// </summary>
        /// <returns></returns>
        private async Task UpdateSkillUIAsync()
        {
            if (Dispatcher.HasThreadAccess)
            {
                // Show skill description members in UI
                UISkillName.Text = m_detectorDescriptor.Information.Name;

                UISkillDescription.Text = SkillHelper.SkillHelperMethods.GetSkillDescriptorString(m_detectorDescriptor);

                int featureIndex = 0;
                foreach (var featureDesc in m_detectorDescriptor.InputFeatureDescriptors)
                {
                    UISkillInputDescription.Text += SkillHelper.SkillHelperMethods.GetSkillFeatureDescriptorString(featureDesc);
                    if (featureIndex++ > 0 && featureIndex < m_detectorDescriptor.InputFeatureDescriptors.Count - 1)
                    {
                        UISkillInputDescription.Text += "\n----\n";
                    }
                }

                featureIndex = 0;
                foreach (var featureDesc in m_detectorDescriptor.OutputFeatureDescriptors)
                {
                    UISkillOutputDescription.Text += SkillHelper.SkillHelperMethods.GetSkillFeatureDescriptorString(featureDesc);
                    if (featureIndex++ > 0 && featureIndex < m_detectorDescriptor.OutputFeatureDescriptors.Count - 1)
                    {
                        UISkillOutputDescription.Text += "\n----\n";
                    }
                }

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
                        if (m_availableExecutionDevices[i].ExecutionDeviceKind == m_detectorBinding.Device.ExecutionDeviceKind
                            && m_availableExecutionDevices[i].Name == m_detectorBinding.Device.Name)
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
                m_detectorSkill = await m_detectorDescriptor.CreateSkillAsync(device) as ObjectDetectorSkill;
            }
            else
            {
                m_detectorSkill = await m_detectorDescriptor.CreateSkillAsync() as ObjectDetectorSkill;
            }
            m_detectorBinding = await m_detectorSkill.CreateSkillBindingAsync() as ObjectDetectorBinding;

            m_inputImageFeatureDescriptor = m_detectorBinding["InputImage"].Descriptor as SkillFeatureImageDescriptor;
        }

        /// <summary>
        /// Bind and evaluate the frame with the ObjectDetector skill
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        private async Task RunSkillsAsync(VideoFrame frame)
        {
            m_bindTime = 0F;
            m_evalTime = 0F;
            m_frameCounter++;

            // Update all trackers
            m_evalStopwatch.Restart();
            var bindTasks = new List<Task>();
            foreach (var binding in m_trackerBindings)
            {
                bindTasks.Add(binding.SetInputImageAsync(frame).AsTask());
            }
            await Task.WhenAll(bindTasks);
            m_evalStopwatch.Stop();
            m_bindTime += (float)m_evalStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000f;

            m_evalStopwatch.Restart();
            var evalTasks = new List<Task>();
            foreach (var binding in m_trackerBindings)
            {
                evalTasks.Add(m_trackerSkill.EvaluateAsync(binding).AsTask());
            }
            await Task.WhenAll(evalTasks);
            m_evalStopwatch.Stop();
            m_evalTime += (float)m_evalStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000f;

            // Add results to histories
            for (int i = 0; i < m_trackerBindings.Count; i++)
            {
                m_trackerHistories[i].Enqueue(
                    new TrackerResult()
                    {
                        boundingRect = m_trackerBindings[i].BoundingRect,
                        succeeded = m_trackerBindings[i].Succeeded
                    }
                );
                if (m_trackerHistories[i].Count > m_maxTrackerHistoryLength)
                {
                    m_trackerHistories[i].Dequeue();
                }
            }

            // Run detector if no successful trackers or we've reached our desired detector period
            if (m_trackerBindings.Where(binding => binding.Succeeded).Count() == 0 || (m_frameCounter % m_detectorEvalInterval) == 0)
            {
                // Bind
                m_evalStopwatch.Restart();
                await m_detectorBinding.SetInputImageAsync(frame);
                m_evalStopwatch.Stop();
                m_bindTime += (float)m_evalStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000f;

                // Evaluate
                m_evalStopwatch.Restart();
                await m_detectorSkill.EvaluateAsync(m_detectorBinding);
                m_evalStopwatch.Stop();
                m_evalTime += (float)m_evalStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000f;

                // Clear and re-initialize trackers
                m_trackerBindings.Clear();
                m_trackerHistories.Clear();

                IEnumerable<Rect> filteredBoundingRects = m_detectorBinding.DetectedObjects.Where(det => ((m_objectKinds?.Count ?? 0) == 0) || m_objectKinds.Contains(det.Kind)).Select(det => det.Rect);
                var initializeTasks = new List<Task>();
                m_evalStopwatch.Restart();  // Since we're initializing trackers in parallel, it's easiest to count it all as evaluation
                                            // (including tracker binding)
                foreach (Rect boundingRect in filteredBoundingRects)
                {
                    // Cap at max trackers
                    if (m_trackerBindings.Count >= m_maxNumberTrackers)
                    {
                        break;
                    }

                    // Create and initialize new tracker
                    ObjectTrackerBinding binding = await m_trackerSkill.CreateSkillBindingAsync() as ObjectTrackerBinding;
                    initializeTasks.Add(m_trackerSkill.InitializeTrackerAsync(binding, frame, boundingRect).AsTask());
                    m_trackerBindings.Add(binding);

                    // Add corresponding tracker history
                    m_trackerHistories.Add(new Queue<TrackerResult>());
                    m_trackerHistories.Last().Enqueue(
                        new TrackerResult()
                        {
                            boundingRect = boundingRect,
                            succeeded = true
                        }
                    );
                }
                await Task.WhenAll(initializeTasks);
                m_evalStopwatch.Stop();
                m_evalTime += (float)m_evalStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000f;

                m_frameCounter = 0; // Reset frame counter
            }
        }

        /// <summary>
        /// Render ObjectDetector skill results
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        private async Task DisplayFrameAndResultAsync(VideoFrame frame)
        {
            // Cache values
            float bindTime = m_bindTime;
            float evalTime = m_evalTime;

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                try
                {
                    SoftwareBitmap targetSoftwareBitmap = frame.SoftwareBitmap;

                    // If we receive a Direct3DSurface-backed VideoFrame, convert to a SoftwareBitmap in a format that can be rendered via the UI element
                    if(targetSoftwareBitmap == null)
                    {
                        if(m_renderTargetFrame == null)
                        {
                            m_renderTargetFrame = new VideoFrame(BitmapPixelFormat.Bgra8, frame.Direct3DSurface.Description.Width, frame.Direct3DSurface.Description.Height, BitmapAlphaMode.Ignore);
                        }

                        // Leverage the VideoFrame.CopyToAsync() method that can convert the input Direct3DSurface-backed VideoFrame to a SoftwareBitmap-backed VideoFrame
                        await frame.CopyToAsync(m_renderTargetFrame);
                        targetSoftwareBitmap = m_renderTargetFrame.SoftwareBitmap;
                    }
                    // Else, if we receive a SoftwareBitmap-backed VideoFrame, if its format cannot already be rendered via the UI element, convert it accordingly
                    else
                    {
                        if (targetSoftwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 || targetSoftwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Ignore)
                        {
                            if (m_renderTargetFrame == null)
                            {
                                m_renderTargetFrame = new VideoFrame(BitmapPixelFormat.Bgra8, targetSoftwareBitmap.PixelWidth, targetSoftwareBitmap.PixelHeight, BitmapAlphaMode.Ignore);
                            }

                            // Leverage the VideoFrame.CopyToAsync() method that can convert the input SoftwareBitmap-backed VideoFrame to a different format
                            await frame.CopyToAsync(m_renderTargetFrame);
                            targetSoftwareBitmap = m_renderTargetFrame.SoftwareBitmap;
                        }                        
                    }
                    await m_processedBitmapSource.SetBitmapAsync(targetSoftwareBitmap);

                    //// Retrieve and filter results if requested
                    //IReadOnlyList<ObjectDetectorResult> objectDetections = m_detectorBinding.DetectedObjects;
                    //if (m_objectKinds?.Count > 0)
                    //{
                    //    objectDetections = objectDetections.Where(det => m_objectKinds.Contains(det.Kind)).ToList();
                    //}

                    //// Update displayed results
                    //// FIXME
                    //m_renderer.Render(objectDetections);

                    // Render results
                    m_renderer.ClearCanvas();
                    m_renderer.RenderTrackerResults(m_trackerHistories, true);

                    // Update the displayed performance text
                    UIPerfTextBlock.Text = $"bind: {bindTime.ToString("F2")}ms, eval: {evalTime.ToString("F2")}ms";
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
        /// Configure an IFrameSource from a StorageFile or MediaCapture instance to produce optionally a specified format of frame
        /// </summary>
        /// <param name="source"></param>
        /// <param name="inputImageDescriptor"></param>
        /// <returns></returns>
        private async Task ConfigureFrameSourceAsync(object source, ISkillFeatureImageDescriptor inputImageDescriptor = null)
        {
            await m_lock.WaitAsync();
            {
                // Reset bitmap rendering component
                UIProcessedPreview.Source = null;
                m_renderTargetFrame = null;
                m_processedBitmapSource = new SoftwareBitmapSource();
                UIProcessedPreview.Source = m_processedBitmapSource;

                // Clean up previous frame source
                if (m_frameSource != null)
                {
                    m_frameSource.FrameArrived -= FrameSource_FrameAvailable;
                    var disposableFrameSource = m_frameSource as IDisposable;
                    if (disposableFrameSource != null)
                    {
                        // Lock disposal based on frame source consumers
                        disposableFrameSource.Dispose();
                    }
                }

                // Create new frame source and register a callback if the source fails along the way
                m_frameSource = await FrameSourceFactory.CreateFrameSourceAsync(
                    source, 
                    (sender, message) => 
                    {
                        NotifyUser(message);
                    },
                    inputImageDescriptor);

                // TODO: Workaround for a bug in ObjectDetectorBinding when binding consecutively VideoFrames with Direct3DSurface and SoftwareBitmap
                m_detectorBinding = await m_detectorSkill.CreateSkillBindingAsync() as ObjectDetectorBinding;
            }
            m_lock.Release();

            // If we obtained a valid frame source, start it
            if (m_frameSource != null)
            {
                m_frameSource.FrameArrived += FrameSource_FrameAvailable;
                await m_frameSource.StartAsync();
            }
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
                UIFilePickerButton.IsEnabled = enableButtons;
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
        private void FrameSource_FrameAvailable(object sender, VideoFrame frame)
        {
            // Locking behavior, so only one skill execution happens at a time
            if (m_lock.Wait(0))
            {
#pragma warning disable CS4014
                // Purposely don't await this: want handler to exit ASAP
                // so that realtime capture doesn't wait for completion.
                // Instead, we unlock only when processing finishes ensuring that
                // only one execution is active at a time, dropping frames or
                // aborting skill runs as necessary
                Task.Run(async () =>
                {
                    try
                    {
                        await RunSkillsAsync(frame);
                        await DisplayFrameAndResultAsync(frame);
                    }
                    catch (Exception ex)
                    {
                        NotifyUser(ex.Message);
                    }
                    finally
                    {
                        m_lock.Release();
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
        private async void UIFilePickerButton_Click(object sender, RoutedEventArgs e)
        {
            // Disable the top menu while handling the click
            await UpdateMediaSourceButtonsAsync(false);

            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            // Add common video file extensions
            picker.FileTypeFilter.Add(".mp4");
            picker.FileTypeFilter.Add(".avi");
            // Add common image file extensions
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".bmp");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                await ConfigureFrameSourceAsync(file);
                NotifyUser("Loading file: " + file.Path);
            }

            // Re-enable the top menu once done handling the click
            await UpdateMediaSourceButtonsAsync(true);
        }

        /// <summary>
        /// Click handler for camera button. Spawns device picker UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UICameraButton_Click(object sender, RoutedEventArgs e)
        {
            // Disable the top menu while handling the click
            await UpdateMediaSourceButtonsAsync(false);

            var devicePicker = new DevicePicker();
            devicePicker.Filter.SupportedDeviceClasses.Add(DeviceClass.VideoCapture);

            // Calculate the position to show the picker (right below the buttons)
            GeneralTransform ge = UICameraButton.TransformToVisual(null);
            Point point = ge.TransformPoint(new Point());
            Rect rect = new Rect(point, new Point(point.X + UICameraButton.ActualWidth, point.Y + UICameraButton.ActualHeight));

            DeviceInformation di = await devicePicker.PickSingleDeviceAsync(rect);
            if (di != null)
            {
                try
                {
                    NotifyUser("Attaching to camera " + di.Name);
                    await ConfigureFrameSourceAsync(di, m_inputImageFeatureDescriptor);
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
        /// Triggers when a skill execution device is selected from the UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UISkillExecutionDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedDevice = m_availableExecutionDevices[UISkillExecutionDevices.SelectedIndex];
            await m_lock.WaitAsync();
            {
                await InitializeObjectDetectorAsync(selectedDevice);
            }
            m_lock.Release();
            if (m_frameSource != null)
            {
                await m_frameSource.StartAsync();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UIObjectKindFilters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await m_lock.WaitAsync();
            {
                m_objectKinds = UIObjectKindFilters.SelectedItems.Cast<ObjectKind>().ToHashSet();
            }
            m_lock.Release();
        }

        /// <summary>
        /// Triggered when the image control is resized, making sure the canvas size stays in sync with the frame display control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UIProcessedPreview_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Make sure the aspect ratio is honored when rendering the body limbs
            float cameraAspectRatio = (float)m_frameSource.FrameWidth / m_frameSource.FrameHeight;
            float previewAspectRatio = (float)(UIProcessedPreview.ActualWidth / UIProcessedPreview.ActualHeight);
            UIOverlayCanvas.Width = cameraAspectRatio >= previewAspectRatio ? UIProcessedPreview.ActualWidth : UIProcessedPreview.ActualHeight * cameraAspectRatio;
            UIOverlayCanvas.Height = cameraAspectRatio >= previewAspectRatio ? UIProcessedPreview.ActualWidth / cameraAspectRatio : UIProcessedPreview.ActualHeight;
        }

        /// <summary>
        /// Triggered when the expander is expanded and collapsed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UIExpander_Expanded(object sender, EventArgs e)
        {
            var expander = (sender as Expander);
            if(expander.IsExpanded)
            {
                UIVideoFeed.Visibility = Visibility.Collapsed;
            }
            else
            {
                UIVideoFeed.Visibility = Visibility.Visible;
            }
        }
    }
}