// Copyright (C) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace ObjectTrackerSkillSample
{
    /// <summary>
    /// Helper struct for storing tracker results
    /// </summary>
    internal struct TrackerResult
    {
        public string label;
        public Rect boundingRect;
        public bool succeeded;
    }

    /// <summary>
    /// Convenience class for rendering a rectangle on screen
    /// </summary>
    internal class ObjectTrackRenderer
    {
        private readonly SolidColorBrush successBrush = new SolidColorBrush(Windows.UI.Colors.Green);
        private readonly SolidColorBrush failBrush = new SolidColorBrush(Windows.UI.Colors.DarkOrange);
        private readonly SolidColorBrush newRectBrush = new SolidColorBrush(Windows.UI.Colors.Gold);
        private readonly double lineThickness = 2.0;
        private readonly SolidColorBrush fillBrush = new SolidColorBrush(Windows.UI.Colors.Transparent);
        private Canvas m_canvas;
        private readonly object m_lock = new object();

        /// <summary>
        /// ObjectTrackRenderer constructor
        /// </summary>
        /// <param name="canvas"></param>
        public ObjectTrackRenderer(Canvas canvas)
        {
            m_canvas = canvas;
            IsVisible = true;
        }

        /// <summary>
        /// Set visibility of ObjectTrackRenderer UI controls
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
        /// Clears the ObjectTrackRenderer canvas
        /// </summary>
        public void ClearCanvas()
        {
            lock (m_lock)
            {
                m_canvas.Children.Clear();
            }
        }

        /// <summary>
        /// Takes the webcam image and ObjectTracker results and assembles the visualization onto the Canvas.
        /// </summary>
        /// <param name="trackerResult"></param>
        /// <param name="showPaths"></param>
        public void RenderTrackerResults(IReadOnlyList<IEnumerable<TrackerResult>> histories, bool showPaths = false)
        {
            double widthScaleFactor = m_canvas.ActualWidth;
            double heightScaleFactor = m_canvas.ActualHeight;

            lock (m_lock)
            {
                foreach (var history in histories)
                {
                    // Create a rectangle element for displaying the motion predictor box
                    Rect rect = history.Last().boundingRect;
                    Rectangle box = new Rectangle();
                    box.Width = rect.Width * widthScaleFactor;
                    box.Height = rect.Height * heightScaleFactor;
                    box.Fill = this.fillBrush;
                    box.Stroke = history.Last().succeeded ? this.successBrush : this.failBrush;
                    box.StrokeThickness = this.lineThickness;
                    box.Margin = new Thickness(
                        rect.X * widthScaleFactor,
                        rect.Y * heightScaleFactor,
                        0, 0);
                    m_canvas.Children.Add(box);

                    // Render text label, if any
                    string label = history.Last().label;
                    if (!String.IsNullOrEmpty(label))
                    {
                        TextBlock uiLabel = new TextBlock();
                        uiLabel.Text = label;
                        uiLabel.Foreground = box.Stroke;
                        uiLabel.FontSize = 18;
                        uiLabel.Margin = new Thickness(
                            rect.X * widthScaleFactor + 2,
                            rect.Y * heightScaleFactor + 2,
                            0, 0);
                        m_canvas.Children.Add(uiLabel);
                    }

                    if (showPaths)
                    {
                        Polyline path = new Polyline();
                        bool prevResultSuccess = true;
                        foreach (TrackerResult result in history)
                        {
                            Rect historyRect = result.boundingRect;
                            // Adjust values for scale
                            double pointX = (historyRect.X + historyRect.Width / 2) * widthScaleFactor;
                            double pointY = (historyRect.Y + historyRect.Height / 2) * heightScaleFactor;
                            path.Points.Add(new Point(pointX, pointY));

                            // Break up lines based on success status as necessary
                            if (result.succeeded != prevResultSuccess)
                            {
                                // Commit/terminate current line
                                path.Stroke = prevResultSuccess ? this.successBrush : this.failBrush;
                                path.StrokeThickness = this.lineThickness;
                                m_canvas.Children.Add(path);

                                // Start new line
                                path = new Polyline();
                                path.Points.Add(new Point(pointX, pointY));
                                prevResultSuccess = result.succeeded;
                            }
                        }
                        // Commit/terminate last line
                        path.Stroke = prevResultSuccess ? this.successBrush : this.failBrush;
                        path.StrokeThickness = this.lineThickness;
                        m_canvas.Children.Add(path);
                    }
                }
            }
        }

        /// <summary>
        /// Renders a collection of Rects onto the Canvas
        /// </summary>
        /// <param name="rects"></param>
        public void RenderRects(IReadOnlyList<Rect> rects)
        {
            double widthScaleFactor = m_canvas.ActualWidth;
            double heightScaleFactor = m_canvas.ActualHeight;

            lock (m_lock)
            {
                foreach (var rect in rects)
                {
                    Rectangle box = new Rectangle();
                    box.Width = rect.Width * widthScaleFactor;
                    box.Height = rect.Height * heightScaleFactor;
                    box.Fill = this.fillBrush;
                    box.Stroke = this.newRectBrush;
                    box.StrokeThickness = this.lineThickness;
                    box.Margin = new Thickness(
                        rect.X * widthScaleFactor,
                        rect.Y * heightScaleFactor,
                        0, 0);
                    m_canvas.Children.Add(box);
                }
            }
        }
    }
}
