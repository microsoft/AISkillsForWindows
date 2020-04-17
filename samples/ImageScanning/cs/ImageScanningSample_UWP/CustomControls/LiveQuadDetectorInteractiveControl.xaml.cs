// Copyright (c) Microsoft Corporation. All rights reserved.

using ImageScanningSample.Helper;
using Microsoft.AI.Skills.SkillInterface;
using Microsoft.AI.Skills.Vision.ImageScanning;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ImageScanningSample
{
    /// <summary>
    /// Encapsulates input values
    /// </summary>
    public class LiveQuadDetectorBindingInputFeatureValues
    {
        public bool Reset;
    }

    /// <summary>
    /// Helper class to display interactive controls for the LiveQuadDetectorBinding features
    /// </summary>
    public sealed partial class LiveQuadDetectorSkillInteractiveControl : UserControl
    {
        public event RoutedEventHandler ResetCheckedUnchecked;

        /// <summary>
        /// LiveQuadDetectorSkillInteractiveControl constructor
        /// </summary>
        public LiveQuadDetectorSkillInteractiveControl()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Update displayed input values
        /// </summary>
        /// <param name="initialValues"></param>
        public void UpdateDisplayedInputValues(LiveQuadDetectorBindingInputFeatureValues initialValues)
        {
            UIReset.IsChecked = initialValues.Reset;
        }

        /// <summary>
        /// Update displayed output values
        /// </summary>
        /// <param name="isSimilar"></param>
        public void UpdateDisplayedOutputValues(bool isSimilar)
        {
            UIIsSimilar.IsChecked = isSimilar;
        }

        // -- Event handlers -- //
        #region EventHandlers

        /// <summary>
        /// Handler of UIReset Checked and Unchecked events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UIReset_Checked(object sender, RoutedEventArgs e)
        {
            if (ResetCheckedUnchecked != null)
            {
                ResetCheckedUnchecked.Invoke(sender, e);
            }
        }

        #endregion EventHandlers
    }

    /// <summary>
    /// Custom control to render and manipulate LiveQuadDetector inputs & outputs
    /// </summary>
    public class LiveQuadDetectorControl : SkillControl
    {
        private LiveQuadDetectorBindingInputFeatureValues m_liveQuadDetectorBindingFeatureValues;
        private Canvas m_canvas = new Canvas();
        private QuadSetRenderer m_quadsResultRenderer = null;
        private IReadOnlyList<Point> m_resultQuadCorners = null;
        private LiveQuadDetectorSkillInteractiveControl m_interactiveControl = null;

        /// <summary>
        ///  LiveQuadDetectorControl constructor
        /// </summary>
        /// <param name="binding"></param>
        public LiveQuadDetectorControl(ISkillBinding binding) : base(binding)
        {
            // Update the view of the initial binding values
            m_liveQuadDetectorBindingFeatureValues = ExtractBindingValues(binding);
            m_interactiveControl = new LiveQuadDetectorSkillInteractiveControl();
            m_interactiveControl.ResetCheckedUnchecked += LiveQuadDetectorSkillInteractiveControl_ResetCheckedUnchecked;

            m_imageGrid.Children.Add(m_canvas);
            m_image.SetValue(Canvas.ZIndexProperty, -1);
            m_image.SizeChanged += Image_SizeChanged;

            // Add Quad results control
            m_quadsResultRenderer = new QuadSetRenderer(ref m_canvas, 1);
            m_quadsResultRenderer.IsVisible = false;

            Children.Add(m_interactiveControl);
            m_interactiveControl.UpdateDisplayedInputValues(m_liveQuadDetectorBindingFeatureValues);
        }

        /// <summary>
        /// Update results displayed
        /// </summary>
        /// <param name="additionalResult"></param>
        /// <returns></returns>
        override public async Task UpdateSkillControlValuesAsync(object additionalResult)
        {
            bool isSimilar = false;
            m_resultQuadCorners = (additionalResult as LiveQuadDetectorBinding).DetectedQuad(out isSimilar);

            m_quadsResultRenderer.Update(m_resultQuadCorners);
            m_quadsResultRenderer.IsVisible = true;

            m_interactiveControl.UpdateDisplayedOutputValues(isSimilar);

            await base.UpdateSkillControlValuesAsync(additionalResult);
        }

        /// <summary>
        /// Update the view of the binding values
        /// </summary>
        /// <param name="binding"></param>
        private LiveQuadDetectorBindingInputFeatureValues ExtractBindingValues(ISkillBinding binding)
        {
            LiveQuadDetectorBindingInputFeatureValues result = new LiveQuadDetectorBindingInputFeatureValues();
            result.Reset = (binding["Reset"].FeatureValue as SkillFeatureTensorBooleanValue).GetAsVectorView()[0];

            return result;
        }

        // -- Event handlers -- //
        #region EventHandlers

        override async protected void RunButton_Click(object sender, RoutedEventArgs e)
        {
            // Update ISKillBinding
            LiveQuadDetectorBinding binding = m_skillBinding as LiveQuadDetectorBinding;

            await binding["Reset"].SetFeatureValueAsync(m_liveQuadDetectorBindingFeatureValues.Reset);

            // Invoke event handlers
            base.RunButton_Click(sender, e);
        }

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

            m_quadsResultRenderer.Update(m_resultQuadCorners);
        }

        private void LiveQuadDetectorSkillInteractiveControl_ResetCheckedUnchecked(object sender, RoutedEventArgs e)
        {
            m_liveQuadDetectorBindingFeatureValues.Reset = (sender as CheckBox).IsChecked == true;

            m_runButton.IsEnabled = true;
        }

        #endregion EventHandlers
    }
}
