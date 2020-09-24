// Copyright (c) Microsoft Corporation. All rights reserved.

using ImageScanningSample.Helper;
using Microsoft.AI.Skills.SkillInterface;
using Microsoft.AI.Skills.Vision.ImageScanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ImageScanningSample
{
    public sealed partial class CurvedEdgesDetectorSkillInteractiveControl : UserControl
    {
        /// <summary>
        /// CurvedEdgesDetectorSkillInteractiveControl constructor
        /// </summary>
        public CurvedEdgesDetectorSkillInteractiveControl()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Update displayed quadrangle corners
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
    }

    /// <summary>
    /// Custom control to render and manipulate CurvedEdgesDetector inputs & outputs
    /// </summary>
    public class CurvedEdgesDetectorControl : SkillControl
    {
        private CurvedEdgesDetectorSkillInteractiveControl m_interactiveControl = null;
        private Canvas m_canvas = new Canvas();
        private PolylineRenderer m_linesResultRenderer = null;
        private InteractiveQuadRenderer m_inputQuadRenderer;
        private IReadOnlyList<Point> m_resultCurvedEdgesPoints = null;
        private List<Point> m_inputQuadCorners = new List<Point>()
       {
            new Point(0.2, 0.2),
            new Point(0.8, 0.2),
            new Point(0.8, 0.8),
            new Point(0.2, 0.8),
       };

        /// <summary>
        ///  CurvedEdgesDetectorControl constructor
        /// </summary>
        /// <param name="binding"></param>
        public CurvedEdgesDetectorControl(ISkillBinding binding) : base(binding)
        {
            // Update the view of the initial binding values
            m_interactiveControl = new CurvedEdgesDetectorSkillInteractiveControl();

            m_imageGrid.Children.Add(m_canvas);
            m_image.SetValue(Canvas.ZIndexProperty, -1);
            m_image.SizeChanged += Image_SizeChanged;

            // Add line result control
            m_linesResultRenderer = new PolylineRenderer(ref m_canvas);

            m_linesResultRenderer.IsVisible = false;

            Children.Add(m_interactiveControl);
            m_interactiveControl.UpdateBaseQuadCorners(m_inputQuadCorners);

            // Add control to manipulate InputQuad
            m_inputQuadRenderer = new InteractiveQuadRenderer(ref m_canvas);
            m_inputQuadRenderer.IsVisible = true;
            m_inputQuadRenderer.Update(m_inputQuadCorners);
            m_inputQuadRenderer.CornersChanged += InteractiveQuadRenderer_CornersChanged;
            m_inputQuadRenderer.CornersChangeCompleted += InteractiveQuadRenderer_CornersChangeCompleted;
        }

        /// <summary>
        /// Update results displayed
        /// </summary>
        /// <param name="additionalResult"></param>
        /// <returns></returns>
        override public async Task UpdateSkillControlValuesAsync(object additionalResult)
        {
            m_resultCurvedEdgesPoints = (additionalResult as CurvedEdgesDetectorBinding).DetectedCurvedEdges;

            m_linesResultRenderer.Update(m_resultCurvedEdgesPoints);
            m_linesResultRenderer.IsVisible = true;

            await base.UpdateSkillControlValuesAsync(additionalResult);
        }

        // -- Event handlers -- //
        #region EventHandlers

        private void InteractiveQuadRenderer_CornersChangeCompleted(List<Point> corners)
        {
            m_inputQuadCorners = corners.ToList();
        }

        private void InteractiveQuadRenderer_CornersChanged(List<Point> corners)
        {
            m_interactiveControl.UpdateBaseQuadCorners(corners);
        }

        override async protected void RunButton_Click(object sender, RoutedEventArgs e)
        {
            // Update ISKillBinding
            CurvedEdgesDetectorBinding binding = m_skillBinding as CurvedEdgesDetectorBinding;

            await binding.SetInputQuadAsync(m_inputQuadCorners);

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

            m_linesResultRenderer.Update(m_resultCurvedEdgesPoints);
            m_inputQuadRenderer.Update(m_inputQuadCorners);
        }

        #endregion EventHandlers
    }
}
