using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.AI.Skills.SkillInterfacePreview;
using Microsoft.AI.Skills.Vision.ObjectDetectorPreview;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Graphics.Imaging;
using Windows.Devices.Enumeration;
using Microsoft.Toolkit.Uwp.UI.Controls;
using FrameSourceHelper_UWP;
using ObjectDetectorSkillSample;
using System.Diagnostics;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace GalleryApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ObjectDetectorPage : Page
    {
        private IFrameSource m_frameSource = null;

        // Vision Skills
        private ObjectDetectorDescriptor m_descriptor = null;
        private ObjectDetectorBinding m_binding = null;
        private ObjectDetectorSkill m_skill = null;
        private IReadOnlyList<ISkillExecutionDevice> m_availableExecutionDevices = null;

        // Misc
        private BoundingBoxRenderer m_bboxRenderer = null;
        private HashSet<ObjectKind> m_objectKinds = null;

        // Frames
        private SoftwareBitmapSource m_processedBitmapSource;

        // Performance metrics
        private Stopwatch m_evalStopwatch = new Stopwatch();
        private float m_bindTime = 0;
        private float m_evalTime = 0;
        private Stopwatch m_renderStopwatch = new Stopwatch();

        // Locks
        private SemaphoreSlim m_lock = new SemaphoreSlim(1);

        public ObjectDetectorPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Backward navigation to MainPage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
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
                m_descriptor = new ObjectDetectorDescriptor();
                m_availableExecutionDevices = await m_descriptor.GetSupportedExecutionDevicesAsync();

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
        /// Bind and evaluate the frame with the ObjectDetector skill
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        private async Task DetectObjectsAsync(VideoFrame frame)
        {
            m_evalStopwatch.Restart();

            // Bind
            await m_binding.SetInputImageAsync(frame);

            m_bindTime = (float)m_evalStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000f;
            m_evalStopwatch.Restart();

            // Evaluate
            await m_skill.EvaluateAsync(m_binding);

            m_evalTime = (float)m_evalStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000f;
            m_evalStopwatch.Stop();
        }

        /// <summary>
        /// Render ObjectDetector skill results
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="objectDetections"></param>
        /// <returns></returns>
        private async Task DisplayFrameAndResultAsync(VideoFrame frame)
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

                    // Retrieve and filter results if requested
                    IReadOnlyList<ObjectDetectorResult> objectDetections = m_binding.DetectedObjects;
                    if (m_objectKinds?.Count > 0)
                    {
                        objectDetections = objectDetections.Where(det => m_objectKinds.Contains(det.Kind)).ToList();
                    }

                    // Update displayed results
                    m_bboxRenderer.Render(objectDetections);

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
        /// Configure an IFrameSource from a StorageFile or MediaCapture instance
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private async Task ConfigureFrameSourceAsync(object source)
        {
            await m_lock.WaitAsync();
            {
                // Reset bitmap rendering component
                UIProcessedPreview.Source = null;
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

                // Create new frame source and rgister a callback if the source fails along the way
                m_frameSource = await FrameSourceFactory.CreateFrameSourceAsync(source, (sender, message) =>
                {
                    NotifyUser(message);
                });

                // TODO: Workaround for a bug in ObjectDetectorBinding when binding consecutively VideoFrames with Direct3DSurface and SoftwareBitmap
                m_binding = await m_skill.CreateSkillBindingAsync() as ObjectDetectorBinding;
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
                        await DetectObjectsAsync(frame);
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
                NotifyUser("Loading file: " + file.Path);
                await ConfigureFrameSourceAsync(file);
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
                    await ConfigureFrameSourceAsync(di);
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

            m_bboxRenderer.ResizeContent(e);
        }

        /// <summary>
        /// Triggered when the expander is expanded and collapsed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UIExpander_Expanded(object sender, EventArgs e)
        {
            var expander = (sender as Expander);
            if (expander.IsExpanded)
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
