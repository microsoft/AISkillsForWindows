// Copyright (c) Microsoft Corporation. All rights reserved.

using ImageScanningSample.Helper;
using Microsoft.AI.Skills.SkillInterface;
using Microsoft.AI.Skills.Vision.ImageScanning;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;


namespace ImageScanningSample
{
    /// <summary>
    /// Encapsulates input values
    /// </summary>
    public class QuadEdgesDetectorBindingInputFeatureValues
    {
        public int MaxQuadEdges;
    }

    /// <summary>
    /// Helper class to display interactive controls for the QuadEdgesDetectorBinding features
    /// </summary>
    public sealed partial class QuadEdgesDetectorSkillInteractiveControl : UserControl
    {
        public event RangeBaseValueChangedEventHandler MaxQuadEdgesValueChanged;

        /// <summary>
        /// QuadEdgesDetectorSkillInteractiveControl constructor
        /// </summary>
        public QuadEdgesDetectorSkillInteractiveControl()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Update displayed values
        /// </summary>
        /// <param name="initialValues"></param>
        public void UpdateDisplayedInputValues(QuadEdgesDetectorBindingInputFeatureValues initialValues)
        {
            UIMaxQuadEdges.Value = initialValues.MaxQuadEdges;
        }

        /// <summary>
        /// Update displayed output values
        /// </summary>
        /// <param name="detectedVerticalEdgesCount"></param>
        /// <param name="detectedHorizontalEdgesCount"></param>
        public void UpdateDisplayedOutputValues(int detectedVerticalEdgesCount, int detectedHorizontalEdgesCount)
        {
            UIDetectedVerticalEdgeCount.Content = detectedVerticalEdgesCount;
            UIDetectedHorizontalEdgeCount.Content = detectedHorizontalEdgesCount;
        }

        // -- Event handlers -- //
        #region EventHandlers

        private void UIMaxDetectedEdges_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            UIMaxQuadEdgesText.Text = UIMaxQuadEdges.Value.ToString();
            if (MaxQuadEdgesValueChanged != null)
            {
                MaxQuadEdgesValueChanged.Invoke(sender, e);
            }
        }

        #endregion EventHandlers
    }

    /// <summary>
    /// Custom control to render and manipulate QuadEdgesDetector inputs & outputs
    /// </summary>
    public class QuadEdgesDetectorControl : SkillControl
    {
        private QuadEdgesDetectorBindingInputFeatureValues m_quadEdgesDetectorBindingFeatureValues;
        private Canvas m_canvas = new Canvas();
        private LineSetRenderer m_verticalLinesResultRenderer = null;
        private LineSetRenderer m_horizontalLinesResultRenderer = null;
        private IReadOnlyList<Point> m_detectedVerticalEdges = null;
        private IReadOnlyList<Point> m_detectedHorizontalEdges = null;
        private QuadEdgesDetectorSkillInteractiveControl m_interactiveControl = null;

        /// <summary>
        ///  QuadEdgesDetectorControl constructor
        /// </summary>
        /// <param name="binding"></param>
        public QuadEdgesDetectorControl(ISkillBinding binding) : base(binding)
        {
            // Update the view of the initial binding values
            m_quadEdgesDetectorBindingFeatureValues = ExtractBindingValues(binding);
            m_interactiveControl = new QuadEdgesDetectorSkillInteractiveControl();
            m_interactiveControl.MaxQuadEdgesValueChanged += QuadEdgesDetectorSkillInteractiveControl_MaxQuadEdgesValueChanged;

            m_imageGrid.Children.Add(m_canvas);
            m_image.SetValue(Canvas.ZIndexProperty, -1);
            m_image.SizeChanged += Image_SizeChanged;

            // Add line results controls
            m_verticalLinesResultRenderer = new LineSetRenderer(ref m_canvas, Colors.Blue);
            m_verticalLinesResultRenderer.IsVisible = false;
            m_horizontalLinesResultRenderer = new LineSetRenderer(ref m_canvas, Colors.Orange);
            m_horizontalLinesResultRenderer.IsVisible = false;

            Children.Add(m_interactiveControl);
            m_interactiveControl.UpdateDisplayedInputValues(m_quadEdgesDetectorBindingFeatureValues);
        }

        /// <summary>
        /// Update results displayed
        /// </summary>
        /// <param name="additionalResult"></param>
        /// <returns></returns>
        override public async Task UpdateSkillControlValuesAsync(object additionalResult)
        {
            QuadEdgesDetectorBinding binding = additionalResult as QuadEdgesDetectorBinding;
            m_detectedVerticalEdges = binding.DetectedVerticalEdges;
            m_detectedHorizontalEdges = binding.DetectedHorizontalEdges;

            m_verticalLinesResultRenderer.Update(m_detectedVerticalEdges);
            m_verticalLinesResultRenderer.IsVisible = true;
            m_horizontalLinesResultRenderer.Update(m_detectedHorizontalEdges);
            m_horizontalLinesResultRenderer.IsVisible = true;

            m_interactiveControl.UpdateDisplayedOutputValues(m_detectedVerticalEdges.Count, m_detectedHorizontalEdges.Count);

            await base.UpdateSkillControlValuesAsync(additionalResult);
        }

        /// <summary>
        /// Update the view of the binding values
        /// </summary>
        /// <param name="binding"></param>
        private QuadEdgesDetectorBindingInputFeatureValues ExtractBindingValues(ISkillBinding binding)
        {
            QuadEdgesDetectorBindingInputFeatureValues result = new QuadEdgesDetectorBindingInputFeatureValues();
            result.MaxQuadEdges = (binding["MaxDetectedEdges"].FeatureValue as SkillFeatureTensorIntValue).GetAsVectorView()[0];

            return result;
        }

        // -- Event handlers -- //
        #region EventHandlers

        override async protected void RunButton_Click(object sender, RoutedEventArgs e)
        {
            // Update ISKillBinding
            QuadEdgesDetectorBinding binding = m_skillBinding as QuadEdgesDetectorBinding;

            await binding["MaxDetectedEdges"].SetFeatureValueAsync(m_quadEdgesDetectorBindingFeatureValues.MaxQuadEdges);

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

            m_verticalLinesResultRenderer.Update(m_detectedVerticalEdges);
            m_horizontalLinesResultRenderer.Update(m_detectedHorizontalEdges);
        }

        private void QuadEdgesDetectorSkillInteractiveControl_MaxQuadEdgesValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            m_quadEdgesDetectorBindingFeatureValues.MaxQuadEdges = (int)e.NewValue;
        }

        #endregion EventHandlers
    }
}
