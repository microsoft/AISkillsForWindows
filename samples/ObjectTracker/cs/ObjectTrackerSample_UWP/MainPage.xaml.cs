// Copyright (C) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Microsoft.AI.Skills.SkillInterfacePreview;
using Microsoft.AI.Skills.Vision.ObjectTrackerPreview;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml.Media.Imaging;
using FrameSourceHelper_UWP;

namespace ObjectTrackerSample
{
    /// <summary>
    /// Helper struct for storing tracker results
    /// </summary>
    internal struct TrackerResult
    {
        public Rect boundingRect;
        public bool succeeded;
    }

    /// <summary>
    /// Page for demonstrating the ObjectTrackerSkill on a webcam snapshot.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Ink analyzer
        private InkAnalyzer m_inkAnalyzer = new InkAnalyzer();
        private List<Rect> m_drawnRects = new List<Rect>();

        // Skill-related variables
        private ObjectTrackerDescriptor m_descriptor = null;
        private ObjectTrackerSkill m_skill = null;
        private List<ObjectTrackerBinding> m_bindings = null;
        private IReadOnlyList<ISkillExecutionDevice> m_availableExecutionDevices = null;
        private List<List<TrackerResult>> m_trackerHistory = null;

        // Frame source
        private IFrameSource m_frameSource = null;
        private SoftwareBitmapSource m_bitmapSource = new SoftwareBitmapSource();
        private bool m_frameSourceIsStreaming = false;

        // UI-related variables
        private ObjectTrackRenderer m_objectTrackRenderer = null;
        private uint m_cameraFrameWidth, m_cameraFrameHeight;
        private bool m_isCameraFrameDimensionInitialized = false;

        // Stopwatches
        private Stopwatch m_skillEvalStopWatch = new Stopwatch();
        private Stopwatch m_renderStopWatch = new Stopwatch();

        // Synchronization
        private SemaphoreSlim m_lock = new SemaphoreSlim(1);
        private SemaphoreSlim m_bboxLock = new SemaphoreSlim(1);
        private SemaphoreSlim m_renderLock = new SemaphoreSlim(1);

        /// <summary>
        /// Initializes a new instance of the MainPage class.
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();

            // Set supported inking device types.
            UIInkCanvasOverlay.InkPresenter.InputDeviceTypes =
                Windows.UI.Core.CoreInputDeviceTypes.Mouse |
                Windows.UI.Core.CoreInputDeviceTypes.Pen |
                Windows.UI.Core.CoreInputDeviceTypes.Touch;

