// Copyright (C) Microsoft Corporation. All rights reserved.

using Microsoft.AI.Skills.Vision.SkeletalDetector;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace SkeletalDetectorSample
{
    /// <summary>
    /// Convenience class for rendering skeletons on screen
    /// </summary>    
    internal class BodyRenderer
    {
        protected Canvas m_canvas;

        protected const int LINES_THICKNESS = 3;
        protected static SolidColorBrush[] m_colorBrushes = new SolidColorBrush[7]
        {
                new SolidColorBrush(Colors.Red),
                new SolidColorBrush(Colors.Yellow),
                new SolidColorBrush(Colors.Green),
                new SolidColorBrush(Colors.Blue),
                new SolidColorBrush(Colors.Violet),
                new SolidColorBrush(Colors.Gray),
                new SolidColorBrush(Colors.Brown),
        };

        public BodyRenderer(Canvas canvas)
        {
            m_canvas = canvas;
            m_canvas.Children.Add(new Line());
            IsVisible = false;
        }

        /// <summary>
        /// Updates the skeletal stick figure renderer. A secondary parameter may be passed in 
        /// to render tooltips for joint labels. 
        /// </summary>
        /// <param name="bodies"></param>
        /// <param name="enableLabels"></param>
        public void Update(IReadOnlyList<SkeletalDetectorResult> bodies, bool enableLabels = false)
        {
            m_canvas.Children.Clear();
            for (int i = 0; i < bodies?.Count; ++i)
            {
                Dictionary<string, Joint> map = new Dictionary<string, Joint>();

                var body = bodies.ElementAt(i);
                foreach (var limb in body.Limbs)
                {
                    var line = new Line() { Stroke = m_colorBrushes[i % m_colorBrushes.Count()], StrokeThickness = LINES_THICKNESS };
                    var point1 = limb.Joint1;
                    var point2 = limb.Joint2;
                    line.X1 = 0.0f;
                    line.Y1 = 0.0f;
                    line.X2 = (point2.X - point1.X) * m_canvas.Width;
                    line.Y2 = (point2.Y - point1.Y) * m_canvas.Height;
                    m_canvas.Children.Add(line);
                    Canvas.SetLeft(line, point1.X * m_canvas.Width);
                    Canvas.SetTop(line, point1.Y * m_canvas.Height);

                    map[point1.Label.ToString()] = point1;
                    map[point2.Label.ToString()] = point2;
                }

                if (enableLabels)
                {
                    foreach (KeyValuePair<string, Joint> pair in map)
                    {
                        Ellipse point = new Ellipse()
                        {
                            Stroke = m_colorBrushes[i % m_colorBrushes.Count()],
                            Fill = m_colorBrushes[i % m_colorBrushes.Count()],
                            StrokeThickness = LINES_THICKNESS,
                            Height = 10,
                            Width = 10,
                        };
                        ToolTip toolTip = new ToolTip();
                        toolTip.Content = pair.Key;
                        ToolTipService.SetToolTip(point, toolTip);
                        m_canvas.Children.Add(point);
                        Canvas.SetLeft(point, pair.Value.X * m_canvas.Width - point.Width / 2);
                        Canvas.SetTop(point, pair.Value.Y * m_canvas.Height - point.Height / 2);
                    }
                }
            }
        }

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
    }
}
