// Copyright (c) Microsoft Corporation. All rights reserved.

using ImageScanningSample.Helper;
using Microsoft.AI.Skills.SkillInterface;
using Microsoft.AI.Skills.Vision.ImageScanning;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Microsoft.Toolkit.Uwp.UI.Media;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace ImageScanningSample
{
    /// <summary>
    /// Encapsulates input values
    /// </summary>
    public class QuadDetectorBindingInputFeatureValues
    {
        public int SubMarginPercentage;
        public int MaxDetectedQuads;
        public int NumberOfEdgePixels;
        public bool UseCenterPoint;
        public Point CenterPointCoordinates;
        public bool UseBaseQuad;
        public List<Point> BaseQuad = new List<Point>() { new Point(), new Point(), new Point(), new Point() };
    }

    /// <summary>
    /// Helper class to display interactive controls for the QuadDetectorBinding features
    /// </summary>
    public sealed partial class QuadDetectorSkillInteractiveControl : UserControl
    {
        public event RoutedEventHandler CenterPointCheckedUnchecked;
        public event RoutedEventHandler SpecifyBaseQuadCheckedUnchecked;
        public event RangeBaseValueChangedEventHandler SubMargingValueChanged;
        public event RangeBaseValueChangedEventHandler MaxQuadValueChanged;
        public event RangeBaseValueChangedEventHandler NumberOfPixelsPerEdgeValueChanged;

        public int LookupRegionCenterCropPercentage { get; set; } = 0;
        public List<Point> PreviousQuad { get; set; }

        /// <summary>
        /// QuadDetectorSkillInteractiveControl constructor
        /// </summary>
        public QuadDetectorSkillInteractiveControl()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Update displayed input values
        /// </summary>
        /// <param name="initialValues"></param>
        public void UpdateDisplayedInputValues(QuadDetectorBindingInputFeatureValues initialValues)
        {
            UIUseCenterPoint.IsChecked = initialValues.UseCenterPoint;
            if (initialValues.UseCenterPoint)
            {
                UpdateCenterPointDisplay(
                    initialValues.CenterPointCoordinates.X,
                    initialValues.CenterPointCoordinates.Y);
            }
            UILookupRegionCenterCropPercentage.Value = initialValues.SubMarginPercentage;
            UIMaxQuad.Value = initialValues.MaxDetectedQuads;
            UINumberOfPixelsPerEdge.Value = initialValues.NumberOfEdgePixels;
            UpdateBaseQuadCorners(initialValues.BaseQuad);
        }

        /// <summary>
        /// Update displayed output values
        /// </summary>
        /// <param name="detectedQuadCount"></param>
        public void UpdateDisplayedOutputValues(int detectedQuadCount)
        {
            UIDetectedQuadCount.Content = detectedQuadCount.ToString();
        }

        /// <summary>
        /// Update displayed center point
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        public void UpdateCenterPointDisplay(double X, double Y)
        {
            UIBoundCenterPointX.Text = X.ToString("0.00");
            UIBoundCenterPointY.Text = Y.ToString("0.00");
        }

        /// <summary>
        /// Update displayed base quad corners
        /// </summary>
        /// <param name="corners"></param>
        public void UpdateBaseQuadCorners(IList<Point> corners)
        {
            UITopLeftBaseQuadCornerX.Text = corners[0].X.ToString("0.00");
            UITopLeftBaseQuadCornerY.Text = corners[0].Y.ToString("0.00");

            UITopRightBaseQuadCornerX.Text = corners[1].X.ToString("0.00");
            UITopRightBaseQuadCornerY.Text = corners[1].Y.ToString("0.00");

            UIBottomLeftBaseQuadCornerX.Text = corners[2].X.ToString("0.00");
            UIBottomLeftBaseQuadCornerY.Text = corners[2].Y.ToString("0.00");

            UIBottomRightBaseQuadCornerX.Text = corners[3].X.ToString("0.00");
            UIBottomRightBaseQuadCornerY.Text = corners[3].Y.ToString("0.00");
        }

        // -- Event handlers -- //
        #region EventHandlers

        private void UIUseCenterPoint_Checked(object sender, RoutedEventArgs e)
        {
            UIBoundCenterPointX.IsEnabled = (bool)UIUseCenterPoint.IsChecked;
            UIBoundCenterPointY.IsEnabled = (bool)UIUseCenterPoint.IsChecked;
            if (UIUseCenterPoint.IsChecked == false)
            {
                UIBoundCenterPointX.Text = "";
                UIBoundCenterPointY.Text = "";
            }
            if (CenterPointCheckedUnchecked != null)
            {
                CenterPointCheckedUnchecked.Invoke(sender, e);
            }
        }

        private void UILookupRegionCenterCropPercentage_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            UILookupRegionCenterCropPercentageText.Text = UILookupRegionCenterCropPercentage.Value.ToString();
            if (SubMargingValueChanged != null)
            {
                SubMargingValueChanged.Invoke(sender, e);
            }
        }

        private void UISpecifyBaseQuad_Checked(object sender, RoutedEventArgs e)
        {
            UITopLeftBaseQuadCornerX.IsEnabled = (bool)UISpecifyBaseQuad.IsChecked;
            UITopLeftBaseQuadCornerY.IsEnabled = (bool)UISpecifyBaseQuad.IsChecked;

            UITopRightBaseQuadCornerX.IsEnabled = (bool)UISpecifyBaseQuad.IsChecked;
            UITopRightBaseQuadCornerY.IsEnabled = (bool)UISpecifyBaseQuad.IsChecked;

            UIBottomLeftBaseQuadCornerX.IsEnabled = (bool)UISpecifyBaseQuad.IsChecked;
            UIBottomLeftBaseQuadCornerY.IsEnabled = (bool)UISpecifyBaseQuad.IsChecked;

            UIBottomRightBaseQuadCornerX.IsEnabled = (bool)UISpecifyBaseQuad.IsChecked;
            UIBottomRightBaseQuadCornerY.IsEnabled = (bool)UISpecifyBaseQuad.IsChecked;

            if (UISpecifyBaseQuad.IsChecked == false)
            {
                UITopLeftBaseQuadCornerX.Text = "";
                UITopLeftBaseQuadCornerY.Text = "";

                UITopRightBaseQuadCornerX.Text = "";
                UITopRightBaseQuadCornerY.Text = "";

                UIBottomLeftBaseQuadCornerX.Text = "";
                UIBottomLeftBaseQuadCornerY.Text = "";

                UIBottomRightBaseQuadCornerX.Text = "";
                UIBottomRightBaseQuadCornerY.Text = "";
            }
            if (SpecifyBaseQuadCheckedUnchecked != null)
            {
                SpecifyBaseQuadCheckedUnchecked.Invoke(sender, e);
            }
        }

        private void UIMaxQuad_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            UIMaxQuadText.Text = UIMaxQuad.Value.ToString();
            if (MaxQuadValueChanged != null)
            {
                MaxQuadValueChanged.Invoke(sender, e);
            }
        }

        private void UINumberOfPixelsPerEdge_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            UINumberOfPixelsPerEdgeText.Text = UINumberOfPixelsPerEdge.Value.ToString();
            if (NumberOfPixelsPerEdgeValueChanged != null)
            {
                NumberOfPixelsPerEdgeValueChanged.Invoke(sender, e);
            }
        }

        #endregion EventHandlers
    }

    /// <summary>
    /// Custom control to render and manipulate QuadDetector inputs & outputs
    /// </summary>
    public class QuadDetectorControl : SkillControl
    {
        private QuadDetectorBindingInputFeatureValues m_quadDetectorBindingFeatureValues;
        private Canvas m_canvas = new Canvas();
        private InteractiveQuadRenderer m_baseQuadRenderer = null;
        private QuadSetRenderer m_quadsResultRenderer = null;
        private IReadOnlyList<Point> m_resultQuadCorners = null;
        private QuadDetectorSkillInteractiveControl m_interactiveControl = null;
        private ImageCropperThumb m_centerPointControl = new ImageCropperThumb() { Visibility = Visibility.Collapsed };

        private Border[] m_margins = new Border[4]
        {
            new Border() { Opacity = 1.0, Background = new BackdropSepiaBrush() { Intensity = 1 } },
            new Border() { Opacity = 1.0, Background = new BackdropSepiaBrush() { Intensity = 1 } },
            new Border() { Opacity = 1.0, Background = new BackdropSepiaBrush() { Intensity = 1 } },
            new Border() { Opacity = 1.0, Background = new BackdropSepiaBrush() { Intensity = 1 } }
        };

        private Point[] m_baseQuadCorners = new Point[4]
        {
            new Point(0.2, 0.2),
            new Point(0.8, 0.2),
            new Point(0.8, 0.8),
            new Point(0.2, 0.8),
        };

        /// <summary>
        ///  QuadDetectorControl constructor
        /// </summary>
        /// <param name="binding"></param>
        public QuadDetectorControl(ISkillBinding binding) : base(binding)
        {
            // Update the view of the initial binding values
            m_quadDetectorBindingFeatureValues = ExtractBindingValues(binding);
            m_interactiveControl = new QuadDetectorSkillInteractiveControl();

            m_imageGrid.Children.Add(m_canvas);
            m_image.SetValue(Canvas.ZIndexProperty, -1);
            m_image.SizeChanged += Image_SizeChanged;

            // Add SubRectangleMargin control
            foreach (var margin in m_margins)
            {
                m_canvas.Children.Add(margin);
            }
            m_interactiveControl.SubMargingValueChanged += QuadDetectorSkillInteractiveControl_SubMargingValueChanged;
            m_interactiveControl.MaxQuadValueChanged += QuadDetectorSkillInteractiveControl_MaxQuadValueChanged;
            m_interactiveControl.NumberOfPixelsPerEdgeValueChanged += QuadDetectorSkillInteractiveControl_NumberOfPixelsPerEdgeValueChanged;

            // Add Quad results control
            m_quadsResultRenderer = new QuadSetRenderer(ref m_canvas, 10);
            m_quadsResultRenderer.IsVisible = false;

            // Add BaseQuad control
            m_baseQuadRenderer = new InteractiveQuadRenderer(ref m_canvas);
            m_baseQuadRenderer.IsVisible = false;
            m_baseQuadRenderer.CornersChanged += InteractiveQuadRenderer_CornersChanged;
            m_baseQuadRenderer.CornersChangeCompleted += InteractiveQuadRenderer_CornersChangeCompleted;
            m_interactiveControl.SpecifyBaseQuadCheckedUnchecked += QuadDetectorSkillInteractiveControl_SpecifyBaseQuadCheckedUnchecked;

            // Add CenterPoint control
            m_centerPointControl.ManipulationDelta += QuadRendererCenterPoint_ManipulationDelta;
            m_centerPointControl.ManipulationCompleted += QuadRendererCenterPoint_ManipulationCompleted;
            Mouse.SetCursor(m_centerPointControl, Windows.UI.Core.CoreCursorType.Pin);
            m_canvas.Children.Add(m_centerPointControl);
            m_interactiveControl.CenterPointCheckedUnchecked += QuadDetectorSkillInteractiveControl_CenterPointCheckedUnchecked;

            Children.Add(m_interactiveControl);
            m_interactiveControl.UpdateDisplayedInputValues(m_quadDetectorBindingFeatureValues);
        }

        /// <summary>
        /// Update the view of the binding values
        /// </summary>
        /// <param name="binding"></param>
        private QuadDetectorBindingInputFeatureValues ExtractBindingValues(ISkillBinding binding)
        {
            QuadDetectorBindingInputFeatureValues result = new QuadDetectorBindingInputFeatureValues();
            result.SubMarginPercentage = (binding["SubRectangleMargin"].FeatureValue as SkillFeatureTensorIntValue).GetAsVectorView()[0];
            result.MaxDetectedQuads = (binding["MaxDetectedQuads"].FeatureValue as SkillFeatureTensorIntValue).GetAsVectorView()[0];
            result.NumberOfEdgePixels = (binding["NumberOfEdgePixels"].FeatureValue as SkillFeatureTensorIntValue).GetAsVectorView()[0];

            var baseQuadFeature = binding["BaseQuad"].FeatureValue;
            var baseQuadFeatureValue = (baseQuadFeature as SkillFeatureTensorFloatValue).GetAsVectorView();
            for (int i = 0; i < baseQuadFeatureValue.Count; i += 2)
            {
                result.BaseQuad[i / 2] = new Point(baseQuadFeatureValue[i], baseQuadFeatureValue[i + 1]);
            }

            result.UseCenterPoint = (binding["UseCenterPoint"].FeatureValue as SkillFeatureTensorBooleanValue).GetAsVectorView()[0];

            var centerPointFeature = binding["CenterPoint"].FeatureValue;
            var centerPointTensor = (centerPointFeature as SkillFeatureTensorFloatValue).GetAsVectorView();
            if (centerPointTensor.Count > 0)
            {
                result.CenterPointCoordinates.X = centerPointTensor[0];
                result.CenterPointCoordinates.Y = centerPointTensor[1];
            }

            return result;
        }

        /// <summary>
        /// Update results displayed
        /// </summary>
        /// <param name="additionalResult"></param>
        /// <returns></returns>
        override public async Task UpdateSkillControlValuesAsync(object additionalResult)
        {
            QuadDetectorBinding binding = (additionalResult as QuadDetectorBinding);
            m_resultQuadCorners = binding.DetectedQuads;
            m_quadsResultRenderer.Update(m_resultQuadCorners);
            m_quadsResultRenderer.IsVisible = true;

            int detectedQuadCount = (binding["DetectedQuadCount"].FeatureValue as SkillFeatureTensorIntValue).GetAsVectorView()[0];
            m_interactiveControl.UpdateDisplayedOutputValues(detectedQuadCount);

            await base.UpdateSkillControlValuesAsync(additionalResult);
        }

        // -- Event handlers -- //
        #region EventHandlers

        override async protected void RunButton_Click(object sender, RoutedEventArgs e)
        {
            // Update ISKillBinding
            QuadDetectorBinding binding = m_skillBinding as QuadDetectorBinding;

            binding.SetLookupRegionCenterCropPercentage(m_quadDetectorBindingFeatureValues.SubMarginPercentage);
            binding.SetMaxQuadCount(m_quadDetectorBindingFeatureValues.MaxDetectedQuads);
            if (m_quadDetectorBindingFeatureValues.UseCenterPoint)
            {
                binding.SetCenterPoint(m_quadDetectorBindingFeatureValues.CenterPointCoordinates);
            }
            else
            {
                binding.SetCenterPoint(null);
            }

            if (m_quadDetectorBindingFeatureValues.UseBaseQuad)
            {
                binding.SetPreviousQuad(m_quadDetectorBindingFeatureValues.BaseQuad);
            }
            else
            {
                binding.SetPreviousQuad(null);
            }
            await binding["NumberOfEdgePixels"].SetFeatureValueAsync(new List<int>() { m_quadDetectorBindingFeatureValues.NumberOfEdgePixels });

            // Invoke event handlers
            base.RunButton_Click(sender, e);
        }

        private void InteractiveQuadRenderer_CornersChangeCompleted(IList<Point> corners)
        {
            m_quadDetectorBindingFeatureValues.BaseQuad = corners.ToList();

            m_runButton.IsEnabled = true;
        }

        private void InteractiveQuadRenderer_CornersChanged(IList<Point> corners)
        {
            m_interactiveControl.UpdateBaseQuadCorners(corners);
        }

        private void QuadDetectorSkillInteractiveControl_MaxQuadValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            m_quadDetectorBindingFeatureValues.MaxDetectedQuads = (int)e.NewValue;
        }

        private void QuadDetectorSkillInteractiveControl_NumberOfPixelsPerEdgeValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            m_quadDetectorBindingFeatureValues.NumberOfEdgePixels = (int)e.NewValue;
        }

        private void QuadDetectorSkillInteractiveControl_SpecifyBaseQuadCheckedUnchecked(object sender, RoutedEventArgs e)
        {
            if (m_baseQuadRenderer.IsVisible)
            {
                m_baseQuadRenderer.IsVisible = false;
            }
            else
            {
                m_baseQuadRenderer.IsVisible = true;
            }

            m_quadDetectorBindingFeatureValues.UseBaseQuad = (sender as CheckBox).IsChecked == true;
        }

        private void QuadDetectorSkillInteractiveControl_SubMargingValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            double sideMarginWidth = m_canvas.ActualWidth * e.NewValue / 100 / 2;
            double sideMarginHeight = m_canvas.ActualHeight * e.NewValue / 100 / 2;

            if (e.NewValue > 0)
            {
                // left side margin
                Canvas.SetLeft(m_margins[0], 0);
                Canvas.SetTop(m_margins[0], 0);
                m_margins[0].Width = sideMarginWidth;
                m_margins[0].Height = m_canvas.ActualHeight;

                // right side margin
                Canvas.SetLeft(m_margins[1], m_canvas.ActualWidth - sideMarginWidth);
                Canvas.SetTop(m_margins[1], 0);
                m_margins[1].Width = sideMarginWidth;
                m_margins[1].Height = m_canvas.ActualHeight;

                // top margin
                Canvas.SetLeft(m_margins[2], sideMarginWidth);
                Canvas.SetTop(m_margins[2], 0);
                m_margins[2].Width = m_canvas.ActualWidth - sideMarginWidth - sideMarginWidth;
                m_margins[2].Height = sideMarginHeight;

                // bottom margin
                Canvas.SetLeft(m_margins[3], sideMarginWidth);
                Canvas.SetTop(m_margins[3], m_canvas.ActualHeight - sideMarginHeight);
                m_margins[3].Width = m_canvas.ActualWidth - sideMarginWidth - sideMarginWidth;
                m_margins[3].Height = sideMarginHeight;
            }
            m_quadDetectorBindingFeatureValues.SubMarginPercentage = (int)e.NewValue;
        }

        private void QuadDetectorSkillInteractiveControl_CenterPointCheckedUnchecked(object sender, RoutedEventArgs e)
        {
            if (m_centerPointControl.Visibility == Visibility.Collapsed)
            {
                Canvas.SetLeft(m_centerPointControl, m_canvas.ActualWidth / 2);
                Canvas.SetTop(m_centerPointControl, m_canvas.ActualHeight / 2);
                m_interactiveControl.UpdateCenterPointDisplay(
                    Canvas.GetLeft(m_centerPointControl) / m_canvas.ActualWidth,
                    Canvas.GetTop(m_centerPointControl) / m_canvas.ActualHeight);

                m_centerPointControl.Visibility = Visibility.Visible;
            }
            else
            {
                m_centerPointControl.Visibility = Visibility.Collapsed;
            }

            m_quadDetectorBindingFeatureValues.UseCenterPoint = (sender as CheckBox).IsChecked == true;
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

            m_baseQuadRenderer.Update(m_baseQuadCorners);
            m_quadsResultRenderer.Update(m_resultQuadCorners);
        }

        private void QuadRendererCenterPoint_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var cornerControl = sender as ImageCropperThumb;
            m_quadDetectorBindingFeatureValues.CenterPointCoordinates = new Point(
                Canvas.GetLeft(cornerControl) / m_canvas.ActualWidth,
                Canvas.GetTop(cornerControl) / m_canvas.ActualHeight);
        }

        private void QuadRendererCenterPoint_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var cornerControl = sender as ImageCropperThumb;
            Canvas.SetLeft(cornerControl, Math.Max(0, Math.Min(m_canvas.ActualWidth, Canvas.GetLeft(cornerControl) + e.Delta.Translation.X)));
            Canvas.SetTop(cornerControl, Math.Max(0, Math.Min(m_canvas.ActualHeight, Canvas.GetTop(cornerControl) + e.Delta.Translation.Y)));
            m_interactiveControl.UpdateCenterPointDisplay(
                Canvas.GetLeft(cornerControl) / m_canvas.ActualWidth,
                Canvas.GetTop(cornerControl) / m_canvas.ActualHeight);
        }

        #endregion EventHandlers
    }
}
