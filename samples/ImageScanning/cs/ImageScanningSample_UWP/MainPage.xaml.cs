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
using Windows.Media;
using Microsoft.AI.Skills.Vision.ImageScanning;
using System.Threading;
using System.Collections.Generic;
using Microsoft.AI.Skills.SkillInterface;
using System.Diagnostics;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;
using ImageScanningSample.Helper;
using SkillHelper;

namespace ImageScanningSample
{
    /// <summary>
    /// Main page to this application
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Skill wrappers
        private List<SkillWrapper> m_skillWrappers = new List<SkillWrapper>()
        {
            new SkillWrapper(new CurvedEdgesDetectorDescriptor()),
            new SkillWrapper(new ImageCleanerDescriptor()),
            new SkillWrapper(new ImageRectifierDescriptor()),
            new SkillWrapper(new LiveQuadDetectorDescriptor()),
            new SkillWrapper(new QuadDetectorDescriptor()),
            new SkillWrapper(new QuadEdgesDetectorDescriptor())
        };
        private SkillWrapper m_currentSkillWrapper = null;
        private SkillControl m_currentSkillControl = null;

        // Threading and UI specific
        private SemaphoreSlim m_bindingLock = new SemaphoreSlim(1);
        private SemaphoreSlim m_evaluationLock = new SemaphoreSlim(1);
        private IReadOnlyList<ISkillExecutionDevice> m_availableDevices = null;
        private Stopwatch m_perfWatch = new Stopwatch();

        /// <summary>
        /// MainPage constructor
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Evaluate the specified ISkillBinding and update the specified ResultItem with the outcome
        /// </summary>
        /// <param name="binding"></param>
        /// <param name="control"></param>
        /// <returns></returns>
        private async Task EvaluateBindingAsync(ISkillBinding binding, SkillControl control)
        {
            // Take a lock for using a skill if one is available, or wait if not
            m_evaluationLock.Wait();

            try
            {
                var baseTime = (float)m_perfWatch.ElapsedTicks / Stopwatch.Frequency * 1000f;

                // Evaluate binding
                await m_currentSkillWrapper.Skill.EvaluateAsync(binding);

                // Record evaluation time for display
                control.EvalTime = (float)m_perfWatch.ElapsedTicks / Stopwatch.Frequency * 1000f - baseTime;

                m_evaluationLock.Release();
            }
            catch (Exception ex)
            {
                NotifyUser(ex.Message, NotifyType.ErrorMessage);

                m_bindingLock.Release();

                m_evaluationLock.Release();
                return;
            }

            m_bindingLock.Release();
        }

