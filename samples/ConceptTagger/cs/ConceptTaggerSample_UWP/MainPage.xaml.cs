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
using Microsoft.AI.Skills.Vision.ConceptTagger;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System.Threading;
using System.Collections.Generic;
using Microsoft.AI.Skills.SkillInterface;
using System.Diagnostics;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;
using System.Collections.Immutable;
using System.Collections.Concurrent;
using ConceptTaggerSample.Helper;

namespace ConceptTaggerSample
{
    /// <summary>
    /// Main page to this application
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Skill specific
        private ConceptTaggerDescriptor m_skillDescriptor = new ConceptTaggerDescriptor();
        private ConcurrentQueue<ConceptTaggerSkill> m_skillQueue = new ConcurrentQueue<ConceptTaggerSkill>();
        private ConcurrentQueue<ConceptTaggerBinding> m_bindingQueue = new ConcurrentQueue<ConceptTaggerBinding>();

        // Threading and UI specific
        public int m_concurrentSkillCount = 1;
        public int m_concurrentBindingCount = 1;
        private static int m_imageProcessedCount = 0;
        private static int m_imageToProcessTotal = 0;
        private static float m_e2eRunTime = 0.0f;
        private SemaphoreSlim m_bindingLock = new SemaphoreSlim(1);
        private SemaphoreSlim m_evaluationLock = new SemaphoreSlim(1);
        private ImmutableHashSet<ConceptTagScore>.Builder m_hashTags = ImmutableHashSet.CreateBuilder(new TagEqualityComparer());
        private IReadOnlyList<ISkillExecutionDevice> m_availableDevices = null;
        private Stopwatch m_perfWatch = new Stopwatch();
        private int m_topX = 5;
        private double m_threshold = 0.7;

        /// <summary>
        /// Internal functor to evaluate equality between 2 ConceptTagScore instances
        /// This is required when instantiating a HashSet
        /// </summary>
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
            // Refresh UI with skill information (name, description, etc.) feature descriptions, available execution devices
            //foreach (var information in SkillHelper.SkillHelperMethods.GetSkillInformationStrings(m_skillDescriptor.Information))
            //{
            //    UISkillInformation.Children.Add(new HeaderedContentControl() { Header = information.Key, Content = information.Value });
            //}

            // Refresh UI with skill input feature descriptions
            foreach (var featureDesc in m_skillDescriptor.InputFeatureDescriptors)
            {
                foreach (var information in SkillHelper.SkillHelperMethods.GetSkillFeatureDescriptorStrings(featureDesc))
                {
                    UISkillInputDescription.Children.Add(new HeaderedContentControl() { Header = information.Key, Content = information.Value });
                }
            }

            // Refresh UI with skill output feature descriptions
            foreach (var featureDesc in m_skillDescriptor.OutputFeatureDescriptors)
            {
                foreach (var information in SkillHelper.SkillHelperMethods.GetSkillFeatureDescriptorStrings(featureDesc))
                {
                    UISkillOutputDescription.Children.Add(new HeaderedContentControl() { Header = information.Key, Content = information.Value });
                }
            }

