// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Media;
using Microsoft.AI.Skills.Vision.ConceptTaggerPreview;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System.Threading;
using System.Collections.Generic;
using Microsoft.AI.Skills.SkillInterfacePreview;
using System.Diagnostics;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;

namespace ConceptTaggerSample
{
    /// <summary>
    /// Main page to this application
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private ConceptTaggerDescriptor m_skillDescriptor = new ConceptTaggerDescriptor();
        private ConceptTaggerSkill m_skill = null;
        private ConceptTaggerBinding m_binding = null;
        private SemaphoreSlim m_lock = new SemaphoreSlim(1);
        private IReadOnlyList<ISkillExecutionDevice> m_availableDevices = null;
        private Stopwatch m_perfWatch = new Stopwatch();

        class TagEqualityComparer : IEqualityComparer<ConceptTagScore>
        {
            public bool Equals(ConceptTagScore t1, ConceptTagScore t2)
            {
                if (t2 == null && t1 == null)
                    return true;
                else if (t1 == null || t2 == null)
                    return false;
                else if (t1.Name == t2.Name)
                    return true;
                else
                    return false;
            }

            public int GetHashCode(ConceptTagScore tag)
            {
                return tag.Name.GetHashCode();
            }
        }
        private HashSet<ConceptTagScore> m_hashTags = new HashSet<ConceptTagScore>(new TagEqualityComparer());

        /// <summary>
        /// Helper class to render image and results correctly in the UI
        /// </summary>
        private class ResultItem : StackPanel
        {
            private SoftwareBitmapSource m_softwareBitmapSource = new SoftwareBitmapSource();

            /// <summary>
            /// ResultItem class constructor
            /// </summary>
            private ResultItem() { }

            /// <summary>
            /// Expose image and results in UI
            /// </summary>
            /// <param name="softwareBitmap"></param>
            /// <param name="result"></param>
            /// <param name="additionalMessage"></param>
            /// <returns></returns>
            public static async Task<ResultItem> CreateResultItemAsync(SoftwareBitmap softwareBitmap, IEnumerable<ConceptTagScore> result, string additionalMessage = null)
            {
                var resultItem = new ResultItem();
                resultItem.m_result = result;
                await resultItem.m_softwareBitmapSource.SetBitmapAsync(SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied));
                resultItem.Children.Add(new Image() { Source = resultItem.m_softwareBitmapSource, MaxWidth = 200, MaxHeight = 200, Stretch = Stretch.Uniform });
                var hashtagContainer = new WrapPanel();

                foreach (var tag in result)
                {
                    var hashTag = new Border() { BorderThickness = new Thickness(2.0), CornerRadius = new CornerRadius(5.0), Background = new SolidColorBrush(Windows.UI.Colors.DarkViolet) };
                    hashTag.Child = new TextBlock() { Text = "#" + tag.Name.Split(';')[0], Foreground = new SolidColorBrush(Windows.UI.Colors.White) };
                    hashtagContainer.Children.Add(hashTag);
                }
                resultItem.Children.Add(hashtagContainer);
                resultItem.Children.Add(new Expander()
                {
                    Header = "Scores",
                    IsExpanded = false,
                    ExpandDirection = ExpandDirection.Down,
                    Content = new ListView() { ItemsSource = result.Select(x => new HeaderedContentControl() { Header = x.Name, Content = x.Score }) }
                });

                if (additionalMessage != null)
                {
                    ToolTipService.SetToolTip(resultItem, additionalMessage);
                }

                return resultItem;
            }