        /// <summary>
        /// Launch file picker for user to select a set of picture files
        /// </summary>
        /// <returns>VideoFrame instanciated from the selected image file</returns>
        public static async Task<StorageFile> GetFilePickedAsync()
        {
            // Trigger file picker to select an image file
            FileOpenPicker fileOpenPicker = new FileOpenPicker();
            fileOpenPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            fileOpenPicker.FileTypeFilter.Add(".jpg");
            fileOpenPicker.FileTypeFilter.Add(".png");
            fileOpenPicker.ViewMode = PickerViewMode.Thumbnail;

            return await fileOpenPicker.PickSingleFileAsync();
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

        // -- Event handlers -- //
        #region EventHandlers

        /// <summary>
        /// Triggered when the file picker button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UIButtonFilePick_Click(object sender, RoutedEventArgs e)
        {
            // Pick image file
            var imageFile = await GetFilePickedAsync();
            if (imageFile == null)
            {
                return;
            }

            UIResultPanel.Items.Clear();

            // Disable UI
            UISkillTabs.IsEnabled = false;
            NotifyUser("", NotifyType.ClearMessage);

            // Start our stopwatch to measure the time it takes to process all of this
            m_perfWatch.Restart();

            // Take a lock for using the binding
            await m_bindingLock.WaitAsync();

            // Display a staging content in our UI
            m_currentSkillControl = SkillControl.CreateControl(m_currentSkillWrapper.Binding);
            UIResultPanel.Items.Add(m_currentSkillControl);
            m_currentSkillControl.RunButtonClicked += SkillControl_RunButtonClicked;

            // Start a task that will Load the frame, display it in the UI, bind it and schedule 
            // execution of the skill against that binding
            // (fire and forget)
            var bindingTask = Task.Run(async () =>
            {
                // Execute concept tag skill
                try
                {
                    // Load the VideoFrame from the image file
                    var frame = await LoadVideoFrameFromFileAsync(imageFile);
                    if (frame == null)
                    {
                        throw new Exception($"Error reading image file: {imageFile.Path}");
                    }

                    var baseTime = (float)m_perfWatch.ElapsedTicks / Stopwatch.Frequency * 1000f;

                    // Bind input image
                    await m_currentSkillWrapper.Binding["InputImage"].SetFeatureValueAsync(frame);

                    // Record bind time for display
                    m_currentSkillControl.BindTime = (float)m_perfWatch.ElapsedTicks / Stopwatch.Frequency * 1000f - baseTime;

                    // Display image
                    await UIResultPanel.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        async () =>
                        {
                            await m_currentSkillControl.UpdateSkillControlInputImageAsync(frame.SoftwareBitmap);
                        });
                }
                catch (Exception ex)
                {
                    NotifyUser(ex.Message, NotifyType.ErrorMessage);
                }
                m_bindingLock.Release();

                // Enable UI
                await Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () =>
                    {
                        UISkillTabs.IsEnabled = true;
                        UIButtonFilePick.IsEnabled = true;
                    });

            });
        }

        /// <summary>
        /// Triggered when the run button for the skill currently toggled is clicked
        /// </summary>
        /// <param name="binding"></param>
        private async void SkillControl_RunButtonClicked(ISkillBinding binding)
        {
            // Disable UI
            UISkillTabs.IsEnabled = false;
            UIButtonFilePick.IsEnabled = false;
            NotifyUser("", NotifyType.ClearMessage);

            // Evaluate binding (fire and forget)
            await EvaluateBindingAsync(m_currentSkillWrapper.Binding, m_currentSkillControl);

            // Display image and results
            await UIResultPanel.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                async () =>
                {
                    await m_currentSkillControl.UpdateSkillControlValuesAsync(m_currentSkillWrapper.Binding);
                });

            // Enable UI
            UISkillTabs.IsEnabled = true;
            UIButtonFilePick.IsEnabled = true;
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
                m_bindingLock.Wait();
                m_evaluationLock.Wait();

                var device = m_skillWrappers.First().ExecutionDevices.First();

                try
                {
                    // Initialize skill and binding
                    await m_currentSkillWrapper.InitializeSkillAsync(device);

                    // Alow user to interact with the app
                    UISkillTabs.IsEnabled = true;
                    UIButtonFilePick.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    NotifyUser(ex.Message, NotifyType.ErrorMessage);
                }

                m_evaluationLock.Release();
                m_bindingLock.Release();
            }
        }

        /// <summary>
        /// Triggered when a skill is selected from the skill tab at the top of the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UISkillTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UISkillTabs.SelectedIndex < 0)
            {
                return;
            }

            // Clear any previous results
            UIResultPanel.Items.Clear();

            // Prevent premature user interaction
            UIButtonFilePick.IsEnabled = false;
            UISkillTabs.IsEnabled = false;
            m_currentSkillWrapper = m_skillWrappers[UISkillTabs.SelectedIndex];

            // Refresh UI with available execution devices on the system supported by the skill
            m_availableDevices = await m_currentSkillWrapper.Descriptor.GetSupportedExecutionDevicesAsync();
            if (m_availableDevices.Count == 0)
            {
                await (new MessageDialog("No execution devices available, this skill cannot run on this device")).ShowAsync();
            }
            else
            {
                // Display available execution devices and select the CPU by default
                UISkillExecutionDevices.ItemsSource = m_availableDevices.Select((device) => new SkillExecutionDeviceWrappper(device));

                int selectionIndex = -1;
                for (int i = 0; i < m_availableDevices.Count; i++)
                {
                    if (m_availableDevices[i].ExecutionDeviceKind == SkillExecutionDeviceKind.Cpu)
                    {
                        selectionIndex = i;
                        break;
                    }
                }

                UISkillExecutionDevices.SelectedIndex = selectionIndex;
            }
        }

        #endregion EventHandlers
    }
}
