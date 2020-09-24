// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Contoso.FaceSentimentAnalyzer;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI;
using Windows.UI.Xaml.Shapes;
using Microsoft.AI.Skills.SkillInterface;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Toolkit.Uwp.Helpers;
using System.Threading;
using System.Diagnostics;

namespace FaceSentimentAnalysisTestApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Skill-related variables
        private FaceSentimentAnalyzerDescriptor m_skillDescriptor = null;
        private FaceSentimentAnalyzerSkill m_skill = null;
        private FaceSentimentAnalyzerBinding m_binding = null;

        // UI-related variables
        private SoftwareBitmapSource m_bitmapSource = new SoftwareBitmapSource(); // used to render an image from a file
        private FaceSentimentRenderer m_faceSentimentRenderer = null; // used to render a face rectangle on top of an iamge
        private IReadOnlyList<ISkillExecutionDevice> m_availableExecutionDevices = null;
        private uint m_cameraFrameWidth, m_cameraFrameHeight;
        private bool m_isCameraFrameDimensionInitialized = false;
        private enum FrameSourceToggledType { None, ImageFile, Camera};
        private FrameSourceToggledType m_currentFrameSourceToggled = FrameSourceToggledType.None;

        private Stopwatch m_perfStopwatch = new Stopwatch();

        // Synchronization
        private SemaphoreSlim m_lock = new SemaphoreSlim(1);

        /// <summary>
        /// MainPage constructor
        /// </summary>
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
            m_faceSentimentRenderer = new FaceSentimentRenderer(UICanvasOverlay);

            try
            {
                // Instatiate skill descriptor to display details about the skill and populate UI
                m_skillDescriptor = new FaceSentimentAnalyzerDescriptor();
                m_availableExecutionDevices = await m_skillDescriptor.GetSupportedExecutionDevicesAsync();

                // Show skill description members in UI
                UISkillName.Text = m_skillDescriptor.Information.Name;

                UISkillDescription.Text = SkillHelper.SkillHelperMethods.GetSkillDescriptorString(m_skillDescriptor);

                int featureIndex = 0;
                foreach (var featureDesc in m_skillDescriptor.InputFeatureDescriptors)
                {
                    UISkillInputDescription.Text += SkillHelper.SkillHelperMethods.GetSkillFeatureDescriptorString(featureDesc);
                    if (featureIndex++ < m_skillDescriptor.InputFeatureDescriptors.Count - 1)
                    {
                        UISkillInputDescription.Text += "\n----\n";
                    }
                }

                featureIndex = 0;
                foreach (var featureDesc in m_skillDescriptor.OutputFeatureDescriptors)
                {
                    UISkillOutputDescription.Text += SkillHelper.SkillHelperMethods.GetSkillFeatureDescriptorString(featureDesc);
                    if (featureIndex++ < m_skillDescriptor.OutputFeatureDescriptors.Count - 1)
                    {
                        UISkillOutputDescription.Text += "\n----\n";
                    }
                }

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
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message).ShowAsync();
            }

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
                m_skill = await m_skillDescriptor.CreateSkillAsync(m_availableExecutionDevices[UISkillExecutionDevices.SelectedIndex]) as FaceSentimentAnalyzerSkill;

                // Instantiate a binding object that will hold the skill's input and output resource
                m_binding = await m_skill.CreateSkillBindingAsync() as FaceSentimentAnalyzerBinding;

                var frame = await LoadVideoFrameFromFilePickedAsync();
                if (frame != null)
                {
                    await m_bitmapSource.SetBitmapAsync(frame.SoftwareBitmap);
                    UIImageViewer.Source = m_bitmapSource;

                    UIImageViewer_SizeChanged(null, null);

                    await RunSkillAsync(frame);
                }

                m_skill = null;
                m_binding = null;

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
        /// Launch file picker for user to select a picture file and return a VideoFrame.
        /// </summary>
        /// <returns>VideoFrame instantiated from the selected image file</returns>
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
                m_skill = await m_skillDescriptor.CreateSkillAsync(m_availableExecutionDevices[UISkillExecutionDevices.SelectedIndex]) as FaceSentimentAnalyzerSkill;

                // Instantiate a binding object that will hold the skill's input and output resource
                m_binding = await m_skill.CreateSkillBindingAsync() as FaceSentimentAnalyzerBinding;

                // Initialize the CameraPreview control, register frame arrived event callback
                UIImageViewer.Visibility = Visibility.Collapsed;
                UICameraPreview.Visibility = Visibility.Visible;
                await UICameraPreview.StartAsync();

                UICameraPreview.CameraHelper.FrameArrived += CameraHelper_FrameArrived;
                m_currentFrameSourceToggled = FrameSourceToggledType.Camera;
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
                        m_cameraFrameWidth = UICameraPreview.CameraHelper.PreviewFrameSource.CurrentFormat.VideoFormat.Width;
                        m_cameraFrameHeight = UICameraPreview.CameraHelper.PreviewFrameSource.CurrentFormat.VideoFormat.Height;

                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            UIImageViewer_SizeChanged(null, null);
                        });

                        m_isCameraFrameDimensionInitialized = true;
                    }

                    // Run the skill against the frame
                    await RunSkillAsync(e.VideoFrame);
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
        /// Run the skill against the frame passed as parameter
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        private async Task RunSkillAsync(VideoFrame frame)
        {
            m_perfStopwatch.Restart();
            // Update input image and run the skill against it
            await m_binding.SetInputImageAsync(frame);
            var bindTime = (float)m_perfStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000F;

            await m_skill.EvaluateAsync(m_binding);
            var evalTime = (float)m_perfStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000F - bindTime;
            m_perfStopwatch.Stop();

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                // Retrieve result
                if (!m_binding.IsFaceFound)
                {
                    // if no face found, hide the rectangle in the UI
                    m_faceSentimentRenderer.IsVisible = false;
                    UISkillOutputDetails.Text = "No face found";
                }
                else // Display the face rectangle and sentiment in the UI
                {
                    var rawScores = (m_binding["FaceSentimentsScores"].FeatureValue as SkillFeatureTensorFloatValue).GetAsVectorView();
                    m_faceSentimentRenderer.Update(m_binding.FaceBoundingBoxes, m_binding.PredominantSentiments, rawScores);
                    m_faceSentimentRenderer.IsVisible = true;
                    UISkillOutputDetails.Text = $"Bind: {bindTime.ToString("F2")}ms | Eval: {evalTime.ToString("F2")}";
                }
            });
        }

        /// <summary>
        /// Triggered when the execution device selected changes. We simply retrigger the image source toggle to reinitialize the skill accordingly. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UISkillExecutionDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch(m_currentFrameSourceToggled)
            {
                case FrameSourceToggledType.ImageFile:
                    UIButtonFilePick_Click(null, null);
                    break;
                case FrameSourceToggledType.Camera:
                    UICameraToggle_Click(null, null);
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
                UICanvasOverlay.Width = aspectRatio >= 1.0f ? UICameraPreview.ActualWidth : UICameraPreview.ActualHeight * aspectRatio;
                UICanvasOverlay.Height = aspectRatio >= 1.0f ? UICameraPreview.ActualWidth / aspectRatio : UICameraPreview.ActualHeight;
            }
        }
    }

    /// <summary>
    /// Convenience class for rendering a rectangle on screen
    /// </summary>
    internal class FaceSentimentRenderer
    {
        private Canvas m_canvas;
        private List<TextBlock> m_sentimentControls = new List<TextBlock>();
        private List<Rectangle> m_rectangles = new List<Rectangle>();
        private List<ToolTip> m_controlToolTips = new List<ToolTip>();
        private Dictionary<SentimentType, string> m_emojis = new Dictionary<SentimentType, string>
        {
            { SentimentType.neutral, "😒" },
            { SentimentType.happiness, "😄" },
            { SentimentType.surprise, "😲" },
            { SentimentType.sadness, "😢" },
            { SentimentType.anger, "😡" },
            { SentimentType.disgust, "😝" },
            { SentimentType.fear, "😱" },
            { SentimentType.contempt, "😤" }
        };
        private IReadOnlyList<string> m_sentimentTypesStrings = Enum.GetNames(typeof(SentimentType));

        /// <summary>
        /// FaceSentimentRenderer constructor
        /// </summary>
        /// <param name="canvas"></param>
        public FaceSentimentRenderer(Canvas canvas)
        {
            m_canvas = canvas;
            IsVisible = false;
        }

        /// <summary>
        /// Set visibility of FaceSentimentRendere UI controls
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
        /// Update coordinates of face bounding boxes and predominant sentiments passed as parameter
        /// </summary>
        /// <param name="coordinates"></param>
        public void Update(IReadOnlyList<float> bounds, IReadOnlyList<SentimentType> sentiments, IReadOnlyList<float> sentimentScores)
        {
            if (bounds == null || bounds.Count == 0)
            {
                return;
            }
            int sentimentTypeOffset = m_emojis.Count;
            if (bounds.Count / 4 != sentiments.Count 
                || sentimentScores.Count / sentimentTypeOffset != sentiments.Count)
            {
                throw new Exception("Must supply a matching count of bounds, sentiments and set of sentiment scores");
            }

            // Make sure we have enough UI controls available to draw around each face
            int missingCanvasRectangleCount = bounds.Count / 4 - m_rectangles.Count;
            if (missingCanvasRectangleCount > 0)
            {
                AddNewFaceControls(missingCanvasRectangleCount);
            }

            // Update and make visible the controls for the set of face bounds and sentiments passed in
            int i = 0;
            for(; i < bounds.Count; i+=4)
            {
                int index = i / 4;
                var sentiment = sentiments[i/4];

                // Update face bounds
                m_rectangles[index].Width = (bounds[i+2] - bounds[i]) * m_canvas.Width;
                m_rectangles[index].Height = (bounds[i+3] - bounds[i+1]) * m_canvas.Height;
                m_rectangles[index].Visibility = Visibility.Visible;
                Canvas.SetLeft(m_rectangles[index], bounds[i] * m_canvas.Width);
                Canvas.SetTop(m_rectangles[index], bounds[i+1] * m_canvas.Height);

                // Update face sentiment emoji
                m_sentimentControls[index].Text = $"{m_emojis[sentiment]}";
                m_sentimentControls[index].Visibility = Visibility.Visible;
                Canvas.SetLeft(m_sentimentControls[index], bounds[i] * m_canvas.Width);
                Canvas.SetTop(m_sentimentControls[index], bounds[i+1] * m_canvas.Height);

                // Update face sentiment scores tooltip
                string rawScores = "";
                for(int j = 0; j < sentimentTypeOffset; j++)
                {
                    if(j == (int)sentiment)
                    {
                        rawScores += "-->";
                    }
                    rawScores += $"{m_sentimentTypesStrings[j]} : {sentimentScores[sentimentTypeOffset * index + j]}\n";
                }
                m_controlToolTips[index].Content = rawScores;
                ToolTipService.SetToolTip(m_sentimentControls[index], m_controlToolTips[index]);
            }
            i /= 4;
            // hide remaining unused controls
            for (; i < m_rectangles.Count; i++)
            {
                m_rectangles[i].Visibility = Visibility.Collapsed;
                m_sentimentControls[i].Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Add new UI element to draw face bounds and sentiment to the canvas
        /// </summary>
        /// <param name="count"></param>
        private void AddNewFaceControls(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var rectangle = new Rectangle() { Stroke = new SolidColorBrush(Colors.Red), StrokeThickness = 2 };
                m_rectangles.Add(rectangle);
                m_canvas.Children.Add(rectangle);

                var sentimentControl = new TextBlock() { FontSize = 20 };
                m_sentimentControls.Add(sentimentControl);
                m_canvas.Children.Add(sentimentControl);

                m_controlToolTips.Add(new ToolTip());
            }
        }
    }
}
