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
using FaceSentimentAnalyzer;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI;
using Windows.UI.Xaml.Shapes;
using Microsoft.AI.Skills.SkillInterfacePreview;

namespace FaceSentimentAnalysisTestApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Skill-related variables
        private FaceSentimentAnalyzerBinding m_binding = null;
        private FaceSentimentAnalyzerSkill m_skill = null;

        // UI-related variables
        private SoftwareBitmapSource m_bitmapSource = new SoftwareBitmapSource();
        private FaceSentimentRenderer m_faceSentimentRenderer = null;

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
                    // Initialize skill with first supported device
                    FaceSentimentAnalyzerDescriptor desc = new FaceSentimentAnalyzerDescriptor();
                    var devices = await desc.GetSupportedExecutionDevicesAsync();
                    var skill = await desc.CreateSkillAsync(devices.First());
                    m_skill = skill as FaceSentimentAnalyzerSkill;

                    // Instantiate a binding object that will hold the skill's input and output resource
                    m_binding = await m_skill.CreateSkillBindingAsync() as FaceSentimentAnalyzerBinding;

                    // Refresh UI
                    await Dispatcher.RunAsync(
                        Windows.UI.Core.CoreDispatcherPriority.Normal,
                        () =>
                        {
                            // Show skill description members in UI
                            UISkillName.Text = desc.Name;

                            UISkillDescription.Text = $"{desc.Description}" +
                            $"\n\tauthored by: {desc.Version.Author}" +
                            $"\n\tpublished by: {desc.Version.Author}" +
                            $"\n\tversion: {desc.Version.Major}.{desc.Version.Minor}" +
                            $"\n\tunique ID: {desc.Id}";

                            var inputDesc = desc.InputFeatureDescriptors[0] as SkillFeatureImageDescriptor;
                            UISkillInputDescription.Text = $"\tName: {inputDesc.Name}" +
                            $"\n\tDescription: {inputDesc.Description}" +
                            $"\n\tType: {inputDesc.FeatureKind}" +
                            $"\n\tWidth: {inputDesc.Width}" +
                            $"\n\tHeight: {inputDesc.Height}" + 
                            $"\n\tSupportedBitmapPixelFormat: {inputDesc.SupportedBitmapPixelFormat}" + 
                            $"\n\tSupportedBitmapAlphaMode: {inputDesc.SupportedBitmapAlphaMode}";

                            var outputDesc1 = desc.OutputFeatureDescriptors[0] as SkillFeatureTensorDescriptor;
                            UISkillOutputDescription1.Text = $"\tName: {outputDesc1.Name}, Description: {outputDesc1.Description} \n\tType: {outputDesc1.FeatureKind} of {outputDesc1.ElementKind} with shape [{outputDesc1.Shape.Select( i => i.ToString()).Aggregate((a, b) => a + ", " + b)}]";

                            var outputDesc2 = desc.OutputFeatureDescriptors[1] as SkillFeatureTensorDescriptor;
                            UISkillOutputDescription2.Text = $"\tName: {outputDesc2.Name} \n\tDescription: {outputDesc2.Description} \n\tType: {outputDesc2.FeatureKind} of {outputDesc2.ElementKind} with shape [{outputDesc2.Shape.Select(i => i.ToString()).Aggregate((a, b) => a + ", " + b)}]";

                            // Alow user to interact with the app
                            UIButtonFilePick.IsEnabled = true;
                            UIButtonFilePick.Focus(FocusState.Keyboard);
                        });
                }
                catch(Exception ex)
                {
                    await new MessageDialog(ex.Message).ShowAsync();
                }
            });
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
        /// Triggered when UIButtonFilePick is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UIButtonFilePick_Click(object sender, RoutedEventArgs e)
        {
            // Disable subsequent trigger of this event callback 
            UIButtonFilePick.IsEnabled = false;
            try
            {
                var frame = await LoadVideoFrameFromFilePickedAsync();
                await m_bitmapSource.SetBitmapAsync(frame.SoftwareBitmap);
                UIImageViewer.Source = m_bitmapSource;

                // Update input image and run the skill against it
                await m_binding.SetInputImageAsync(frame);
                await m_skill.EvaluateAsync(m_binding);

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
                    var scores = (m_binding["faceSentimentScores"].FeatureValue as SkillFeatureTensorFloatValue).GetAsVectorView();
                    UISkillOutputDetails.Text = "";
                    for(int i = 0; i < (int)SentimentType.contempt; i++)
                    {
                        UISkillOutputDetails.Text += $"{(SentimentType)i} : {scores[i]} \n";
                    }
                }
            }
            catch (Exception ex)
            {
                await (new MessageDialog(ex.Message)).ShowAsync();
            }

            // Enable subsequent trigger of this event callback
            UIButtonFilePick.IsEnabled = true;
        }

        /// <summary>
        /// Triggers when the iamge control is resized, makes sure the canvas size stays in sync
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UIImageViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UICanvasOverlay.Width = UIImageViewer.ActualWidth;
            UICanvasOverlay.Height = UIImageViewer.ActualHeight;
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
