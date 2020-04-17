// Copyright (c) Microsoft Corporation. All rights reserved.

using ImageScanningSample.Helper;
using Microsoft.AI.Skills.SkillInterface;
using Microsoft.AI.Skills.Vision.ImageScanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public class ImageCleanerBindingInputFeatureValues
    {
        public ImageCleaningKind InputImageType;
    }

    /// <summary>
    /// Helper class to display interactive controls for the ImageCleanerBinding features
    /// </summary>
    public sealed partial class ImageCleanerInteractiveControl : UserControl
    {
        private SoftwareBitmapSource m_resultImageSource = new SoftwareBitmapSource();
        private VideoFrame m_cachedRectifiedImage = null;
        public delegate void ImageCleaningKindChangedHandler(ImageCleaningKind type);
        public event ImageCleaningKindChangedHandler ImageCleaningKindChanged;

        /// <summary>
        /// ImageCleanerInteractiveControl constructor
        /// </summary>
        public ImageCleanerInteractiveControl()
        {
            this.InitializeComponent();
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
        /// Update the output ImageCleaningKind displayed
        /// </summary>
        /// <param name="imageCleaningKind"></param>
        public void UpdateResultImageType(ImageCleaningKind imageCleaningKind)
        {
            UIDetectedImageType.Content = imageCleaningKind.ToString();
        }

        /// <summary>
        /// Update the input ImageCleaningKind displayed
        /// </summary>
        /// <param name="imageCleaningKind"></param>
        public void UpdateSelectedImageCleaningKind(ImageCleaningKind imageCleaningKind)
        {
            UIInputImageType.SelectedIndex = (int)imageCleaningKind;
        }

        // -- Event handlers -- //
        #region EventHandlers

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UIInputImageType.ItemsSource = Enum.GetNames(typeof(ImageCleaningKind));
            UIResultImage.Source = m_resultImageSource;
        }

        private void UIInputImageType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UIInputImageType.SelectedIndex >= 0)
            {
                if (ImageCleaningKindChanged != null)
                {
                    ImageCleaningKindChanged.Invoke((ImageCleaningKind)UIInputImageType.SelectedIndex);
                }
            }
        }

        private async void UISaveImageButton_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker fileSavePicker = new FileSavePicker();
            fileSavePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            fileSavePicker.DefaultFileExtension = ".jpg";
            fileSavePicker.SuggestedFileName = "Cleaned";
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
    /// Custom control to render and manipulate image cleaner inputs & outputs
    /// </summary>
    public class ImageCleanerControl : SkillControl
    {
        private ImageCleanerBindingInputFeatureValues m_ImageCleanerBindingFeatureValues;
        private ImageCleanerInteractiveControl m_interactiveControl = null;

        /// <summary>
        ///  ImageCleanerControl constructor
        /// </summary>
        /// <param name="binding"></param>
        public ImageCleanerControl(ISkillBinding binding) : base(binding)
        {
            // Update the view of the initial binding values
            m_ImageCleanerBindingFeatureValues = ExtractBindingValues(binding);
            m_interactiveControl = new ImageCleanerInteractiveControl();
            m_interactiveControl.ImageCleaningKindChanged += ImageCleanerInteractiveControl_ImageCleaningKindChanged;

            Children.Add(m_interactiveControl);
            m_interactiveControl.UpdateSelectedImageCleaningKind(m_ImageCleanerBindingFeatureValues.InputImageType);
        }

        /// <summary>
        /// ImageCleaningKindChanged event handler
        /// </summary>
        /// <param name="type"></param>
        private void ImageCleanerInteractiveControl_ImageCleaningKindChanged(ImageCleaningKind type)
        {
            m_ImageCleanerBindingFeatureValues.InputImageType = type;
        }

        /// <summary>
        /// Update the view of the binding values
        /// </summary>
        /// <param name="binding"></param>
        private ImageCleanerBindingInputFeatureValues ExtractBindingValues(ISkillBinding binding)
        {
            ImageCleanerBindingInputFeatureValues result = new ImageCleanerBindingInputFeatureValues();

            var inputImageType = binding["InputImageType"].FeatureValue;
            var inputImageTypeFeatureValue = (inputImageType as SkillFeatureTensorStringValue).GetAsVectorView();
            result.InputImageType = (ImageCleaningKind)Enum.GetNames(typeof(ImageCleaningKind)).ToList().IndexOf(inputImageTypeFeatureValue[0]);

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
            ImageCleanerBinding binding = m_skillBinding as ImageCleanerBinding;

            // Set interpolation type
            await binding.SetImageCleaningKindAsync(m_ImageCleanerBindingFeatureValues.InputImageType);

            // Invoke event handlers
            base.RunButton_Click(sender, e);
        }

        /// <summary>
        /// Update results displayed
        /// </summary>
        /// <param name="additionalResult"></param>
        /// <returns></returns>
        override public async Task UpdateSkillControlValuesAsync(object additionalResult)
        {
            ImageCleanerBinding binding = (additionalResult as ImageCleanerBinding);
            VideoFrame resultImage = binding.OutputImage;
            await m_interactiveControl.UpdateResultImageAsync(resultImage);

            var detectedImageType = binding["DetectedImageType"].FeatureValue;
            var detectedImageTypeFeatureValue = (detectedImageType as SkillFeatureTensorStringValue).GetAsVectorView()[0];
            var value = (ImageCleaningKind)Enum.GetNames(typeof(ImageCleaningKind)).ToList().IndexOf(detectedImageTypeFeatureValue);
            m_interactiveControl.UpdateResultImageType(value);

            await base.UpdateSkillControlValuesAsync(additionalResult);
        }
    }
}
