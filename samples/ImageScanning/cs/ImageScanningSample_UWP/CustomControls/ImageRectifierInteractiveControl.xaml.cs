// Copyright (c) Microsoft Corporation. All rights reserved.

using ImageScanningSample.Helper;
using Microsoft.AI.Skills.SkillInterface;
using Microsoft.AI.Skills.Vision.ImageScanning;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace ImageScanningSample
{
    /// <summary>
    /// Encapsulates input values
    /// </summary>
    public class ImageRectifierBindingInputFeatureValues
    {
        public ImageInterpolationKind InterpolationType;
        public List<Point> InputQuad = new List<Point>()
        {
            new Point(0.2, 0.2),
            new Point(0.8, 0.2),
            new Point(0.8, 0.8),
            new Point(0.2, 0.8)
        };
    }

    /// <summary>
    /// Helper class to display interactive controls for the ImageRectifierBinding features
    /// </summary>
    public sealed partial class ImageRectifierInteractiveControl : UserControl
    {
        private SoftwareBitmapSource m_resultImageSource = new SoftwareBitmapSource();
        private VideoFrame m_cachedRectifiedImage = null;
        public delegate void InterpolationTypeChangedHandler(ImageInterpolationKind type);
        public event InterpolationTypeChangedHandler InterpolationTypeChanged;

        /// <summary>
        /// ImageRectifierInteractiveControl constructor
        /// </summary>
        public ImageRectifierInteractiveControl()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Update displayed input values
        /// </summary>
        /// <param name="initialValues"></param>
        public void UpdateDisplayedInputValues(ImageRectifierBindingInputFeatureValues initialValues)
        {
            UpdateInputQuadCorners(initialValues.InputQuad);
        }

        /// <summary>
        /// Update displayed input quad corner values
        /// </summary>
        /// <param name="corners"></param>
        public void UpdateInputQuadCorners(IList<Point> corners)
        {
            Debug.Assert(corners.Count % 4 == 0);
            UITopLeftInputQuadCornerX.Text = corners[0].X.ToString("0.00");
            UITopLeftInputQuadCornerY.Text = corners[0].Y.ToString("0.00");

            UITopRightInputQuadCornerX.Text = corners[1].X.ToString("0.00");
            UITopRightInputQuadCornerY.Text = corners[1].Y.ToString("0.00");

            UIBottomLeftInputQuadCornerX.Text = corners[2].X.ToString("0.00");
            UIBottomLeftInputQuadCornerY.Text = corners[2].Y.ToString("0.00");

            UIBottomRightInputQuadCornerX.Text = corners[3].X.ToString("0.00");
            UIBottomRightInputQuadCornerY.Text = corners[3].Y.ToString("0.00");
        }

        /// <summary>
        /// Update the output image displayed
        /// </summary>
        /// <param name="videoFrame"></param>
        /// <returns></returns>
        public async Task UpdateResultImageAsync(VideoFrame videoFrame)
        {
            m_cachedRectifiedImage = videoFrame;
            await m_resultImageSource.SetBitmapAsync(videoFrame.SoftwareBitmap);
            UISaveImageButton.IsEnabled = true;
        }

        /// <summary>
        /// Update displayed input ImageInterpolationKind values
        /// </summary>
        /// <param name="interpolationType"></param>
        public void UpdateInterpolationType(ImageInterpolationKind interpolationType)
        {
            UIInterpolationType.SelectedIndex = (int)interpolationType;
        }

        // -- Event handlers -- //
        #region EventHandlers

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UIInterpolationType.ItemsSource = Enum.GetNames(typeof(ImageInterpolationKind));
            UIResultImage.Source = m_resultImageSource;
        }

        private void UIInterpolationType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UIInterpolationType.SelectedIndex >= 0)
            {
                if (InterpolationTypeChanged != null)
                {
                    InterpolationTypeChanged.Invoke((ImageInterpolationKind)UIInterpolationType.SelectedIndex);
                }
            }
        }

        private async void UISaveImageButton_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker fileSavePicker = new FileSavePicker();
            fileSavePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            fileSavePicker.DefaultFileExtension = ".jpg";
            fileSavePicker.SuggestedFileName = "Rectified";
            fileSavePicker.FileTypeChoices.Add("image", new List<string>() { ".jpg" });
            var file = await fileSavePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Create the encoder from the stream
                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                    encoder.SetSoftwareBitmap(m_cachedRectifiedImage.SoftwareBitmap);
                    await encoder.FlushAsync();
                }
            }
        }
        #endregion EventHandlers
    }

    /// <summary>
    /// Custom control to render and manipulate image rectifier inputs & outputs
    /// </summary>
    public class ImageRectifierControl : SkillControl
    {
        private ImageRectifierBindingInputFeatureValues m_ImageRectifierBindingFeatureValues;
        private Canvas m_canvas = new Canvas();
        private InteractiveQuadRenderer m_inputQuadRenderer = null;
        private ImageRectifierInteractiveControl m_interactiveControl = null;

        private List<Point> m_inputQuadCorners = new List<Point>
        {
            new Point(0.2, 0.2),
            new Point(0.8, 0.2),
            new Point(0.8, 0.8),
            new Point(0.2, 0.8)
        };

        /// <summary>
        /// ImageRectifierControl constructor
        /// </summary>
        /// <param name="binding"></param>
        public ImageRectifierControl(ISkillBinding binding) : base(binding)
        {
            // Update the view of the initial binding values
            m_ImageRectifierBindingFeatureValues = ExtractBindingValues(binding);
            m_interactiveControl = new ImageRectifierInteractiveControl();
            m_interactiveControl.InterpolationTypeChanged += ImageRectifierInteractiveControl_InterpolationTypeChanged;

            m_imageGrid.Children.Add(m_canvas);
            m_image.SetValue(Canvas.ZIndexProperty, -1);
            m_image.SizeChanged += Image_SizeChanged;

            // Add InputQuad control
            m_inputQuadRenderer = new InteractiveQuadRenderer(ref m_canvas);
            m_inputQuadRenderer.CornersChanged += InteractiveQuadRenderer_CornersChanged;
            m_inputQuadRenderer.CornersChangeCompleted += InteractiveQuadRenderer_CornersChangeCompleted;
            m_inputQuadRenderer.Update(m_ImageRectifierBindingFeatureValues.InputQuad);

            Children.Add(m_interactiveControl);
            m_interactiveControl.UpdateDisplayedInputValues(m_ImageRectifierBindingFeatureValues);
            m_interactiveControl.UpdateInterpolationType(m_ImageRectifierBindingFeatureValues.InterpolationType);
        }

        private void ImageRectifierInteractiveControl_InterpolationTypeChanged(ImageInterpolationKind type)
        {
            m_ImageRectifierBindingFeatureValues.InterpolationType = type;
        }

        /// <summary>
        /// Update the view of the binding values
        /// </summary>
        /// <param name="binding"></param>
        private ImageRectifierBindingInputFeatureValues ExtractBindingValues(ISkillBinding binding)
        {
            ImageRectifierBindingInputFeatureValues result = new ImageRectifierBindingInputFeatureValues();

            var inputQuadFeature = binding["InputQuad"].FeatureValue;
            var inputQuadFeatureValue = (inputQuadFeature as SkillFeatureTensorFloatValue).GetAsVectorView();
            for (int i = 0; i < inputQuadFeatureValue.Count; i += 2)
            {
                result.InputQuad[i / 2] = new Point(inputQuadFeatureValue[i], inputQuadFeatureValue[i + 1]);
            }
            var interpolationType = binding["InterpolationType"].FeatureValue;
            var interpolationTypeFeatureValue = (interpolationType as SkillFeatureTensorStringValue).GetAsVectorView();
            result.InterpolationType = (ImageInterpolationKind)Enum.GetNames(typeof(ImageInterpolationKind)).ToList().IndexOf(interpolationTypeFeatureValue[0]);

            return result;
        }

        /// <summary>
        /// Triggered when Run button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        override async protected void RunButton_Click(object sender, RoutedEventArgs e)
        {
            // Update ISKillBinding
            ImageRectifierBinding binding = m_skillBinding as ImageRectifierBinding;

            // Set interpolation type
            binding.SetInterpolationKind(m_ImageRectifierBindingFeatureValues.InterpolationType);

            await binding.SetInputQuadAsync(m_ImageRectifierBindingFeatureValues.InputQuad);

            // Invoke event handlers
            base.RunButton_Click(sender, e);
        }

        /// <summary>
        /// CornersChangeCompleted event handler
        /// </summary>
        private void InteractiveQuadRenderer_CornersChangeCompleted(List<Point> corners)
        {
            m_inputQuadCorners = corners;
            m_ImageRectifierBindingFeatureValues.InputQuad = m_inputQuadCorners;

            m_runButton.IsEnabled = true;
        }

        /// <summary>
        /// CornersChanged event handler
        /// </summary>
        /// <param name="corners"></param>
        private void InteractiveQuadRenderer_CornersChanged(List<Point> corners)
        {
            m_interactiveControl.UpdateInputQuadCorners(corners);
        }

        /// <summary>
        /// Handle image size change events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            float cameraAspectRatio = (float)m_frameWidth / m_frameHeight;
            float uiAspectRatio = (float)(m_image.ActualWidth / m_image.ActualHeight);
            if (uiAspectRatio > cameraAspectRatio)
            {
                m_canvas.Height = m_image.ActualHeight;
                m_canvas.Width = m_image.ActualHeight * cameraAspectRatio;
            }
            else
            {
                m_canvas.Width = m_image.ActualWidth;
                m_canvas.Height = m_image.ActualWidth / cameraAspectRatio;
            }

            m_inputQuadRenderer.Update(m_inputQuadCorners);
        }


        override public async Task UpdateSkillControlValuesAsync(object additionalResult)
        {
            VideoFrame resultImage = (additionalResult as ImageRectifierBinding).OutputImage;
            await m_interactiveControl.UpdateResultImageAsync(resultImage);

            await base.UpdateSkillControlValuesAsync(additionalResult);
        }
    }
}
