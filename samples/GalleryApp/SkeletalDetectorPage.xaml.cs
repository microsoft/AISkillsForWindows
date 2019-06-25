using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Microsoft.AI.Skills.SkillInterfacePreview;
using Microsoft.AI.Skills.Vision.SkeletalDetectorPreview;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Media.Imaging;
using FrameSourceHelper_UWP;
using Windows.Devices.Enumeration;
using SkeletalDetectorSample;

namespace GalleryApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SkeletalDetectorPage : Page
    {
        private IFrameSource m_frameSource = null;

        // Skill-related variables
        private SkeletalDetectorSkill m_skill;
        private SkeletalDetectorBinding m_binding;
        private SkeletalDetectorDescriptor m_descriptor;

        // UI Related
        private BodyRenderer m_bodyRenderer;
        private IReadOnlyList<ISkillExecutionDevice> m_availableExecutionDevices;

        // Frames
        private SoftwareBitmapSource m_processedBitmapSource;

        // Synchronization
        private SemaphoreSlim m_lock = new SemaphoreSlim(1);

        // Performance metrics
        private Stopwatch m_evalPerfStopwatch = new Stopwatch();
        private float m_bindTime = 0;
        private float m_evalTime = 0;


        public SkeletalDetectorPage()
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
            // Initialize helper class used to render the skill results on screen
            m_bodyRenderer = new BodyRenderer(UICanvasOverlay);

            // Initialize skill-related instances and populate UI options
            m_lock.Wait();
            {
                NotifyUser("Initializing skill...");
                m_descriptor = new SkeletalDetectorDescriptor();
                m_availableExecutionDevices = await m_descriptor.GetSupportedExecutionDevicesAsync();

                await InitializeSkeletalDetectorAsync();
                await UpdateSkillUIAsync();
            }
            m_lock.Release();

            // Ready to begin, enable buttons
            NotifyUser("Skill initialized. Select a media source from the top to begin.");
        }

        /// <summary>
        /// Initialize the SkeletalDetector skill and binding instances
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        private async Task InitializeSkeletalDetectorAsync(ISkillExecutionDevice device = null)
        {
            if (device != null)
            {
                m_skill = await m_descriptor.CreateSkillAsync(device) as SkeletalDetectorSkill;
            }
            else
            {
                m_skill = await m_descriptor.CreateSkillAsync() as SkeletalDetectorSkill;
            }
            m_binding = await m_skill.CreateSkillBindingAsync() as SkeletalDetectorBinding;
        }

        /// <summary>
        /// Run the skill against the frame passed as parameter
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        private async Task RunSkillAsync(VideoFrame frame)
        {
            m_evalPerfStopwatch.Restart();

            // Update bound input image
            await m_binding.SetInputImageAsync(frame);

            m_bindTime = (float)m_evalPerfStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000f;
            m_evalPerfStopwatch.Restart();

            // Run the skill against the binding
            await m_skill.EvaluateAsync(m_binding);

            m_evalTime = (float)m_evalPerfStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000f;
            m_evalPerfStopwatch.Stop();
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

                var outputDesc1 = m_descriptor.OutputFeatureDescriptors[0] as SkeletalDetectorResultListDescriptor;
                UISkillOutputDescription1.Text = $"\tName: {outputDesc1.Name}, Description: {outputDesc1.Description} \n\tType: Custom";

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
            }
            else
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => await UpdateSkillUIAsync());
            }
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
                UIImageViewer.Source = null;
                m_processedBitmapSource = new SoftwareBitmapSource();
                UIImageViewer.Source = m_processedBitmapSource;
                m_bodyRenderer.IsVisible = false;

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
                m_frameSource = await FrameSourceFactory.CreateFrameSourceAsync(source, (sender, message) =>
                {
                    NotifyUser(message);
                });

            }
            m_lock.Release();

            RunSkill_Execution();
        }


        /// <summary>
        /// If a valid frame source is obtained, run skill on it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RunSkill_Execution()
        {
            if (m_frameSource != null)
            {
                m_frameSource.FrameArrived += FrameSource_FrameAvailable;
                await m_frameSource.StartAsync();
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
                        await RunSkillAsync(frame);
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
        /// Display a frame and the evaluation results on the UI
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        private async Task DisplayFrameAndResultAsync(VideoFrame frame)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                try
                {
                    // Enable results to be displayed
                    m_bodyRenderer.IsVisible = true;

                    // Display the input frame
                    if (frame.SoftwareBitmap != null)
                    {
                        await m_processedBitmapSource.SetBitmapAsync(frame.SoftwareBitmap);
                    }
                    else
                    {
                        var bitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Direct3DSurface, BitmapAlphaMode.Ignore);
                        await m_processedBitmapSource.SetBitmapAsync(bitmap);
                    }

                    // If our canvas overlay is properly resized, update displayed results
                    if (UICanvasOverlay.ActualWidth != 0)
                    {
                        m_bodyRenderer.Update(m_binding.Bodies, m_frameSource.FrameSourceType != FrameSourceType.Camera);
                    }

                    // Output result and perf text
                    UISkillOutputDetails.Text = $"Found {m_binding.Bodies.Count} bodies (bind: {m_bindTime.ToString("F2")}ms, eval: {m_evalTime.ToString("F2")}ms";
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
        /// Triggered when the execution device selected changes. We simply retrigger the image source toggle to reinitialize the skill accordingly. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UISkillExecutionDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedDevice = m_availableExecutionDevices[UISkillExecutionDevices.SelectedIndex];
            await m_lock.WaitAsync();
            {
                await InitializeSkeletalDetectorAsync(selectedDevice);
            }
            m_lock.Release();
            if (m_frameSource != null)
            {
                await m_frameSource.StartAsync();
            }
        }

        /// <summary>
        /// Triggered when the image control is resized, making sure the canvas size stays in sync with the frame display control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UIImageViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Make sure the aspect ratio is honored when rendering the body limbs
            float cameraAspectRatio = (float)m_frameSource.FrameWidth / m_frameSource.FrameHeight;
            float previewAspectRatio = (float)(UIImageViewer.ActualWidth / UIImageViewer.ActualHeight);
            UICanvasOverlay.Width = cameraAspectRatio >= previewAspectRatio ? UIImageViewer.ActualWidth : UIImageViewer.ActualHeight * cameraAspectRatio;
            UICanvasOverlay.Height = cameraAspectRatio >= previewAspectRatio ? UIImageViewer.ActualWidth / cameraAspectRatio : UIImageViewer.ActualHeight;

            m_bodyRenderer.Update(m_binding.Bodies, m_frameSource.FrameSourceType != FrameSourceType.Camera);
        }
    }
}
