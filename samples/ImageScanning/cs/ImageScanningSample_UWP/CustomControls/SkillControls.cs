// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.AI.Skills.SkillInterface;
using Microsoft.AI.Skills.Vision.ImageScanning;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace ImageScanningSample.Helper
{
    /// <summary>
    /// Helper base class to render input image and results correctly in the UI
    /// </summary>
    public abstract class SkillControl : StackPanel
    {
        protected ISkillBinding m_skillBinding;
        protected Grid m_imageGrid = new Grid() { MaxWidth = 400, MaxHeight = 400 };
        protected Image m_image = new Image() { Stretch = Stretch.Uniform };
        protected SoftwareBitmapSource m_softwareBitmapSource = new SoftwareBitmapSource();
        protected int m_frameWidth;
        protected int m_frameHeight;
        protected ProgressRing m_progressRing = new ProgressRing() { IsActive = false, Width = 200, Height = 200 };
        protected Button m_runButton = new Button() { Content = "Run", IsEnabled = true };
        protected TextBlock m_perfTextBlock = new TextBlock();

        public delegate void RunSkillClickedHandler(ISkillBinding binding);
        public event RunSkillClickedHandler RunButtonClicked;
        public float BindTime { get; set; }
        public float EvalTime { get; set; }


        /// <summary>
        /// SkillControl factory to instantiate known derivatives
        /// </summary>
        /// <param name="binding"></param>
        /// <returns></returns>
        public static SkillControl CreateControl(ISkillBinding binding)
        {
            if (binding is QuadDetectorBinding)
            {
                return new QuadDetectorControl(binding);
            }
            else if (binding is LiveQuadDetectorBinding)
            {
                return new LiveQuadDetectorControl(binding);
            }
            else if (binding is ImageRectifierBinding)
            {
                return new ImageRectifierControl(binding);
            }
            else if (binding is ImageCleanerBinding)
            {
                return new ImageCleanerControl(binding);
            }
            else if (binding is CurvedEdgesDetectorBinding)
            {
                return new CurvedEdgesDetectorControl(binding);
            }
            else if (binding is QuadEdgesDetectorBinding)
            {
                return new QuadEdgesDetectorControl(binding);
            }
            else
            {
                throw new ArgumentException("Unexpected skill binding type specified");
            }
        }

        /// <summary>
        /// SkillControl class constructor
        /// </summary>
        public SkillControl(ISkillBinding binding)
        {
            Orientation = Orientation.Horizontal;
            m_skillBinding = binding;
            m_imageGrid.Children.Add(m_image);
            m_imageGrid.Children.Add(m_progressRing);

            Children.Add(new StackPanel() { Children = { m_imageGrid, m_runButton, m_perfTextBlock } });
            m_runButton.Click += RunButton_Click;
        }

        /// <summary>
        /// Triggered when Run button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        virtual protected void RunButton_Click(object sender, RoutedEventArgs e)
        {
            m_progressRing.IsActive = true;
            if (RunButtonClicked != null)
            {
                m_runButton.IsEnabled = false;
                RunButtonClicked.Invoke(m_skillBinding);
            }
        }

        /// <summary>
        /// Expose input image in UI
        /// </summary>
        /// <param name="softwareBitmap"></param>
        /// <returns></returns>
        virtual public async Task UpdateSkillControlInputImageAsync(SoftwareBitmap softwareBitmap)
        {
            if (softwareBitmap != null)
            {
                await m_softwareBitmapSource.SetBitmapAsync(SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied));
                m_frameHeight = softwareBitmap.PixelHeight;
                m_frameWidth = softwareBitmap.PixelWidth;
                m_image.Source = m_softwareBitmapSource;
            }
        }

        /// <summary>
        /// Expose results in UI, called after eval has completed
        /// </summary>
        /// <param name="softwareBitmap"></param>
        /// <returns></returns>
        virtual public async Task UpdateSkillControlValuesAsync(object additionalResult)
        {
            m_progressRing.IsActive = false;
            m_runButton.IsEnabled = true;
            m_perfTextBlock.Text = $"Bind time: {BindTime}ms | Eval Time: {EvalTime}ms";
        }

        /// <summary>
        /// Reset UI state
        /// </summary>
        /// <param name="softwareBitmap"></param>
        /// <returns></returns>
        virtual public void Reset()
        {
            m_image.Source = null; ;
            m_progressRing.IsActive = true;
        }
    }

    // -- Shape rendering helpers -- //
    #region ShapeRenderingHelpers

    /// <summary>
    /// Convenience class for rendering a set of lines on screen
    /// </summary>
    internal class LineSetRenderer
    {
        private Canvas m_canvas;
        private const int LINE_THICKNESS = 2;
        private const int ELLIPSE_RADIUS = 5;
        private SolidColorBrush m_lineBrush;
        private List<Line> m_lines = new List<Line>();
        private List<Ellipse> m_ellipses = new List<Ellipse>();
        private Visibility m_currentVisibility = Visibility.Visible;

        /// <summary>
        /// LineSetRenderer constructor
        /// </summary>
        /// <param name="canvas"></param>
        public LineSetRenderer(ref Canvas canvas, Color lineColor)
        {
            m_canvas = canvas;
            m_lineBrush = new SolidColorBrush(lineColor);
        }

        /// <summary>
        /// Set visibility of line rendering canvas control
        /// </summary>
        public bool IsVisible
        {
            get
            {
                return m_currentVisibility == Visibility.Visible;
            }
            set
            {
                m_currentVisibility = value == true ? Visibility.Visible : Visibility.Collapsed;
                foreach (var line in m_lines)
                {
                    line.Visibility = m_currentVisibility;
                }
                foreach (var ellipse in m_ellipses)
                {
                    ellipse.Visibility = m_currentVisibility;
                }
            }
        }

        /// <summary>
        /// Update coordinates of rendered line points within this UI control
        /// </summary>
        /// <param name="corners"></param>
        public void Update(IReadOnlyList<Point> points)
        {
            if (points == null)
            {
                return;
            }
            if (points.Count % 2 != 0)
            {
                throw new Exception("you can only pass a List of pairs of points to this method");
            }

            var lineCount = points.Count / 2;
            // If we need to render more lines than what we have instantiated so far, fill up the inventory of lines
            if (m_lines.Count < lineCount)
            {
                int originalCount = m_lines.Count;
                for (int i = 0; i < (lineCount - originalCount); i++)
                {
                    m_lines.Add(new Line() { Stroke = m_lineBrush, StrokeThickness = LINE_THICKNESS });
                    m_canvas.Children.Add(m_lines.Last());
                    m_ellipses.Add(new Ellipse() { Stroke = m_lineBrush, StrokeThickness = LINE_THICKNESS, Height = ELLIPSE_RADIUS * 2, Width = ELLIPSE_RADIUS * 2 });
                    m_canvas.Children.Add(m_ellipses.Last());
                    m_ellipses.Add(new Ellipse() { Stroke = m_lineBrush, StrokeThickness = LINE_THICKNESS, Height = ELLIPSE_RADIUS * 2, Width = ELLIPSE_RADIUS * 2 });
                    m_canvas.Children.Add(m_ellipses.Last());
                }
            }

            int lineIndex = 0;

            // Show and update previously instantiated lines needed
            for (int i = 0; i < points.Count - 1; i += 2)
            {
                var line = m_lines[lineIndex];
                line.X1 = 0.0f;
                line.Y1 = 0.0f;
                line.X2 = (points[i + 1].X - points[i].X) * m_canvas.ActualWidth;
                line.Y2 = (points[i + 1].Y - points[i].Y) * m_canvas.ActualHeight;
                Canvas.SetLeft(line, points[i].X * m_canvas.ActualWidth);
                Canvas.SetTop(line, points[i].Y * m_canvas.ActualHeight);

                Canvas.SetLeft(m_ellipses[i], points[i].X * m_canvas.ActualWidth - ELLIPSE_RADIUS);
                Canvas.SetTop(m_ellipses[i], points[i].Y * m_canvas.ActualHeight - ELLIPSE_RADIUS);

                Canvas.SetLeft(m_ellipses[i + 1], points[i + 1].X * m_canvas.ActualWidth - ELLIPSE_RADIUS);
                Canvas.SetTop(m_ellipses[i + 1], points[i + 1].Y * m_canvas.ActualHeight - ELLIPSE_RADIUS);

                lineIndex++;
            }

            // Hide previously instantiated lines not needed this time
            for (; lineIndex < m_lines.Count; lineIndex++)
            {
                var line = m_lines[lineIndex];
                line.X2 = 0.0f;
                line.Y2 = 0.0f;
                Canvas.SetLeft(line, 0.0f);
                Canvas.SetTop(line, 0.0f);

                Canvas.SetLeft(m_ellipses[lineIndex * 2], 0.0f);
                Canvas.SetTop(m_ellipses[lineIndex * 2], 0.0f);

                Canvas.SetLeft(m_ellipses[lineIndex * 2 + 1], 0.0f);
                Canvas.SetTop(m_ellipses[lineIndex * 2 + 1], 0.0f);
            }
        }
    }

    /// <summary>
    /// Convenience class for rendering a polyline on screen
    /// </summary>
    internal class PolylineRenderer
    {
        private Canvas m_canvas;
        private const int LINE_THICKNESS = 2;
        private Polyline m_polyline = null;
        private Visibility m_currentVisibility = Visibility.Visible;

        /// <summary>
        /// PolylineRenderer constructor
        /// </summary>
        /// <param name="canvas"></param>
        public PolylineRenderer(ref Canvas canvas)
        {
            m_canvas = canvas;
            m_polyline = new Polyline() { Stroke = new SolidColorBrush(Colors.Green), StrokeThickness = LINE_THICKNESS };
            m_canvas.Children.Add(m_polyline);
        }

        /// <summary>
        /// Set visibility of line rendering canvas control
        /// </summary>
        public bool IsVisible
        {
            get
            {
                return m_currentVisibility == Visibility.Visible;
            }
            set
            {
                m_currentVisibility = value == true ? Visibility.Visible : Visibility.Collapsed;
                m_polyline.Visibility = m_currentVisibility;
            }
        }

        /// <summary>
        /// Update coordinates of rendered line points within this UI control
        /// </summary>
        /// <param name="corners"></param>
        public void Update(IReadOnlyList<Point> points)
        {
            if (points == null)
            {
                return;
            }

            var lineCount = points.Count;
            var polyPoints = new PointCollection();
            foreach (var point in points)
            {
                polyPoints.Add(new Point(point.X * m_canvas.ActualWidth, point.Y * m_canvas.ActualHeight));
            }
            m_polyline.Points = polyPoints;
            Canvas.SetLeft(m_polyline, 0.0f);
            Canvas.SetTop(m_polyline, 0.0f);
        }
    }

    /// <summary>
    /// Convenience class for rendering quadrilateral contours on screen
    /// </summary>
    internal class QuadSetRenderer
    {
        private Canvas m_canvas;
        private const int QUAD_LINES_THICKNESS = 2;
        private List<Line[]> m_quadLines = new List<Line[]>();
        private Visibility m_currentVisibility = Visibility.Visible;

        /// <summary>
        /// QuadSetRenderer constructor
        /// </summary>
        /// <param name="canvas"></param>
        public QuadSetRenderer(ref Canvas canvas, int quadCount)
        {
            m_canvas = canvas;
            for (int i = 0; i < quadCount; i++)
            {
                m_quadLines.Add(new Line[4]
                {
                    new Line() { Stroke = new SolidColorBrush(Colors.Red), StrokeThickness = QUAD_LINES_THICKNESS },
                    new Line() { Stroke = new SolidColorBrush(Colors.Red), StrokeThickness = QUAD_LINES_THICKNESS },
                    new Line() { Stroke = new SolidColorBrush(Colors.Red), StrokeThickness = QUAD_LINES_THICKNESS },
                    new Line() { Stroke = new SolidColorBrush(Colors.Red), StrokeThickness = QUAD_LINES_THICKNESS }
                });
            }
            foreach (var quad in m_quadLines)
            {
                foreach (var line in quad)
                {
                    m_canvas.Children.Add(line);
                }
            }
        }

        /// <summary>
        /// Set visibility of quad rendering canvas control
        /// </summary>
        public bool IsVisible
        {
            get
            {
                return m_currentVisibility == Visibility.Visible;
            }
            set
            {
                m_currentVisibility = value == true ? Visibility.Visible : Visibility.Collapsed;
                foreach (var quad in m_quadLines)
                {
                    foreach (var line in quad)
                    {
                        line.Visibility = m_currentVisibility;
                    }
                }
            }
        }

        /// <summary>
        /// Update coordinates of rendered quadrilateral shape's corners within this UI control
        /// </summary>
        /// <param name="corners"></param>
        public void Update(IReadOnlyList<Point> corners)
        {
            if (corners == null)
            {
                return;
            }
            if (corners.Count % 4 != 0)
            {
                throw new Exception("you can only pass a List of groups of 4 corner points to this method");
            }
            int quadCount = 0;

            // Place each quad on the canvas
            for (; quadCount < m_quadLines.Count; quadCount++)
            {
                var quadLines = m_quadLines[quadCount];
                for (int i = 0; i < 4; i++)
                {
                    int cornerIndex = quadCount * 4 + i;
                    var line = quadLines[i];
                    line.X1 = 0.0f;
                    line.Y1 = 0.0f;

                    // Show quad lines on canvas for quads previsouly instantiated and needed
                    if (quadCount < corners.Count / 4)
                    {
                        line.X2 = (corners[quadCount * 4 + ((i + 1) % 4)].X - corners[cornerIndex].X) * m_canvas.ActualWidth;
                        line.Y2 = (corners[quadCount * 4 + ((i + 1) % 4)].Y - corners[cornerIndex].Y) * m_canvas.ActualHeight;
                        Canvas.SetLeft(line, corners[cornerIndex].X * m_canvas.ActualWidth);
                        Canvas.SetTop(line, corners[cornerIndex].Y * m_canvas.ActualHeight);
                    }
                    // Hide quad line on canvas for quads instantiated but not needed this time
                    else
                    {
                        line.X2 = 0.0f;
                        line.Y2 = 0.0f;
                        Canvas.SetLeft(line, 0.0f);
                        Canvas.SetTop(line, 0.0f);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Convenience class for rendering and interact with a quadrilateral's contour on screen
    /// </summary>
    internal class InteractiveQuadRenderer
    {
        public delegate void CornersChangedHandler(List<Point> corners);
        public event CornersChangedHandler CornersChanged;
        public event CornersChangedHandler CornersChangeCompleted;

        private Canvas m_canvas;
        private Polygon m_polygon = new Polygon();
        private ImageCropperThumb[] m_cornerControls = new ImageCropperThumb[4];

        /// <summary>
        /// InteractiveQuadRenderer constructor, takes a ref to an existing Canvas to attach itselfs to it
        /// </summary>
        /// <param name="canvas"></param>
        public InteractiveQuadRenderer(ref Canvas canvas)
        {
            m_canvas = canvas;

            m_polygon = new Polygon()
            {
                Points = new PointCollection() { new Point(), new Point(), new Point(), new Point() },
                Fill = new SolidColorBrush(new Color() { B = 0, G = 0, R = 255, A = 100 })
            };
            m_canvas.Children.Add(m_polygon);

            for (int cornerIndex = 0; cornerIndex < 4; cornerIndex++)
            {
                m_cornerControls[cornerIndex] = new ImageCropperThumb();
                Mouse.SetCursor(m_cornerControls[cornerIndex], Windows.UI.Core.CoreCursorType.SizeAll);
                m_cornerControls[cornerIndex].ManipulationDelta += InteractiveQuadRenderer_ManipulationDelta;
                m_cornerControls[cornerIndex].ManipulationCompleted += InteractiveQuadRenderer_ManipulationCompleted;
            }
            foreach (var corner in m_cornerControls)
            {
                m_canvas.Children.Add(corner);
            }
        }

        /// <summary>
        /// Quad corner ManipulationCompleted event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InteractiveQuadRenderer_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            // Notify a change in corner value happened
            if (CornersChangeCompleted != null)
            {
                CornersChangeCompleted.Invoke(m_polygon.Points.Select((p) =>
                {
                    p.X /= m_canvas.ActualWidth;
                    p.Y /= m_canvas.ActualHeight;
                    return p;
                }).ToList());
            }
        }

        /// <summary>
        /// Handle manipulation delta of quad corner
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InteractiveQuadRenderer_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var cornerControl = sender as ImageCropperThumb;
            double x = Math.Min(m_canvas.ActualWidth, Math.Max(0.0, Canvas.GetLeft(cornerControl) + e.Delta.Translation.X));
            double y = Math.Min(m_canvas.ActualHeight, Math.Max(0.0, Canvas.GetTop(cornerControl) + e.Delta.Translation.Y));
            Canvas.SetLeft(cornerControl, x);
            Canvas.SetTop(cornerControl, y);

            // Refresh polygon drawing
            var points = new PointCollection();
            foreach (var corner in m_cornerControls)
            {
                points.Add(new Point(
                    Math.Min(m_canvas.ActualWidth, Math.Max(0.0, Canvas.GetLeft(corner))),
                    Math.Min(m_canvas.ActualHeight, Math.Max(0.0, Canvas.GetTop(corner)))));
            }
            m_polygon.Points = points;

            // Notify a change in corner value happened
            if (CornersChanged != null)
            {
                CornersChanged.Invoke(points.Select((p) =>
                {
                    p.X /= m_canvas.ActualWidth;
                    p.Y /= m_canvas.ActualHeight;
                    return p;
                }).ToList());
            }
        }

        /// <summary>
        /// Set visibility of interactive quad rendering canvas control
        /// </summary>
        public bool IsVisible
        {
            get
            {
                return m_polygon.Visibility == Visibility.Visible;
            }
            set
            {
                var visibilityToSet = value ? Visibility.Visible : Visibility.Collapsed;
                m_polygon.Visibility = visibilityToSet;
                foreach (var cornerControl in m_cornerControls)
                {
                    cornerControl.Visibility = visibilityToSet;
                }

                // Refresh corner coordinates
                if (visibilityToSet == Visibility.Visible && CornersChanged != null)
                {
                    CornersChanged.Invoke(m_polygon.Points.Select((p) =>
                    {
                        p.X /= m_canvas.ActualWidth;
                        p.Y /= m_canvas.ActualHeight;
                        return p;
                    }).ToList());
                }
            }
        }

        /// <summary>
        /// Update coordinates of rendered quadrilateral shape's corners within this UI control
        /// </summary>
        /// <param name="corners"></param>
        public void Update(IReadOnlyList<Point> corners)
        {
            if (corners == null)
            {
                return;
            }
            if (corners.Count % 4 != 0)
            {
                throw new Exception("you can only pass a List of 4 corner points to this method");
            }

            int polygonCount = 0;
            {
                var points = new PointCollection();
                for (int i = 0; i < 4; i++)
                {
                    int cornerIndex = polygonCount * 4 + i;
                    var cornerControl = m_cornerControls[i];
                    points.Add(new Point(corners[cornerIndex].X * m_canvas.ActualWidth, corners[cornerIndex].Y * m_canvas.ActualHeight));
                    m_polygon.Points = points;

                    // Show quad line on canvas
                    if (polygonCount < corners.Count / 4)
                    {
                        Canvas.SetLeft(cornerControl, corners[cornerIndex].X * m_canvas.ActualWidth);
                        Canvas.SetTop(cornerControl, corners[cornerIndex].Y * m_canvas.ActualHeight);
                    }
                }
            }
        }
    }

    #endregion ShapeRenderingHelpers
}