            // Refresh UI with available execution devices on the system supported by the skill
            m_availableDevices = await m_skillDescriptor.GetSupportedExecutionDevicesAsync();
            if (m_availableDevices.Count == 0)
            {
                await (new MessageDialog("No execution devices available, this skill cannot run on this device")).ShowAsync();
            }
            else
            {
                // Display available execution devices and select the CPU
                UISkillExecutionDevices.ItemsSource = m_availableDevices.Select((device) => device.Name);
                int selectionIndex = 0;
                for (int i = 0; i < m_availableDevices.Count; i++)
                {
                    if (m_availableDevices[i].ExecutionDeviceKind == SkillExecutionDeviceKind.Cpu)
                    {
                        selectionIndex = i;
                        break;
                    }
                }

                UISkillExecutionDevices.SelectedIndex = selectionIndex;

                // Alow user to interact with the app
                UIButtonFilePick.IsEnabled = true;
                UIButtonFilePick.Focus(FocusState.Keyboard);
            }
        }

        /// <summary>
        /// Evaluate the specified ConceptTaggerBinding and update the specified ResultItem with the outcome
        /// </summary>
        /// <param name="binding"></param>
        /// <param name="resultItem"></param>
        /// <returns></returns>
        private async Task EvaluateBinding(ConceptTaggerBinding binding, ResultItemUserControl resultItem)
        {
            // Take a lock for using a skill if one is available, or wait if not
            m_evaluationLock.Wait();
            ConceptTaggerSkill skill = null;
            try
            {
                if (!m_skillQueue.TryDequeue(out skill))
                {
                    throw new Exception("Could not access skill");
                }

                var baseTime = (float)m_perfWatch.ElapsedTicks / Stopwatch.Frequency * 1000f;

                // Evaluate binding
                await skill.EvaluateAsync(binding);

                // Record evaluation time for display
                resultItem.EvalTime = (float)m_perfWatch.ElapsedTicks / Stopwatch.Frequency * 1000f - baseTime;

                m_skillQueue.Enqueue(skill);
                m_evaluationLock.Release();
            }
            catch (Exception ex)
            {
                NotifyUser(ex.Message, NotifyType.ErrorMessage);
                if (binding != null)
                {
                    m_bindingQueue.Enqueue(binding);
                }
                m_bindingLock.Release();
                if (skill != null)
                {
                    m_skillQueue.Enqueue(skill);
                }
                m_evaluationLock.Release();
                return;
            }
            m_imageProcessedCount++;

            // Attempt to obtain top 5 concept tags identified in image that scored a confidence of above 0.7
            var result = binding.GetTopXTagsAboveThreshold(m_topX, (float)m_threshold);

            m_bindingQueue.Enqueue(binding);
            m_bindingLock.Release();

            if (result != null)
            {
                foreach (var conceptTag in result)
                {
                    m_hashTags.Add(conceptTag);
                }
            }

            // Display image and results
            await UIResultPanel.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    resultItem.UpdateResultItemScore(result);
                    UITotalTime.Content = $"{((float)m_perfWatch.ElapsedTicks / Stopwatch.Frequency - m_e2eRunTime)}s";

                    UIProgressTick.Text = m_imageProcessedCount.ToString();

                    // Refresh the set of hashtag filters in the UI if we reached the end
                    if (m_imageProcessedCount == m_imageToProcessTotal)
                    {
                        UIHashTagBrowser.ItemsSource = null;
                        UIHashTagBrowser.ItemsSource = m_hashTags;
                    }
                });
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
            fileOpenPicker.FileTypeFilter.Add(".png");
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
            await UIParameterDialog.ShowAsync();

            UIResultPanel.Items.Clear();
            m_imageToProcessTotal = imageFiles.Count;
            UIProgressMaxValue.Text = m_imageToProcessTotal.ToString();
            m_hashTags = ImmutableHashSet.CreateBuilder(new TagEqualityComparer());

            // Disable UI
            UIOptionPanel.IsEnabled = false;
            UIButtonFilePick.IsEnabled = false;
            UIHashTagBrowser.IsEnabled = false;
            UIClearFilterButton.IsEnabled = false;
            NotifyUser("", NotifyType.ClearMessage);

            // Start our stopwatch to measure the time it takes to process all of this
            m_perfWatch.Restart();
            m_e2eRunTime = (float)m_perfWatch.ElapsedTicks / Stopwatch.Frequency * 1000f;
            m_imageProcessedCount = 0;
            UIFilterPanel.Visibility = Visibility.Collapsed;
            UIProgressPanel.Visibility = Visibility.Visible;
            UIExecutionProgressRing.Visibility = Visibility.Visible;
            UIExecutionProgressRing.IsActive = true;

            // Process each image selected
            foreach (var file in imageFiles)
            {
                // Take a lock for using a binding if one is available, or wait if not
                await m_bindingLock.WaitAsync();

                // Display a staging content in our UI
                ResultItemUserControl resultItem = new ResultItemUserControl();
                UIResultPanel.Items.Add(resultItem);

                // Start a task that will Load the frame, display it in the UI, binf it and schedule 
                // execution of the skill against that binding
                // (fire and forget)
                var bindingTask = Task.Run(async () =>
                {
                    ConceptTaggerBinding binding = null;

                    // Execute concept tag skill
                    try
                    {
                        // Load the VideoFrame from the image file
                        var frame = await LoadVideoFrameFromFileAsync(file);
                        if (frame == null)
                        {
                            throw new Exception($"Error reading image file: {file.Path}");
                        }


                        if (!m_bindingQueue.TryDequeue(out binding))
                        {
                            throw new Exception("Could not access binding");
                        }

                        var baseTime = (float)m_perfWatch.ElapsedTicks / Stopwatch.Frequency * 1000f;

                        // Bind input image
                        await binding.SetInputImageAsync(frame);

                        // Record bind time for display
                        resultItem.BindTime = (float)m_perfWatch.ElapsedTicks / Stopwatch.Frequency * 1000f - baseTime;

                        // Display image and results
                        await Dispatcher.RunAsync(
                            CoreDispatcherPriority.Normal,
                            async () =>
                            {
                                await resultItem.UpdateResultItemImageAsync(frame.SoftwareBitmap);
                            });

                        // Evaluate binding (fire and forget)
                        var evalTask = Task.Run(() => EvaluateBinding(binding, resultItem));
                    }
                    catch (Exception ex)
                    {
                        NotifyUser(ex.Message, NotifyType.ErrorMessage);
                        if (binding != null)
                        {
                            m_bindingQueue.Enqueue(binding);
                        }
                        m_bindingLock.Release();
                        m_imageToProcessTotal--;
                        await UIProgressMaxValue.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                            () => UIProgressMaxValue.Text = m_imageToProcessTotal.ToString());
                    }
                });
            }

            // Enable UI
            UIOptionPanel.IsEnabled = true;
            UIButtonFilePick.IsEnabled = true;
            UIHashTagBrowser.IsEnabled = true;
            UIClearFilterButton.IsEnabled = true;
            UIFilterPanel.Visibility = Visibility.Visible;
            UIExecutionProgressRing.Visibility = Visibility.Collapsed;
            UIExecutionProgressRing.IsActive = false;
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
                for (int i = 0; i < m_concurrentBindingCount; i++)
                {
                    m_bindingLock.Wait();
                }
                for (int i = 0; i < m_concurrentSkillCount; i++)
                {
                    m_evaluationLock.Wait();
                }

                ISkillExecutionDevice device = m_availableDevices[selectedIndex];

                // If we selected the CPU, let's distribute the workload among a number of binding and skill instances running in parallel equivalent 
                // to half the amount of cores available
                // If we select the GPU, let's use only 1 instance of each and run as fast as possible instead
                m_concurrentSkillCount = device is SkillExecutionDeviceCPU ? Math.Max(1, (device as SkillExecutionDeviceCPU).CoreCount / 2) : 1;
                m_concurrentBindingCount = m_concurrentSkillCount;
                m_bindingLock = new SemaphoreSlim(m_concurrentBindingCount);
                m_evaluationLock = new SemaphoreSlim(m_concurrentSkillCount);

                try
                {
                    m_bindingQueue.Clear();
                    m_skillQueue.Clear();
                    ConceptTaggerSkill skill = null;
                    for (int i = 0; i < m_concurrentSkillCount; i++)
                    {
                        skill = await m_skillDescriptor.CreateSkillAsync(device) as ConceptTaggerSkill;
                        m_skillQueue.Enqueue(skill);
                    }
                    for (int i = 0; i < m_concurrentBindingCount; i++)
                    {
                        m_bindingQueue.Enqueue(await skill.CreateSkillBindingAsync() as ConceptTaggerBinding);
                    }
                }
                catch (Exception ex)
                {
                    NotifyUser(ex.Message, NotifyType.ErrorMessage);
                }
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
                case NotifyType.ClearMessage:
                    UIStatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Transparent);
                    break;
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
            ClearMessage,
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
                    ResultItemUserControl result = item as ResultItemUserControl;
                    result.Visibility = Visibility.Visible;
                }

            }
            else
            {
                var tag = UIHashTagBrowser.SelectedItem as ConceptTagScore;
                foreach (var item in UIResultPanel.Items)
                {
                    ResultItemUserControl result = item as ResultItemUserControl;
                    if (result.Results != null && result.Results.Any(x => x.Name == tag.Name))
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
