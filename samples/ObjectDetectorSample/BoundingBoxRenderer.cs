// Copyright (C) Microsoft Corporation. All rights reserved.

using Microsoft.AI.Skills.Vision.ObjectDetectorPreview;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace ObjectDetectorSkill_SampleApp
{
    /// <summary>
    /// Helper class to render object detections
    /// </summary>
    internal class BoundingBoxRenderer
    {
        private Canvas m_canvas;

        // Pre-populate rectangles/textblocks to avoid clearing and re-creating on each frame
        private Rectangle[] m_rectangles;
        private TextBlock[] m_textBlocks;

        /// <summary>
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="maxBoxes"></param>
        /// <param name="lineThickness"></param>
        /// <param name="colorBrush">Default Colors.SpringGreen color brush if not specified</param>
        public BoundingBoxRenderer(Canvas canvas, int maxBoxes = 50, int lineThickness = 2, SolidColorBrush colorBrush = null)
        {
            m_rectangles = new Rectangle[maxBoxes];
            m_textBlocks = new TextBlock[maxBoxes];
            if (colorBrush == null)
            {
                colorBrush = new SolidColorBrush(Colors.SpringGreen);
            }

            m_canvas = canvas;
            for (int i = 0; i < maxBoxes; i++)
            {
                // Create rectangles
                m_rectangles[i] = new Rectangle();
                // Default configuration
                m_rectangles[i].Stroke = colorBrush;
                m_rectangles[i].StrokeThickness = lineThickness;
                // Hide
                m_rectangles[i].Visibility = Visibility.Collapsed;
                // Add to canvas
                m_canvas.Children.Add(m_rectangles[i]);

                // Create textblocks
                m_textBlocks[i] = new TextBlock();
                // Default configuration
                m_textBlocks[i].Foreground = colorBrush;
                m_textBlocks[i].FontSize = 10;
                // Hide
                m_textBlocks[i].Visibility = Visibility.Collapsed;
                // Add to canvas
                m_canvas.Children.Add(m_textBlocks[i]);
            }
        }

        /// <summary>
        /// Render bounding boxes from ObjectDetections
        /// </summary>
        /// <param name="detections"></param>
        public void Render(IReadOnlyList<ObjectDetectorResult> detections)
        {
            int i = 0;
            // Render detections up to MAX_BOXES
            for (i = 0; i < detections.Count && i < m_rectangles.Length; i++)
            {
                // Render bounding box
                m_rectangles[i].Width = detections[i].Rect.Width * m_canvas.ActualWidth;
                m_rectangles[i].Height = detections[i].Rect.Height * m_canvas.ActualHeight;
                Canvas.SetLeft(m_rectangles[i], detections[i].Rect.X * m_canvas.ActualWidth);
                Canvas.SetTop(m_rectangles[i], detections[i].Rect.Y * m_canvas.ActualHeight);
                m_rectangles[i].Visibility = Visibility.Visible;

                // Render text label
                m_textBlocks[i].Text = detections[i].Kind.ToString();
                Canvas.SetLeft(m_textBlocks[i], detections[i].Rect.X * m_canvas.ActualWidth);
                Canvas.SetTop(m_textBlocks[i], detections[i].Rect.Y * m_canvas.ActualHeight + m_rectangles[i].Height);
                m_textBlocks[i].Visibility = Visibility.Visible;
            }
            // Hide all remaining boxes
            for (; i < m_rectangles.Length; i++)
            {
                // Early exit: Everything after i will already be collapsed
                if (m_rectangles[i].Visibility == Visibility.Collapsed)
                {
                    break;
                }
                m_rectangles[i].Visibility = Visibility.Collapsed;
                m_textBlocks[i].Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Resize canvas and rendered bounding boxes
        /// </summary>
        public void Resize(SizeChangedEventArgs args)
        {
            // Resize canvas
            m_canvas.Width = args.NewSize.Width;
            m_canvas.Height = args.NewSize.Height;

            // Resize rendered bboxes
            for (int i = 0; i < m_rectangles.Length && m_rectangles[i].Visibility == Visibility.Visible; i++)
            {
                // Update bounding box
                m_rectangles[i].Width *= args.NewSize.Width / args.PreviousSize.Width;
                m_rectangles[i].Height *= args.NewSize.Height / args.PreviousSize.Height;
                Canvas.SetLeft(m_rectangles[i], Canvas.GetLeft(m_rectangles[i]) * args.NewSize.Width / args.PreviousSize.Width);
                Canvas.SetTop(m_rectangles[i], Canvas.GetTop(m_rectangles[i]) * args.NewSize.Height / args.PreviousSize.Height);

                // Update text label
                Canvas.SetLeft(m_textBlocks[i], Canvas.GetLeft(m_textBlocks[i]) * args.NewSize.Width / args.PreviousSize.Width);
                Canvas.SetTop(m_textBlocks[i], Canvas.GetTop(m_textBlocks[i]) * args.NewSize.Height / args.PreviousSize.Height);
            }
        }
    }
}
