// Copyright (C) Microsoft Corporation. All rights reserved.

using Microsoft.AI.Skills.SkillInterface;
using Microsoft.AI.Skills.Vision.ObjectDetector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Devices.Enumeration;
using Microsoft.Toolkit.Uwp.UI.Controls;
using FrameSourceHelper_UWP;
using SkillHelper;

namespace ObjectDetectorSkillSample
{
    /// <summary>
    /// Application's main page
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private IFrameSource m_frameSource = null;

        // Vision Skills
        // Skill wrappers
        private List<SkillWrapper> m_skillWrappers = new List<SkillWrapper>() { new SkillWrapper(new ObjectDetectorDescriptor()) };
        private IReadOnlyList<ISkillExecutionDevice> m_availableExecutionDevices = null;
        private ISkillFeatureImageDescriptor m_inputImageFeatureDescriptor = null;

        // Misc
        private BoundingBoxRenderer m_bboxRenderer = null;
        private List<bool> m_objectKindFilterList = null;

        // Frames
        private SoftwareBitmapSource m_processedBitmapSource;
        private VideoFrame m_renderTargetFrame = null;

        // Performance metrics
        private Stopwatch m_evalStopwatch = new Stopwatch();
        private float m_bindTime = 0;
        private float m_evalTime = 0;
        private Stopwatch m_renderStopwatch = new Stopwatch();

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
            m_bboxRenderer = new BoundingBoxRenderer(UIOverlayCanvas);

            m_lock.Wait();
            {
                NotifyUser("Initializing skill...");
                m_availableExecutionDevices = m_skillWrappers[0].ExecutionDevices;

                await InitializeObjectDetectorAsync();
                await UpdateSkillUIAsync();
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
                if (m_availableExecutionDevices.Count == 0)
                {
                    NotifyUser("No execution devices available, this skill cannot run on this device");
                }
                else
                {
                    // Display available execution devices
                    UISkillExecutionDevices.ItemsSource = m_availableExecutionDevices.Select((device) => new SkillExecutionDeviceWrappper(device));
                    
                    // Set SelectedIndex to index of currently selected device
                    for (int i = 0; i < m_availableExecutionDevices.Count; i++)
                    {
                        if (m_availableExecutionDevices[i].ExecutionDeviceKind == m_skillWrappers[0].Binding.Device.ExecutionDeviceKind
                            && m_availableExecutionDevices[i].Name == m_skillWrappers[0].Binding.Device.Name)
                        {
                            UISkillExecutionDevices.SelectedIndex = i;
                            break;
                        }
                    }
                }

                UISelectAllObjectKind.IsChecked = true;
                UIConfidenceThresholdControl.Value = (m_skillWrappers[0].Binding["InputConfidenceThreshold"].FeatureValue as SkillFeatureTensorFloatValue).GetAsVectorView().First();
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
            await m_skillWrappers[0].InitializeSkillAsync(device);
            m_inputImageFeatureDescriptor = m_skillWrappers[0].Binding["InputImage"].Descriptor as SkillFeatureImageDescriptor;
        }

        /// <summary>
        /// Bind and evaluate the frame with the ObjectDetector skill
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        private async Task<IReadOnlyList<ObjectDetectorResult>> DetectObjectsAsync(VideoFrame frame)
        {
            m_evalStopwatch.Restart();

            var objDetectionBinding = m_skillWrappers[0].Binding as ObjectDetectorBinding;

            // Bind
            await objDetectionBinding.SetInputImageAsync(frame);

            m_bindTime = (float)m_evalStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000f;
            m_evalStopwatch.Restart();

            // Evaluate
            await m_skillWrappers[0].Skill.EvaluateAsync(objDetectionBinding);

            m_evalTime = (float)m_evalStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000f;
            m_evalStopwatch.Stop();

            return objDetectionBinding.DetectedObjects;
        }

