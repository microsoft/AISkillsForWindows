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
using Microsoft.AI.Skills.SkillInterfacePreview;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Toolkit.Uwp.Helpers;
using System.Threading;

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
            m_faceSentimentRenderer = new FaceSentimentRenderer(UICanvasOverlay, UISentiment);

            await Task.Run(async () =>
            {
                try
                {
                    // Instatiate skill descriptor to display details about the skill and populate UI
                    m_skillDescriptor = new FaceSentimentAnalyzerDescriptor();
                    m_availableExecutionDevices = await m_skillDescriptor.GetSupportedExecutionDevicesAsync();

                    // Refresh UI
                    await Dispatcher.RunAsync(
                        Windows.UI.Core.CoreDispatcherPriority.Normal,
                        () =>
                        {
                            // Show skill description members in UI
                            UISkillName.Text = m_skillDescriptor.Name;

                            UISkillDescription.Text = $"{m_skillDescriptor.Description}" +
                            $"\n\tauthored by: {m_skillDescriptor.Version.Author}" +
                            $"\n\tpublished by: {m_skillDescriptor.Version.Author}" +
                            $"\n\tversion: {m_skillDescriptor.Version.Major}.{m_skillDescriptor.Version.Minor}" +
                            $"\n\tunique ID: {m_skillDescriptor.Id}";

                            var inputDesc = m_skillDescriptor.InputFeatureDescriptors[0] as SkillFeatureImageDescriptor;
                            UISkillInputDescription.Text = $"\tName: {inputDesc.Name}" +
                            $"\n\tDescription: {inputDesc.Description}" +
                            $"\n\tType: {inputDesc.FeatureKind}" +
                            $"\n\tWidth: {inputDesc.Width}" +
                            $"\n\tHeight: {inputDesc.Height}" +
                            $"\n\tSupportedBitmapPixelFormat: {inputDesc.SupportedBitmapPixelFormat}" +
                            $"\n\tSupportedBitmapAlphaMode: {inputDesc.SupportedBitmapAlphaMode}";

                            var outputDesc1 = m_skillDescriptor.OutputFeatureDescriptors[0] as SkillFeatureTensorDescriptor;
                            UISkillOutputDescription1.Text = $"\tName: {outputDesc1.Name}, Description: {outputDesc1.Description} \n\tType: {outputDesc1.FeatureKind} of {outputDesc1.ElementKind} with shape [{outputDesc1.Shape.Select(i => i.ToString()).Aggregate((a, b) => a + ", " + b)}]";

                            var outputDesc2 = m_skillDescriptor.OutputFeatureDescriptors[1] as SkillFeatureTensorDescriptor;
                            UISkillOutputDescription2.Text = $"\tName: {outputDesc2.Name} \n\tDescription: {outputDesc2.Description} \n\tType: {outputDesc2.FeatureKind} of {outputDesc2.ElementKind} with shape [{outputDesc2.Shape.Select(i => i.ToString()).Aggregate((a, b) => a + ", " + b)}]";

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

            // Register callback for if camera preview encoutners an issue
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
                await m_bitmapSource.SetBitmapAsync(frame.SoftwareBitmap);
                UIImageViewer.Source = m_bitmapSource;

                UIImageViewer_SizeChanged(null, null);

                await RunSkillAsync(frame);

                m_skill = null;
                m_binding = null;
            }
            catch (Exception ex)
            {
                await (new MessageDialog(ex.Message)).ShowAsync();
            }

            m_lock.Release();

            // Enable subsequent trigger of this event callback
            UIButtonFilePick.IsEnabled = true;
            UICameraToggle.IsEnabled = true;
        }

        /// <summary>
        /// Launch file picker for user to select a picture file and return a VideoFrame.
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
            }
            catch (Exception ex)
            {
                await (new MessageDialog(ex.Message)).ShowAsync();
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
                    // Allign overlay canvas and camera preview so that face detection rectangle looks right
                    if (!m_isCameraFrameDimensionInitialized)
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
            // Update input image and run the skill against it
            await m_binding.SetInputImageAsync(frame);
            await m_skill.EvaluateAsync(m_binding);

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                // Retrieve result
                if (!m_binding.IsFaceFound)
                {
                    // if no face found, hide the rectangle in the UI
                    m_faceSentimentRenderer.IsVisible = false;
                    UISkillOutputDetails.Text = "No face found";
                }
                else // Display the face rectangle abd sebtiment in the UI
                {
                    m_faceSentimentRenderer.Update(m_binding.FaceRectangle, m_binding.PredominantSentiment);
                    m_faceSentimentRenderer.IsVisible = true;
                    var scores = (m_binding["FaceSentimentScores"].FeatureValue as SkillFeatureTensorFloatValue).GetAsVectorView();
                    UISkillOutputDetails.Text = "";
                    for (int i = 0; i < (int)SentimentType.contempt; i++)
                    {
                        UISkillOutputDetails.Text += $"{(SentimentType)i} : {scores[i]} \n";
                    }
                }
            });
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
    }

    /// <summary>
    /// Convenience class for rendering a rectangle on screen
    /// </summary>
    internal class FaceSentimentRenderer
    {
        private Canvas m_canvas;
        private TextBox m_sentimentControl;
        private Rectangle m_rectangle = new Rectangle();
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

        /// <summary>
        /// FaceSentimentRenderer constructor
        /// </summary>
        /// <param name="canvas"></param>
        public FaceSentimentRenderer(Canvas canvas, TextBox sentimentControl)
        {
            m_canvas = canvas;
            m_sentimentControl = sentimentControl;
            m_rectangle = new Rectangle() { Stroke = new SolidColorBrush(Colors.Red), StrokeThickness = 2 };
            m_canvas.Children.Add(m_rectangle);
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
                m_sentimentControl.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Update coordinates of face rectangle and predominant sentiment passsed as parameter
        /// </summary>
        /// <param name="coordinates"></param>
        public void Update(IReadOnlyList<float> coordinates, SentimentType sentiment)
        {
            if (coordinates == null)
            {
                return;
            }
            if (coordinates.Count != 4)
            {
                throw new Exception("you can only pass a set of 4 float coordinates (left, top, right, bottom) to this method");
            }
            m_rectangle.Width = (coordinates[2] - coordinates[0]) * m_canvas.Width;
            m_rectangle.Height = (coordinates[3] - coordinates[1]) * m_canvas.Height;
            Canvas.SetLeft(m_rectangle, coordinates[0] * m_canvas.Width);
            Canvas.SetTop(m_rectangle, coordinates[1] * m_canvas.Height);

            m_sentimentControl.Text = $"{sentiment} {m_emojis[sentiment]}";
        }
    }
}
