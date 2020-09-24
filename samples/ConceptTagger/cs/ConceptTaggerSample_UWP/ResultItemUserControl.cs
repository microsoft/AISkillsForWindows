// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.AI.Skills.Vision.ConceptTagger;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace ConceptTaggerSample.Helper
{
    /// <summary>
    /// Helper class to render image and results correctly in the UI
    /// </summary>
    public class ResultItemUserControl : StackPanel
    {
        public float BindTime { get; set; }
        public float EvalTime { get; set; }
        public IEnumerable<ConceptTagScore> Results { get; private set; }

        /// <summary>
        /// ResultItem class constructor
        /// </summary>
        public ResultItemUserControl()
        {
            Children.Add(m_progressRing);
            Children.Add(m_image);
            Children.Add(m_hashtagContainer);
            Children.Add(m_expander);
        }

        /// <summary>
        /// Expose image and results in UI
        /// </summary>
        /// <param name="softwareBitmap"></param>
        /// <returns></returns>
        public async Task UpdateResultItemImageAsync(SoftwareBitmap softwareBitmap)
        {
            await m_softwareBitmapSource.SetBitmapAsync(SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied));
            m_image.Source = m_softwareBitmapSource;
        }

        /// <summary>
        /// Update the score displayed in the ResultItem
        /// </summary>
        /// <param name="result"></param>
        public void UpdateResultItemScore(IEnumerable<ConceptTagScore> results)
        {
            Results = results;
            if (results != null)
            {
                foreach (var tag in results)
                {
                    var hashTag = new Border() { BorderThickness = new Thickness(2.0), CornerRadius = new CornerRadius(5.0), Background = new SolidColorBrush(Windows.UI.Colors.DarkViolet) };
                    hashTag.Child = new TextBlock() { Text = "#" + tag.Name.Split(';')[0], Foreground = new SolidColorBrush(Windows.UI.Colors.White) };
                    m_hashtagContainer.Children.Add(hashTag);
                }

                m_expander.Content = new ListView() { ItemsSource = results.Select(x => new HeaderedContentControl() { Header = x.Name, Content = x.Score }) };
            }
            else
            {
                var hashTag = new Border() { BorderThickness = new Thickness(2.0), CornerRadius = new CornerRadius(5.0), Background = new SolidColorBrush(Windows.UI.Colors.Red) };
                hashTag.Child = new TextBlock() { Text = "None", Foreground = new SolidColorBrush(Windows.UI.Colors.White) };
                m_hashtagContainer.Children.Add(hashTag);
                m_expander.Visibility = Visibility.Collapsed;
            }
            ToolTipService.SetToolTip(this, $"Bind: {BindTime}ms\nEval: {EvalTime}ms");

            m_progressRing.IsActive = false;
            m_progressRing.Visibility = Visibility.Collapsed;
        }

        private WrapPanel m_hashtagContainer = new WrapPanel() { Width = 200 };
        private Expander m_expander = new Expander() { Header = "Scores", IsExpanded = false, ExpandDirection = ExpandDirection.Down };
        private Image m_image = new Image() { MaxWidth = 200, MaxHeight = 200, Stretch = Stretch.Uniform };
        private SoftwareBitmapSource m_softwareBitmapSource = new SoftwareBitmapSource();
        private ProgressRing m_progressRing = new ProgressRing() { IsActive = true };
    }
}