            internal IEnumerable<ConceptTagScore> m_result = null;
        }

        /// <summary>
        /// MainPage constructor
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Triggered on page loaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            m_availableDevices = await m_skillDescriptor.GetSupportedExecutionDevicesAsync();

            // Refresh UI with skill description, feature descriptions, available execution devices
            foreach(var information in SkillHelper.SkillHelperMethods.GetSkillInformationStrings(m_skillDescriptor))
            {
                UISkillInformation.Children.Add(new HeaderedContentControl() { Header = information.Key, Content = information.Value });
            }

            foreach (var featureDesc in m_skillDescriptor.InputFeatureDescriptors)
            {
                foreach (var information in SkillHelper.SkillHelperMethods.GetSkillFeatureDescriptorStrings(featureDesc))
                {
                    UISkillInputDescription.Children.Add(new HeaderedContentControl() { Header = information.Key, Content = information.Value });
                }
            }

            foreach (var featureDesc in m_skillDescriptor.OutputFeatureDescriptors)
            {
                foreach (var information in SkillHelper.SkillHelperMethods.GetSkillFeatureDescriptorStrings(featureDesc))
                {
                    UISkillOutputDescription.Children.Add(new HeaderedContentControl() { Header = information.Key, Content = information.Value });
                }
            }

            if (m_availableDevices.Count == 0)
            {
                await (new MessageDialog("No execution devices available, this skill cannot run on this device")).ShowAsync();
            }
            else
            {
                // Display available execution devices
                UISkillExecutionDevices.ItemsSource = m_availableDevices.Select((device) => device.Name);
                UISkillExecutionDevices.SelectedIndex = 0;

                // Alow user to interact with the app
                UIButtonFilePick.IsEnabled = true;
                UIButtonFilePick.Focus(FocusState.Keyboard);
            }
        }

        /// <summary>
        /// Launch file picker for user to select a set of picture files
        /// </summary>
        /// <returns>VideoFrame instanciated from the selected image file</returns>
        public static async Task<IReadOnlyList<StorageFile>> GetFilesPickedAsync()
        {
            // Trigger file picker to select an image file
            FileOpenPicker fileOpenPicker = new FileOpenPicker();
            fileOpenPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            fileOpenPicker.FileTypeFilter.Add(".jpg");
            fileOpenPicker.ViewMode = PickerViewMode.Thumbnail;
            var selectedStorageFiles = await fileOpenPicker.PickMultipleFilesAsync();

            return selectedStorageFiles;
        }

        /// <summary>
        /// Loads a VideoFrame from an image file
        /// </summary>
        /// <param name="selectedStorageFile"></param>
        /// <returns></returns>
        public static async Task<VideoFrame> LoadVideoFrameFromFileAsync(StorageFile selectedStorageFile)
        {
            VideoFrame resultFrame = null;
            SoftwareBitmap softwareBitmap = null;
            using (IRandomAccessStream stream = await selectedStorageFile.OpenAsync(FileAccessMode.Read))
            {
                // Create the decoder from the stream 
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                // Get the SoftwareBitmap representation of the file in BGRA8 format
                softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                // Convert to friendly format for UI display purpose
                softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            }

            // Encapsulate the image in a VideoFrame instance
            resultFrame = VideoFrame.CreateWithSoftwareBitmap(softwareBitmap);

            return resultFrame;
        }

        /// <summary>
        /// Triggered when the file picker button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UIButtonFilePick_Click(object sender, RoutedEventArgs e)
        {
            // Pick image file
            var imageFiles = await GetFilesPickedAsync();
            if (imageFiles == null || imageFiles.Count == 0)
            {
                return;
            }

            UIResultPanel.Items.Clear();
            m_hashTags = new HashSet<ConceptTagScore>(new TagEqualityComparer());

            // Disable UI
            UIOptionPanel.IsEnabled = false;
            UIButtonFilePick.IsEnabled = false;
            UIHashTagBrowser.IsEnabled = false;
            UIClearFilterButton.IsEnabled = false;

            m_lock.Wait();
            foreach (var file in imageFiles)
            {
                var frame = await LoadVideoFrameFromFileAsync(file);
                if (frame == null)
                {
                    return;
                }

                // Execute concept tag skill
                try
                {
                    // Lazy initialize the binding instance
                    if (m_binding == null)
                    {
                        m_binding = await m_skill.CreateSkillBindingAsync() as ConceptTaggerBinding;
                    }

                    m_perfWatch.Restart();

                    // Bind input image
                    await m_binding.SetInputImageAsync(frame);

                    // Record bind time for display
                    var bindTime = (float)m_perfWatch.ElapsedTicks / Stopwatch.Frequency * 1000F;
                    m_perfWatch.Restart();

                    // Evaluate binding
                    await m_skill.EvaluateAsync(m_binding);

                    // Record evaluation time for display
                    var evalTime = (float)m_perfWatch.ElapsedTicks / Stopwatch.Frequency * 1000F;
                    var additionalMessage = $"Bind: {bindTime}ms\nEval: {evalTime}ms";

                    // Attempt to obtain top 5 concept tags identified in image that scored above 0.7
                    var result = m_binding.GetTopXTagsAboveThreshold(5, 0.7f);

                    foreach (var conceptTag in result)
                    {
                        m_hashTags.Add(conceptTag);
                    }

                    // Display image and results
                    UIResultPanel.Items.Add(await ResultItem.CreateResultItemAsync(frame.SoftwareBitmap, result, additionalMessage));
                }
                catch (Exception ex)
                {
                    NotifyUser(ex.Message, NotifyType.ErrorMessage);
                }
                m_perfWatch.Stop();
            }
            m_lock.Release();

            UIHashTagBrowser.ItemsSource = null;
            UIHashTagBrowser.ItemsSource = m_hashTags;

            // Enable UI
            UIOptionPanel.IsEnabled = true;
            UIButtonFilePick.IsEnabled = true;
            UIHashTagBrowser.IsEnabled = true;
            UIClearFilterButton.IsEnabled = true;
        }

        /// <summary>
        /// Triggered when the selected execution device changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UISkillExecutionDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = UISkillExecutionDevices.SelectedIndex;
            if (selectedIndex >= 0)
            {
                m_lock.Wait();
                try
                {
                    m_binding = null;
                    m_skill = await m_skillDescriptor.CreateSkillAsync(m_availableDevices[selectedIndex]) as ConceptTaggerSkill;
                }
                catch(Exception ex)
                {
                    NotifyUser(ex.Message, NotifyType.ErrorMessage);
                }
                m_lock.Release();
            }
        }

        /// <summary>
        /// Display a message to the user.
        /// This method may be called from any thread.
        /// </summary>
        /// <param name="strMessage"></param>
        /// <param name="type"></param>
        public void NotifyUser(string strMessage, NotifyType type)
        {
            // If called from the UI thread, then update immediately.
            // Otherwise, schedule a task on the UI thread to perform the update.
            if (Dispatcher.HasThreadAccess)
            {
                UpdateStatus(strMessage, type);
            }
            else
            {
                var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateStatus(strMessage, type));
                task.AsTask().Wait();
            }
        }

        /// <summary>
        /// Update the status message displayed on the UI
        /// </summary>
        /// <param name="strMessage"></param>
        /// <param name="type"></param>
        private void UpdateStatus(string strMessage, NotifyType type)
        {
            switch (type)
            {
                case NotifyType.StatusMessage:
                    UIStatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                    break;
                case NotifyType.ErrorMessage:
                    UIStatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                    break;
            }

            UIStatusBlock.Text = strMessage;
        }

        public enum NotifyType
        {
            StatusMessage,
            ErrorMessage
        };

        /// <summary>
        /// Triggered when the clear fitler button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UIClearFilterButton_Click(object sender, RoutedEventArgs e)
        {
            UIHashTagBrowser.SelectedIndex = -1;
        }

        /// <summary>
        /// Triggered when a tag filter is selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UIHashTagBrowser_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UIHashTagBrowser.SelectedIndex == -1)
            {
                foreach (var item in UIResultPanel.Items)
                {
                    ResultItem result = item as ResultItem;
                    result.Visibility = Visibility.Visible;
                }

            }
            else
            {
                var tag = UIHashTagBrowser.SelectedItem as ConceptTagScore;
                foreach (var item in UIResultPanel.Items)
                {
                    ResultItem result = item as ResultItem;
                    if (result.m_result.Any(x => x.Name == tag.Name))
                    {
                        result.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        result.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }
    }
}
