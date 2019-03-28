// Copyright (C) Microsoft Corporation. All rights reserved.

using Microsoft.AI.Skills.SkillInterfacePreview;
using Microsoft.AI.Skills.Vision.SkeletalDetectorPreview;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace SkeletalDetectorSample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Skill-related variables
        private SkeletalDetectorSkill m_skeletalDetectorSkill;
        private SkeletalDetectorBinding m_skeletalDetectorBinding;
        private SkeletalDetectorDescriptor m_skeletalDetectorDescriptor;

        // UI Related
        private BodyRenderer m_bodyRenderer;
        private IReadOnlyList<ISkillExecutionDevice> m_availableExecutionDevices;
        private uint m_cameraFrameWidth, m_cameraFrameHeight;
        private bool m_isCameraFrameDimensionInitialized = false;
        private enum FrameSourceToggledType { None, ImageFile, Camera, Capture };
        private FrameSourceToggledType m_currentFrameSourceToggled = FrameSourceToggledType.None;

        // Synchronization
        private SemaphoreSlim m_lock = new SemaphoreSlim(1);

        private VideoFrame m_cachedFrame = null;

        //Debug
        private Stopwatch m_evalPerfStopwatch = new Stopwatch();
        private long m_skeletalDetectionRunTime = 0;

        /// <summary>
        /// MainPage constructor
        /// </summary
        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Triggered after the page has loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize helper class used to render the skill results on screen
            m_bodyRenderer = new BodyRenderer(UICanvasOverlay);

            await Task.Run(async () =>
            {
                try
                {
                    m_skeletalDetectorDescriptor = new SkeletalDetectorDescriptor();
                    m_availableExecutionDevices = await m_skeletalDetectorDescriptor.GetSupportedExecutionDevicesAsync();

                    // Refresh UI
                    await Dispatcher.RunAsync(
                        Windows.UI.Core.CoreDispatcherPriority.Normal,
                        () =>
                        {
                            // Show skill description members in UI
                            UISkillName.Text = m_skeletalDetectorDescriptor.Name;

                            UISkillDescription.Text = $"{m_skeletalDetectorDescriptor.Description}" +
                            $"\n\tauthored by: {m_skeletalDetectorDescriptor.Version.Author}" +
                            $"\n\tpublished by: {m_skeletalDetectorDescriptor.Version.Author}" +
                            $"\n\tversion: {m_skeletalDetectorDescriptor.Version.Major}.{m_skeletalDetectorDescriptor.Version.Minor}" +
                            $"\n\tunique ID: {m_skeletalDetectorDescriptor.Id}";

                            var inputDesc = m_skeletalDetectorDescriptor.InputFeatureDescriptors[0] as SkillFeatureImageDescriptor;
                            UISkillInputDescription.Text = $"\tName: {inputDesc.Name}" +
                            $"\n\tDescription: {inputDesc.Description}" +
                            $"\n\tType: {inputDesc.FeatureKind}" +
                            $"\n\tWidth: {inputDesc.Width}" +
                            $"\n\tHeight: {inputDesc.Height}" +
                            $"\n\tSupportedBitmapPixelFormat: {inputDesc.SupportedBitmapPixelFormat}" +
                            $"\n\tSupportedBitmapAlphaMode: {inputDesc.SupportedBitmapAlphaMode}";

                            var outputDesc1 = m_skeletalDetectorDescriptor.OutputFeatureDescriptors[0] as SkeletalDetectorResultListDescriptor;
                            UISkillOutputDescription1.Text = $"\tName: {outputDesc1.Name}, Description: {outputDesc1.Description} \n\tType: Custom";

                            if (m_availableExecutionDevices.Count == 0)
                            {
                                UISkillOutputDetails.Text = "No execution devices available, this skill cannot run on this device";
                            }
                            else
                            {
                                // Display available execution devices
                                UISkillExecutionDevices.ItemsSource = m_availableExecutionDevices.Select((device) => device.Name);
                                UISkillExecutionDevices.SelectedIndex = 0;

                                // Alow user to interact with the app
                                UIButtonFilePick.IsEnabled = true;
                                UICameraToggle.IsEnabled = true;
                                UIButtonFilePick.Focus(FocusState.Keyboard);
                            }
                        });
                }
                catch (Exception ex)
                {
                    await new MessageDialog(ex.Message).ShowAsync();
                }
            });

            // Register callback for if camera preview encounters an issue
            UICameraPreview.PreviewFailed += UICameraPreview_PreviewFailed;
        }

        /// <summary>
        /// Triggered when UIButtonFilePick is clicked, grabs a frame from an image file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UIButtonFilePick_Click(object sender, RoutedEventArgs e)
        {
            // Stop Camera preview
            UICameraPreview.Stop();
            if (UICameraPreview.CameraHelper != null)
            {
                await UICameraPreview.CameraHelper.CleanUpAsync();
            }
            UICameraPreview.Visibility = Visibility.Collapsed;
            UIImageViewer.Visibility = Visibility.Visible;

            // Disable subsequent trigger of this event callback 
            UICameraToggle.IsEnabled = false;
            UIButtonFilePick.IsEnabled = false;

            await m_lock.WaitAsync();

            try
            {
                // Initialize skill with the selected supported device
                m_skeletalDetectorSkill = await m_skeletalDetectorDescriptor.CreateSkillAsync(m_availableExecutionDevices[UISkillExecutionDevices.SelectedIndex]) as SkeletalDetectorSkill;

                // Instantiate a binding object that will hold the skill's input and output resource
                m_skeletalDetectorBinding = await m_skeletalDetectorSkill.CreateSkillBindingAsync() as SkeletalDetectorBinding;

                var frame = await LoadVideoFrameFromFilePickedAsync();
                if (frame != null)
                {
                    SoftwareBitmapSource source = new SoftwareBitmapSource();
                    await source.SetBitmapAsync(frame.SoftwareBitmap);
                    UIImageViewer.Source = source;
                    UIImageViewer_SizeChanged(null, null);

                    await RunSkillAsync(frame, false);
                }

                m_skeletalDetectorSkill = null;
                m_skeletalDetectorBinding = null;

                m_currentFrameSourceToggled = FrameSourceToggledType.ImageFile;
            }
            catch (Exception ex)
            {
                await (new MessageDialog(ex.Message)).ShowAsync();
                m_currentFrameSourceToggled = FrameSourceToggledType.None;
            }

            m_lock.Release();

            // Enable subsequent trigger of this event callback
            UIButtonFilePick.IsEnabled = true;
            UICameraToggle.IsEnabled = true;
        }

        /// <summary>
        /// Launch file picker for user to select a picture file and return a VideoFrame
        /// </summary>
        /// <returns>VideoFrame instanciated from the selected image file</returns>
        public static IAsyncOperation<VideoFrame> LoadVideoFrameFromFilePickedAsync()
        {
            return AsyncInfo.Run(async (token) =>
            {
                // Trigger file picker to select an image file
                FileOpenPicker fileOpenPicker = new FileOpenPicker();
                fileOpenPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                fileOpenPicker.FileTypeFilter.Add(".jpg");
                fileOpenPicker.FileTypeFilter.Add(".png");
                fileOpenPicker.ViewMode = PickerViewMode.Thumbnail;
                StorageFile selectedStorageFile = await fileOpenPicker.PickSingleFileAsync();

                if (selectedStorageFile == null)
                {
                    return null;
                }

                // Decoding image file content into a SoftwareBitmap, and wrap into VideoFrame
                VideoFrame resultFrame = null;
                SoftwareBitmap softwareBitmap = null;
                using (IRandomAccessStream stream = await selectedStorageFile.OpenAsync(FileAccessMode.Read))
                {
                    // Create the decoder from the stream 
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                    // Get the SoftwareBitmap representation of the file in BGRA8 format
                    softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                    // Convert to friendly format for UI display purpose
                    softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }

                // Encapsulate the image in a VideoFrame instance
                resultFrame = VideoFrame.CreateWithSoftwareBitmap(softwareBitmap);

                return resultFrame;
            });
        }


        /// <summary>
        /// Triggered when UICameraToggle is clicked, initializes frame grabbing from the camera stream
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UICameraToggle_Click(object sender, RoutedEventArgs e)
        {
            await m_lock.WaitAsync();
            try
            {
                UICameraPreview.Stop();
                if (UICameraPreview.CameraHelper != null)
                {
                    await UICameraPreview.CameraHelper.CleanUpAsync();
                }
                m_isCameraFrameDimensionInitialized = false;

                // Initialize skill with the selected supported device
                m_skeletalDetectorSkill = await m_skeletalDetectorDescriptor.CreateSkillAsync(m_availableExecutionDevices[UISkillExecutionDevices.SelectedIndex]) as SkeletalDetectorSkill;

                // Initialize the CameraPreview control, register frame arrived event callback
                UIImageViewer.Visibility = Visibility.Collapsed;
                UICameraPreview.Visibility = Visibility.Visible;
                await UICameraPreview.StartAsync();

                UICameraPreview.CameraHelper.FrameArrived += CameraHelper_FrameArrived;
                m_currentFrameSourceToggled = FrameSourceToggledType.Camera;

                UIButtonCamera.IsEnabled = true;
                UIButtonCamera.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                await (new MessageDialog(ex.Message)).ShowAsync();
                m_currentFrameSourceToggled = FrameSourceToggledType.None;
            }
            finally
            {
                m_lock.Release();
            }
        }

        /// <summary>
        /// Triggered when a new frame is available from the camera stream.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CameraHelper_FrameArrived(object sender, FrameEventArgs e)
        {
            try
            {
                // Use a lock to process frames one at a time and bypass processing if busy
                if (m_lock.Wait(0))
                {
                    uint cameraFrameWidth = UICameraPreview.CameraHelper.PreviewFrameSource.CurrentFormat.VideoFormat.Width;
                    uint cameraFrameHeight = UICameraPreview.CameraHelper.PreviewFrameSource.CurrentFormat.VideoFormat.Height;

                    // Allign overlay canvas and camera preview so that face detection rectangle looks right
                    if (!m_isCameraFrameDimensionInitialized || cameraFrameWidth != m_cameraFrameWidth || cameraFrameHeight != m_cameraFrameHeight)
                    {
                        // Can't bind frames of different sizes to same binding.
                        // As a workaround, recreate the binding for each eval if framesize changed.
                        m_skeletalDetectorBinding = await m_skeletalDetectorSkill.CreateSkillBindingAsync() as SkeletalDetectorBinding;

                        m_cameraFrameWidth = UICameraPreview.CameraHelper.PreviewFrameSource.CurrentFormat.VideoFormat.Width;
                        m_cameraFrameHeight = UICameraPreview.CameraHelper.PreviewFrameSource.CurrentFormat.VideoFormat.Height;

                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            UIImageViewer_SizeChanged(null, null);
                        });

                        m_isCameraFrameDimensionInitialized = true;
                    }

                    // Run the skill against the frame
                    SoftwareBitmap copyBitmap = SoftwareBitmap.Convert(e.VideoFrame.SoftwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                    m_cachedFrame = VideoFrame.CreateWithSoftwareBitmap(copyBitmap);
                    await RunSkillAsync(m_cachedFrame, true);
                    m_lock.Release();
                }
                e.VideoFrame.Dispose();
            }
            catch (Exception ex)
            {
                // Show the error
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => UISkillOutputDetails.Text = ex.Message);
                m_lock.Release();
            }
        }

        /// <summary>
        /// Triggered when something wrong happens with the camera preview control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UICameraPreview_PreviewFailed(object sender, PreviewFailedEventArgs e)
        {
            await new MessageDialog(e.Error).ShowAsync();
        }

        /// <summary>
        /// Run the skill against the frame passed as parameter. This skill might run on a video feed or 
        /// a static image. Indicate this in the second parameter as a way to tell the renderer to not 
        /// render the joint tooltips on a video. 
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="isStream">indicates whether the frame input is part of a stream</param>
        /// <returns></returns>
        private async Task RunSkillAsync(VideoFrame frame, bool isStream)
        {
            m_evalPerfStopwatch.Restart();

            // Update input image and run the skill against it
            await m_skeletalDetectorBinding.SetInputImageAsync(frame);
            await m_skeletalDetectorSkill.EvaluateAsync(m_skeletalDetectorBinding);

            m_evalPerfStopwatch.Stop();
            m_skeletalDetectionRunTime = m_evalPerfStopwatch.ElapsedMilliseconds;

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                m_bodyRenderer.Update(m_skeletalDetectorBinding.Bodies, !isStream);
                m_bodyRenderer.IsVisible = true;
                UISkillOutputDetails.Text = $"Found {m_skeletalDetectorBinding.Bodies.Count} bodies (took {m_skeletalDetectionRunTime} ms)";
            });
        }

        /// <summary>
        /// Triggered when the execution device selected changes. We simply retrigger the image source toggle to reinitialize the skill accordingly. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UISkillExecutionDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (m_currentFrameSourceToggled)
            {
                case FrameSourceToggledType.ImageFile:
                    UIButtonFilePick_Click(null, null);
                    break;
                case FrameSourceToggledType.Camera:
                    UICameraToggle_Click(null, null);
                    break;
                case FrameSourceToggledType.Capture:
                    UIButtonCamera_Click(null, null);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Triggers when the image control is resized, makes sure the canvas size stays in sync with the frame display control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UIImageViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (UIImageViewer.Visibility == Visibility.Visible) // we are using an image file that we stretch, match UI control dimension
            {
                UICanvasOverlay.Width = UIImageViewer.ActualWidth;
                UICanvasOverlay.Height = UIImageViewer.ActualHeight;
            }
            else // we are using a camera preview, make sure the aspect ratio is honored when rendering the face rectangle
            {
                float aspectRatio = (float)m_cameraFrameWidth / m_cameraFrameHeight;
                UICanvasOverlay.Width = aspectRatio >= 1.0f ? UICameraPreview.ActualWidth : UICameraPreview.ActualWidth * aspectRatio;
                UICanvasOverlay.Height = aspectRatio >= 1.0f ? UICameraPreview.ActualHeight / aspectRatio : UICameraPreview.ActualHeight;
            }
        }

        /// <summary>
        /// Triggers when the camera button is clicked, results in a still of the preview frame at the moment it was taken. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UIButtonCamera_Click(object sender, RoutedEventArgs e)
        {
            // Stop Camera preview
            UICameraPreview.Stop();
            if (UICameraPreview.CameraHelper != null)
            {
                await UICameraPreview.CameraHelper.CleanUpAsync();
            }
            UICameraPreview.Visibility = Visibility.Collapsed;
            UIImageViewer.Visibility = Visibility.Visible;

            // Disable subsequent trigger of this event callback 
            UICameraToggle.IsEnabled = false;
            UIButtonFilePick.IsEnabled = false;
            UIButtonCamera.IsEnabled = false;

            await m_lock.WaitAsync();

            try
            {
                var frame = m_cachedFrame;
                if (frame != null)
                {
                    SoftwareBitmapSource source = new SoftwareBitmapSource();
                    await source.SetBitmapAsync(frame.SoftwareBitmap);
                    UIImageViewer.Source = source;
                    UIImageViewer_SizeChanged(null, null);
                    UIImageViewer.UpdateLayout();

                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        m_bodyRenderer.Update(m_skeletalDetectorBinding.Bodies, true);
                        m_bodyRenderer.IsVisible = true;
                        UISkillOutputDetails.Text = $"Found {m_skeletalDetectorBinding.Bodies.Count} bodies";
                    });
                }

                m_currentFrameSourceToggled = FrameSourceToggledType.Capture;
            }
            catch (Exception ex)
            {
                await (new MessageDialog(ex.Message)).ShowAsync();
                m_currentFrameSourceToggled = FrameSourceToggledType.None;
            }

            m_lock.Release();

            // Enable subsequent trigger of this event callback
            UIButtonFilePick.IsEnabled = true;
            UICameraToggle.IsEnabled = true;
        }

        /// <summary>
        /// Convenience class for rendering skeletons on screen
        /// </summary>    
        internal class BodyRenderer
        {
            protected Canvas m_canvas;

            protected const int LINES_THICKNESS = 3;
            protected static SolidColorBrush[] m_colorBrushes = new SolidColorBrush[7]
            {
                new SolidColorBrush(Colors.Red),
                new SolidColorBrush(Colors.Yellow),
                new SolidColorBrush(Colors.Green),
                new SolidColorBrush(Colors.Blue),
                new SolidColorBrush(Colors.Violet),
                new SolidColorBrush(Colors.Gray),
                new SolidColorBrush(Colors.Brown),
            };

            public BodyRenderer(Canvas canvas)
            {
                m_canvas = canvas;
                m_canvas.Children.Add(new Line());
                IsVisible = false;
            }

            /// <summary>
            /// Updates the skeletal stick figure renderer. A secondary parameter may be passed in 
            /// to render tooltips for joint labels. 
            /// </summary>
            /// <param name="bodies"></param>
            /// <param name="enableLabels"></param>
            public void Update(IReadOnlyList<SkeletalDetectorResult> bodies, bool enableLabels = false)
            {
                m_canvas.Children.Clear();
                for (int i = 0; i < bodies.Count; ++i)
                {
                    Dictionary<string, Joint> map = new Dictionary<string, Joint>();

                    var body = bodies.ElementAt(i);
                    foreach (var limb in body.Limbs)
                    {
                        var line = new Line() { Stroke = m_colorBrushes[i % m_colorBrushes.Count()], StrokeThickness = LINES_THICKNESS };
                        var point1 = limb.joint1;
                        var point2 = limb.joint2;
                        line.X1 = 0.0f;
                        line.Y1 = 0.0f;
                        line.X2 = (point2.X - point1.X) * m_canvas.Width;
                        line.Y2 = (point2.Y - point1.Y) * m_canvas.Height;
                        m_canvas.Children.Add(line);
                        Canvas.SetLeft(line, point1.X * m_canvas.Width);
                        Canvas.SetTop(line, point1.Y * m_canvas.Height);

                        map[point1.Label.ToString()] = point1;
                        map[point2.Label.ToString()] = point2;
                    }

                    if (enableLabels)
                    {
                        foreach (KeyValuePair<string, Joint> pair in map)
                        {
                            Ellipse point = new Ellipse()
                            {
                                Stroke = m_colorBrushes[i % m_colorBrushes.Count()],
                                Fill = m_colorBrushes[i % m_colorBrushes.Count()],
                                StrokeThickness = LINES_THICKNESS,
                                Height = 10,
                                Width = 10,
                            };
                            ToolTip toolTip = new ToolTip();
                            toolTip.Content = pair.Key;
                            ToolTipService.SetToolTip(point, toolTip);
                            m_canvas.Children.Add(point);
                            Canvas.SetLeft(point, pair.Value.X * m_canvas.Width - point.Width / 2);
                            Canvas.SetTop(point, pair.Value.Y * m_canvas.Height - point.Height / 2);
                        }
                    }
                }
            }

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
        }
    }
}