        /// <summary>
        /// Render ObjectDetector skill results
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="objectDetections"></param>
        /// <returns></returns>
        private async Task DisplayFrameAndResultAsync(VideoFrame frame, IReadOnlyList<ObjectDetectorResult> detectedObjects)
        {
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

                    // Update displayed results
                    m_bboxRenderer.Render(detectedObjects);

                    // Update the displayed performance text
                    UIPerfTextBlock.Text = $"bind: {m_bindTime.ToString("F2")}ms, eval: {m_evalTime.ToString("F2")}ms";
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
                await m_skillWrappers[0].InitializeSkillAsync(m_skillWrappers[0].Skill.Device);

                // Set additional input features as exposed in the UI
                await m_skillWrappers[0].Binding["InputObjectKindFilterList"].SetFeatureValueAsync(m_objectKindFilterList);
                await m_skillWrappers[0].Binding["InputConfidenceThreshold"].SetFeatureValueAsync((float)UIConfidenceThresholdControl.Value);
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
                        // Retrieve and filter results if requested
                        IReadOnlyList<ObjectDetectorResult> detectedObjects = await DetectObjectsAsync(frame);
                        await DisplayFrameAndResultAsync(frame, detectedObjects);
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
            Windows.Foundation.Point point = ge.TransformPoint(new Windows.Foundation.Point());
            Windows.Foundation.Rect rect = new Windows.Foundation.Rect(point, new Windows.Foundation.Point(point.X + UICameraButton.ActualWidth, point.Y + UICameraButton.ActualHeight));

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
        private async void UIDetectionThresholdControl_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            UIConfidenceThresholdValue.Text = ((float)UIConfidenceThresholdControl.Value).ToString();
            await m_lock.WaitAsync();
            {
                float valueToSet = (float)UIConfidenceThresholdControl.Value;
                await m_skillWrappers[0].Binding["InputConfidenceThreshold"].SetFeatureValueAsync(valueToSet);
            }
            m_lock.Release();
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UISelectAllObjectKind_Checked(object sender, RoutedEventArgs e)
        {
            await m_lock.WaitAsync();
            {
                // Reset the object kind filter list
                m_objectKindFilterList = null;
                await m_skillWrappers[0].Binding["InputObjectKindFilterList"].SetFeatureValueAsync(m_objectKindFilterList);

                if (UISelectAllObjectKind.IsChecked == true)
                {
                    UIObjectKindFilterExpander.IsEnabled = false;
                    UIObjectKindFilterExpander.IsExpanded = false;
                    UIObjectKindFilterExpander.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // Populate ObjectKind filters list with all possible classes supported by the detector
                    // Exclude Undefined label (not used by the detector) from selector list
                    UIObjectKindFilters.ItemsSource = Enum.GetValues(typeof(ObjectKind)).Cast<ObjectKind>().Where(kind => kind != ObjectKind.Undefined);
                    UIObjectKindFilterExpander.IsEnabled = true;
                    UIObjectKindFilterExpander.IsExpanded = true;
                    UIObjectKindFilterExpander.Visibility = Visibility.Visible;
                }
            }
            m_lock.Release();
            UIObjectKindFilters_SelectionChanged(null, null);
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UIObjectKindFilters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UIObjectKindFilterExpander.IsEnabled)
            {
                await m_lock.WaitAsync();
                {
                    m_objectKindFilterList = new List<bool>(new bool[Enum.GetNames(typeof(ObjectKind)).Length - 1]);
                    foreach (var item in UIObjectKindFilters.SelectedItems)
                    {
                        m_objectKindFilterList[(int)item] = true;
                    }
                    // Set the object kind filter list
                    await m_skillWrappers[0].Binding["InputObjectKindFilterList"].SetFeatureValueAsync(m_objectKindFilterList);
                }
                m_lock.Release();
            }
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

            m_bboxRenderer.ResizeContent(e);
        }

        /// <summary>
        /// Triggered when a skill is selected from the skill tab at the top of the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UISkillTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UISkillTabs.SelectedIndex < 0)
            {
                return;
            }
        }
    }
}