            // Set initial ink stroke attributes.
            InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();
            drawingAttributes.Size = new Size(5.0, 5.0);
            drawingAttributes.Color = Windows.UI.Colors.Cyan;
            drawingAttributes.IgnorePressure = false;
            drawingAttributes.FitToCurve = true;
            UIInkCanvasOverlay.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);
            UIInkCanvasOverlay.InkPresenter.IsInputEnabled = true;
        }

        /// <summary>
        /// Responds when we navigate to this page.
        /// </summary>
        /// <param name="e">Event data</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // Initialize renderer
            m_objectTrackRenderer = new ObjectTrackRenderer(UICanvasOverlay);
            UICameraPreview.Source = m_bitmapSource;

            // Initialize skill
            await InitializeSkillAsync();
            await UpdateSkillUIAsync();

            // Pick a default camera device
            DeviceInformationCollection availableCameras = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            if (availableCameras.Count > 0)
            {
                DeviceInformation defaultCamera = availableCameras.First();
                await ConfigureFrameSourceAsync(defaultCamera);
                // Auto-press the play button for the user
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => TogglePlaybackState(true));
            }
            else
            {
                NotifyUser("No cameras detected. Please select a frame source from the top bar to begin", NotifyType.WarningMessage);
            }
        }

        /// <summary>
        /// Update the UI with skill information
        /// </summary>
        /// <returns></returns>
        private async Task UpdateSkillUIAsync()
        {
            if (Dispatcher.HasThreadAccess)
            {
                // Show skill description members in UI
                UISkillName.Text = m_descriptor.Information.Name;

                UISkillDescription.Text = SkillHelper.SkillHelperMethods.GetSkillDescriptorString(m_descriptor);

                int featureIndex = 0;
                foreach (var featureDesc in m_descriptor.InputFeatureDescriptors)
                {
                    UISkillInputDescription.Text += SkillHelper.SkillHelperMethods.GetSkillFeatureDescriptorString(featureDesc);
                    if (featureIndex++ > 0 && featureIndex < m_descriptor.InputFeatureDescriptors.Count - 1)
                    {
                        UISkillInputDescription.Text += "\n----\n";
                    }
                }

                featureIndex = 0;
                foreach (var featureDesc in m_descriptor.OutputFeatureDescriptors)
                {
                    UISkillOutputDescription.Text += SkillHelper.SkillHelperMethods.GetSkillFeatureDescriptorString(featureDesc);
                    if (featureIndex++ > 0 && featureIndex < m_descriptor.OutputFeatureDescriptors.Count - 1)
                    {
                        UISkillOutputDescription.Text += "\n----\n";
                    }
                }

                if (m_availableExecutionDevices.Count == 0)
                {
                    NotifyUser("No execution devices available, this skill cannot run on this device", NotifyType.ErrorMessage);
                }
                else
                {
                    // Display available execution devices
                    UISkillExecutionDevices.ItemsSource = m_availableExecutionDevices.Select((device) => $"{device.ExecutionDeviceKind} | {device.Name}");

                    // Set SelectedIndex to index of currently selected device
                    for (int i = 0; i < m_availableExecutionDevices.Count; i++)
                    {
                        if (m_availableExecutionDevices[i].ExecutionDeviceKind == m_skill.Device.ExecutionDeviceKind
                            && m_availableExecutionDevices[i].Name == m_skill.Device.Name)
                        {
                            UISkillExecutionDevices.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
            else
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => await UpdateSkillUIAsync());
            }
        }

        /// <summary>
        /// Initialize the skill related members
        /// </summary>
        /// <param name="executionDevice"></param>
        /// <returns></returns>
        private async Task InitializeSkillAsync(ISkillExecutionDevice executionDevice = null)
        {
            m_descriptor = new ObjectTrackerDescriptor();
            m_availableExecutionDevices = await m_descriptor.GetSupportedExecutionDevicesAsync();
            if (m_availableExecutionDevices.Count == 0)
            {
                NotifyUser("No execution devices available, this skill cannot run on this device", NotifyType.ErrorMessage);
                return; // Abort
            }

            // Either create skill using provided execution device or let skill create with default device if none provided
            if (executionDevice != null)
            {
                m_skill = await m_descriptor.CreateSkillAsync(executionDevice) as ObjectTrackerSkill;
            }
            else
            {
                m_skill = await m_descriptor.CreateSkillAsync() as ObjectTrackerSkill;
            }

            m_bindings = new List<ObjectTrackerBinding>();
            m_trackerHistory = new List<List<TrackerResult>>();
        }

        /// <summary>
        /// Update existing trackers with the latest frame and initialize any new trackers based on user input
        /// This should always be called from inside the lock (m_lock)
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        private async Task RunSkillAsync(VideoFrame frame)
        {
            // Update existing trackers
            if (m_bindings.Count > 0)
            {
                long evalTicks = 0L;
                for (int i = 0; i < m_bindings.Count; i++)
                {
                    await m_bindings[i].SetInputImageAsync(frame);
                    m_skillEvalStopWatch.Restart();
                    await m_skill.EvaluateAsync(m_bindings[i]);
                    m_skillEvalStopWatch.Stop();
                    evalTicks += m_skillEvalStopWatch.ElapsedTicks;

                    // Add result to history
                    m_trackerHistory[i].Add(
                        new TrackerResult()
                        {
                            boundingRect = m_bindings[i].BoundingRect,
                            succeeded = m_bindings[i].Succeeded
                        }
                    );
                }

                // Render results
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    m_renderStopWatch.Restart();
                    m_objectTrackRenderer.ClearCanvas();
                    m_objectTrackRenderer.RenderTrackerResults(m_trackerHistory, true);
                    m_renderStopWatch.Stop();

                    // Print performance measurements
                    PerformanceBlock.Text = String.Format(
                        "Skill Eval: {0:0.000}ms, Render: {1:0.000}ms",
                        evalTicks / (Stopwatch.Frequency / 1000F),
                        m_renderStopWatch.ElapsedTicks / (Stopwatch.Frequency / 1000F));
                });
            }

            // Optionally re-initialize any existing trackers if app feature enabled
            if (m_periodicallyReinitializeTrackers && m_reinitializeTrackersPeriod > 0)
            {
                for (int i = 0; i < m_trackerHistory.Count; i++)
                {
                    if (m_trackerHistory[i].Count % m_reinitializeTrackersPeriod == 0)
                    {
                        // Re-initialize tracker if we were successful
                        // TODO: We can also try saving last good rect, but that can get messy quickly
                        if (m_bindings[i].Succeeded)
                        {
                            var recentBoundingRect = m_bindings[i].BoundingRect;
                            await m_skill.InitializeTrackerAsync(m_bindings[i], frame, recentBoundingRect);
                        }
                    }
                }
            }

            // Initialize any new trackers
            if (m_drawnRects.Count > 0)
            {
                // Initialize new trackers if desired
                m_bboxLock.Wait();

                for (int i = 0; i < m_drawnRects.Count; i++)
                {
                    ObjectTrackerBinding binding = await m_skill.CreateSkillBindingAsync() as ObjectTrackerBinding;
                    await m_skill.InitializeTrackerAsync(binding, frame, m_drawnRects[i]);
                    m_bindings.Add(binding);

                    // Add corresponding tracker history
                    m_trackerHistory.Add(
                        new List<TrackerResult> {
                                    new TrackerResult()
                                    {
                                        boundingRect = binding.BoundingRect,
                                        succeeded = true
                                    }
                        }
                    );
                }

                m_drawnRects.Clear();
                m_bboxLock.Release();
            }
        }

        /// <summary>
        /// Types of notification messages
        /// </summary>
        public enum NotifyType
        {
            StatusMessage,
            WarningMessage,
            ErrorMessage
        };

        /// <summary>
        /// Used to display messages to the user
        /// </summary>
        /// <param name="strMessage"></param>
        /// <param name="type"></param>
        public async void NotifyUser(string strMessage, NotifyType type = NotifyType.StatusMessage)
        {
            if (Dispatcher.HasThreadAccess)
            {
                switch (type)
                {
                    case NotifyType.StatusMessage:
                        StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                        break;
                    case NotifyType.WarningMessage:
                        StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Yellow);
                        break;
                    case NotifyType.ErrorMessage:
                        StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                        break;
                }
                StatusBlock.Text = strMessage;

                // Collapse the StatusBlock if it has no text to conserve real estate.
                StatusBorder.Visibility = (StatusBlock.Text != String.Empty) ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => NotifyUser(strMessage, type));
            }
        }

        /// <summary>
        /// Update states of media source buttons
        /// </summary>
        /// <param name="enableButtons"></param>
        /// <returns></returns>
        private async Task UpdateMediaSourceButtonsAsync(bool enableButtons)
        {
            if (Dispatcher.HasThreadAccess)
            {
                CameraButton.IsEnabled = enableButtons;
                VideoFilePickerButton.IsEnabled = enableButtons;
            }
            else
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => await UpdateMediaSourceButtonsAsync(enableButtons));
            }
        }

        /// <summary>
        /// Triggered when a new frame is available from the camera stream.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void FrameSource_FrameArrived(object sender, VideoFrame frame)
        {
            try
            {
                // Use a lock to process frames one at a time and bypass processing if busy
                if (m_lock.Wait(0))
                {
                    // Render incoming frame
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        await m_renderLock.WaitAsync();
                        if (frame.SoftwareBitmap != null)
                        {
                            await m_bitmapSource.SetBitmapAsync(frame.SoftwareBitmap);
                        }
                        else
                        {
                            var bitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Direct3DSurface, BitmapAlphaMode.Ignore);
                            await m_bitmapSource.SetBitmapAsync(bitmap);
                        }
                        m_renderLock.Release();
                    });

                    // Allign overlay canvas and camera preview so that the rendered rectangle looks right
                    if (!m_isCameraFrameDimensionInitialized || m_frameSource.FrameWidth != m_cameraFrameWidth || m_frameSource.FrameHeight != m_cameraFrameHeight)
                    {
                        m_cameraFrameWidth = m_frameSource.FrameWidth;
                        m_cameraFrameHeight = m_frameSource.FrameHeight;

                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            UICameraPreview_SizeChanged(null, null);
                        });

                        m_isCameraFrameDimensionInitialized = true;
                    }

                    // Run the skill
                    await RunSkillAsync(frame);

                    m_lock.Release();
                }
            }
            catch (Exception ex)
            {
                // Show the error
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => NotifyUser(ex.Message, NotifyType.ErrorMessage));
                m_lock.Release();
            }
        }

        /// <summary>
        /// Triggers when the image control is resized, makes sure the canvas size stays in sync with the frame display control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UICameraPreview_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            float aspectRatio = (float)m_cameraFrameWidth / m_cameraFrameHeight;

            // Resize canvas to fit camera aspect ratio
            if ((UICameraPreview.ActualWidth / UICameraPreview.ActualHeight) > aspectRatio)
            {
                UICanvasOverlay.Width = UICameraPreview.ActualHeight * aspectRatio;
                UICanvasOverlay.Height = UICameraPreview.ActualHeight;
            }
            else
            {
                UICanvasOverlay.Width = UICameraPreview.ActualWidth;
                UICanvasOverlay.Height = UICameraPreview.ActualWidth / aspectRatio;
            }
            UIInkCanvasOverlay.Width = UICanvasOverlay.Width;
            UIInkCanvasOverlay.Height = UICanvasOverlay.Height;
        }

        /// <summary>
        /// Conditionally dispose old frame source and create new frame source
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private async Task ConfigureFrameSourceAsync(object source)
        {
            await m_lock.WaitAsync();
            {
                // Dispose old frame source
                if (m_frameSource != null)
                {
                    m_frameSource.FrameArrived -= FrameSource_FrameArrived;
                    var disposableFrameSource = m_frameSource as IDisposable;
                    if (disposableFrameSource != null)
                    {
                        disposableFrameSource.Dispose();
                    }
                }

                // Create new frame source
                m_frameSource = await FrameSourceFactory.CreateFrameSourceAsync(source, (sender, message) =>
                {
                    NotifyUser(message, NotifyType.ErrorMessage);
                });

                // If we obtained a valid frame source, hook a frame callback
                if (m_frameSource != null)
                {
                    m_frameSource.FrameArrived += FrameSource_FrameArrived;
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        UIPlayButton.IsEnabled = true;
                    });
                }
                else
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        UIPlayButton.IsEnabled = false;
                    });
                }
            }
            m_lock.Release();

            // Update playback button state. Warning that this method acquires m_lock, so must be called from outside the lock
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => TogglePlaybackState(false));

            NotifyUser("Frame source configured, ready to begin");
        }

        /// <summary>
        /// Handle user selection of a camera as frame source
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
            GeneralTransform ge = CameraButton.TransformToVisual(null);
            Point point = ge.TransformPoint(new Point());
            Rect rect = new Rect(point, new Point(point.X + CameraButton.ActualWidth, point.Y + CameraButton.ActualHeight));

            DeviceInformation di = await devicePicker.PickSingleDeviceAsync(rect);
            if (di != null)
            {
                // Reset application state
                await m_lock.WaitAsync();
                {
                    m_bindings.Clear(); // Only need to clear bindings to stop skill executions
                }
                m_lock.Release();

                try
                {
                    NotifyUser("Attaching to camera " + di.Name, NotifyType.StatusMessage);

                    await ConfigureFrameSourceAsync(di);
                }
                catch (Exception ex)
                {
                    NotifyUser("Error occurred while initializating MediaCapture:\n" + ex.Message, NotifyType.ErrorMessage);
                }
            }

            // Re-enable the top menu once done handling the click
            await UpdateMediaSourceButtonsAsync(true);
        }

        /// <summary>
        /// Handle user selection of a video file as frame source
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
            picker.FileTypeFilter.Add(".avi");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                NotifyUser("Loading video file: " + file.Path);

                // Reset application state
                await m_lock.WaitAsync();
                {
                    m_bindings.Clear(); // Only need to clear bindings to stop skill executions
                }
                m_lock.Release();

                await ConfigureFrameSourceAsync(file);
            }

            // Re-enable the top menu once done handling the click
            await UpdateMediaSourceButtonsAsync(true);
        }

        /// <summary>
        /// Process drawn ink strokes into a bounding box to track
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UIUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            var inkStrokes = UIInkCanvasOverlay.InkPresenter.StrokeContainer.GetStrokes();
            // Ensure an ink stroke is present.
            if (inkStrokes.Count > 0)
            {
                m_inkAnalyzer.AddDataForStrokes(inkStrokes);

                // Only analyze as drawings
                foreach (var strokeNode in inkStrokes)
                {
                    m_inkAnalyzer.SetStrokeDataKind(strokeNode.Id, InkAnalysisStrokeKind.Drawing);
                }

                var inkAnalysisResults = await m_inkAnalyzer.AnalyzeAsync();

                // Have ink strokes on the canvas changed?
                if (inkAnalysisResults.Status == InkAnalysisStatus.Updated)
                {
                    // Find all strokes that are recognized as a drawing and 
                    // create a corresponding ink analysis InkDrawing node.
                    var inkdrawingNodes = m_inkAnalyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkDrawing);
                    // Draw recognized shapes on UICanvasOverlay and
                    // delete ink analysis data and recognized strokes.
                    List<Rect> drawnRects = new List<Rect>();
                    foreach (InkAnalysisInkDrawing node in inkdrawingNodes)
                    {
                        if (node.DrawingKind == InkAnalysisDrawingKind.Drawing)
                        {
                            // Catch and process unsupported shapes (lines and so on) here.
                            foreach (var strokeId in node.GetStrokeIds())
                            {
                                var stroke = UIInkCanvasOverlay.InkPresenter.StrokeContainer.GetStrokeById(strokeId);
                                var drawingAttributes = stroke.DrawingAttributes;
                                drawingAttributes.Color = Windows.UI.Colors.Red;
                                stroke.DrawingAttributes = drawingAttributes;
                            }
                        }
                        // Process generalized shapes here (ellipses and polygons).
                        else
                        {
                            // Normalized rect
                            var boundingRectNormalized = new Rect(node.BoundingRect.X / UIInkCanvasOverlay.ActualWidth,
                                                            node.BoundingRect.Y / UIInkCanvasOverlay.ActualHeight,
                                                            node.BoundingRect.Width / UIInkCanvasOverlay.ActualWidth,
                                                            node.BoundingRect.Height / UIInkCanvasOverlay.ActualHeight);
                            drawnRects.Add(boundingRectNormalized);

                            // Mark ink node strokes for deletion
                            foreach (var strokeId in node.GetStrokeIds())
                            {
                                var stroke = UIInkCanvasOverlay.InkPresenter.StrokeContainer.GetStrokeById(strokeId);
                                stroke.Selected = true;
                            }
                        }
                        m_inkAnalyzer.RemoveDataForStrokes(node.GetStrokeIds());
                    }
                    UIInkCanvasOverlay.InkPresenter.StrokeContainer.DeleteSelected();
                    
                    await m_bboxLock.WaitAsync();
                    {
                        m_drawnRects.AddRange(drawnRects);
                        m_objectTrackRenderer.RenderRects(m_drawnRects);
                    }
                    m_bboxLock.Release();
                }
            }
        }

        /// <summary>
        /// Clear drawn (unprocessed) ink
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UIClearInkButton_Click(object sender, RoutedEventArgs e)
        {
            UIInkCanvasOverlay.InkPresenter.StrokeContainer.Clear();
        }

        /// <summary>
        /// Clear all ink canvas controls (including active trackers)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UIResetButton_Click(object sender, RoutedEventArgs e)
        {
            await m_bboxLock.WaitAsync();
            {
                m_drawnRects.Clear();
            }
            m_bboxLock.Release();

            await m_lock.WaitAsync();
            {
                // Clear old tracker (binding) and create new binding
                m_bindings.Clear();
                m_trackerHistory.Clear();
            }
            m_lock.Release();

            // Clear UI
            UIInkCanvasOverlay.InkPresenter.StrokeContainer.Clear();
            m_objectTrackRenderer.ClearCanvas();
        }

        /// <summary>
        /// Enable/disable object tracker fallback search option
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void EnableExpandingSearchArea_Toggled(object sender, RoutedEventArgs e)
        {
            await m_lock.WaitAsync();
            {
                foreach (var binding in m_bindings)
                {
                    await binding.SetEnableExpandingSearchAreaAsync(EnableExpandingSearchArea.IsOn);
                }
            }
            m_lock.Release();
        }

        /// <summary>
        /// Toggle the playback state of the media source
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void TogglePlaybackState(bool? overridenState = null)
        {
            if (Dispatcher.HasThreadAccess)
            {
                await m_lock.WaitAsync();
                {
                    m_frameSourceIsStreaming = overridenState ?? !m_frameSourceIsStreaming;

                    if (m_frameSourceIsStreaming)
                    {
                        // Start frame source, update button to pause button
                        await m_frameSource?.StartAsync();
                        UIPlayButton.Icon = new SymbolIcon(Symbol.Pause);
                        UIPlayButton.Label = "Pause";
                    }
                    else
                    {
                        // Stop frame source, update button to play button
                        await m_frameSource?.StopAsync();
                        UIPlayButton.Icon = new SymbolIcon(Symbol.Play);
                        UIPlayButton.Label = "Play";
                    }
                }
                m_lock.Release();
            }
            else
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => TogglePlaybackState(overridenState));
            }
        }

        /// <summary>
        /// Show a help dialogue
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UIHelpButton_Click(object sender, RoutedEventArgs e)
        {
            await UIHelpDialog.ShowAsync();
        }

        /// <summary>
        /// Toggle playback state in response to UI button press
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UIPlayButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePlaybackState();
        }

        /// <summary>
        /// Reset skill to newly selected execution device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UISkillExecutionDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await m_lock.WaitAsync();
            {
                // Clear UI elements from previous tracker instance
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => m_objectTrackRenderer.ClearCanvas());

                // Load skill(s) with new config
                var selectedDevice = m_availableExecutionDevices[UISkillExecutionDevices.SelectedIndex];
                m_skill = await m_descriptor.CreateSkillAsync(selectedDevice) as ObjectTrackerSkill;
                m_bindings.Clear();
                m_trackerHistory.Clear();
            }
            m_lock.Release();
        }
    }

    /// <summary>
    /// Convenience class for rendering a rectangle on screen
    /// </summary>
    internal class ObjectTrackRenderer
    {
        private readonly SolidColorBrush successBrush = new SolidColorBrush(Windows.UI.Colors.Green);
        private readonly SolidColorBrush failBrush = new SolidColorBrush(Windows.UI.Colors.DarkOrange);
        private readonly SolidColorBrush newRectBrush = new SolidColorBrush(Windows.UI.Colors.Gold);
        private readonly double lineThickness = 2.0;
        private readonly SolidColorBrush fillBrush = new SolidColorBrush(Windows.UI.Colors.Transparent);
        private Canvas m_canvas;

        private readonly object m_lock = new object();

        /// <summary>
        /// ObjectTrackRenderer constructor
        /// </summary>
        /// <param name="canvas"></param>
        public ObjectTrackRenderer(Canvas canvas)
        {
            m_canvas = canvas;
            IsVisible = true;
        }

        /// <summary>
        /// Set visibility of ObjectTrackRenderer UI controls
        /// </summary>
        public bool IsVisible
        {
            get
            {
                return m_canvas.Visibility == Visibility.Visible;
            }
            set
            {
                m_canvas.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Clears the ObjectTrackRenderer canvas
        /// </summary>
        public void ClearCanvas()
        {
            lock (m_lock)
            {
                m_canvas.Children.Clear();
            }
        }

        /// <summary>
        /// Takes the webcam image and ObjectTracker results and assembles the visualization onto the Canvas.
        /// </summary>
        /// <param name="trackerResult"></param>
        /// <param name="showPaths"></param>
        public void RenderTrackerResults(IReadOnlyList<IReadOnlyList<TrackerResult>> histories, bool showPaths = false, int pathLength = 20)
        {
            double widthScaleFactor = m_canvas.ActualWidth;
            double heightScaleFactor = m_canvas.ActualHeight;

            lock (m_lock)
            {
                foreach (var history in histories)
                {
                    // Create a rectangle element for displaying the motion predictor box
                    Rect rect = history.Last().boundingRect;
                    Rectangle box = new Rectangle();
                    box.Width = rect.Width * widthScaleFactor;
                    box.Height = rect.Height * heightScaleFactor;
                    box.Fill = this.fillBrush;
                    box.Stroke = history.Last().succeeded ? this.successBrush : this.failBrush;
                    box.StrokeThickness = this.lineThickness;
                    box.Margin = new Thickness(
                        rect.X * widthScaleFactor,
                        rect.Y * heightScaleFactor,
                        0, 0);
                    m_canvas.Children.Add(box);

                    if (showPaths)
                    {
                        Polyline path = new Polyline();
                        // Draw pathLength most recent points in history
                        int i = Math.Max(history.Count - pathLength, 0);
                        while (i < history.Count)
                        {
                            Rect historyRect = history[i].boundingRect;
                            // Adjust values for scale
                            double pointX = (historyRect.X + historyRect.Width / 2) * widthScaleFactor;
                            double pointY = (historyRect.Y + historyRect.Height / 2) * heightScaleFactor;
                            path.Points.Add(new Point(pointX, pointY));

                            // Break up lines based on success status as necessary
                            if ((i + 1) < history.Count && history[i + 1].succeeded != history[i].succeeded)
                            {
                                // Commit/terminate current line
                                path.Stroke = history[i].succeeded ? this.successBrush : this.failBrush;
                                path.StrokeThickness = this.lineThickness;
                                m_canvas.Children.Add(path);

                                // Start new line
                                path = new Polyline();
                                path.Points.Add(new Point(pointX, pointY));
                            }

                            i++;
                        }
                        // Commit/terminate last line
                        path.Stroke = history.Last().succeeded ? this.successBrush : this.failBrush;
                        path.StrokeThickness = this.lineThickness;
                        m_canvas.Children.Add(path);
                    }
                }
            }
        }

        /// <summary>
        /// Renders a collection of Rects onto the Canvas
        /// </summary>
        /// <param name="rects"></param>
        public void RenderRects(IReadOnlyList<Rect> rects)
        {
            double widthScaleFactor = m_canvas.ActualWidth;
            double heightScaleFactor = m_canvas.ActualHeight;

            lock (m_lock)
            {
                foreach (var rect in rects)
                {
                    Rectangle box = new Rectangle();
                    box.Width = rect.Width * widthScaleFactor;
                    box.Height = rect.Height * heightScaleFactor;
                    box.Fill = this.fillBrush;
                    box.Stroke = this.newRectBrush;
                    box.StrokeThickness = this.lineThickness;
                    box.Margin = new Thickness(
                        rect.X * widthScaleFactor,
                        rect.Y * heightScaleFactor,
                        0, 0);
                    m_canvas.Children.Add(box);
                }
            }
        }
    }
}