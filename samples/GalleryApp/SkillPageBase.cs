using Microsoft.AI.Skills.SkillInterfacePreview;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace GalleryApp
{
    // Indexes of all skill execution steps
    public enum Indicator { initialization = 0, binding, evaluating, done };

    // Possible states of an execution state
    public enum ExecutionState { start, end, reset, error }

    /// <summary>
    /// This class contains methods that are all skill pages have
    /// </summary>
    public abstract class SkillPageBase : Page, INotifyPropertyChanged
    {
        // Colors for indicating execution status
        public static readonly SolidColorBrush Green = new SolidColorBrush(Colors.Green);
        public static readonly SolidColorBrush Yellow = new SolidColorBrush(Colors.Yellow);
        public static readonly SolidColorBrush White = new SolidColorBrush(Colors.White);
        public static readonly SolidColorBrush Red = new SolidColorBrush(Colors.Red);

        // Data binding UI components
        private string m_UIMessageTextBlockText = "Select an image source to start";
        private bool m_enableButtons = true;

        // UIIndicators related variables
        public ObservableCollection<SolidColorBrush> IndicatorsList { get; set; } = new ObservableCollection<SolidColorBrush>() { White, White, White, White };
        public ObservableCollection<SolidColorBrush> ColorsList { get; set; } = new ObservableCollection<SolidColorBrush>() { Yellow, Green, White, Red };

        protected string UIMessageTextBlockText
        {
            get { return m_UIMessageTextBlockText; }
            set
            {
                this.m_UIMessageTextBlockText = value;
                this.OnPropertyChanged();
            }
        }

        protected bool EnableButtons
        {
            get { return m_enableButtons; }
            set
            {
                this.m_enableButtons = value;
                this.OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        // Skill-related variable
        protected ISkillFeatureImageDescriptor m_inputImageFeatureDescriptor = null;

        // Abstract methods
        /// <summary>
        /// Configure an IFrameSource from a StorageFile or MediaCapture instance to produce optionally a specified format of frame
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        protected abstract Task ConfigureFrameSourceAsync(object source, ISkillFeatureImageDescriptor inputImageDescriptor = null);

        /// <summary>
        /// Raise the PropertyChanged event, passing the name of the property whose value has changed
        /// </summary>
        /// <param name="propertyName"></param>
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Backward navigation to MainPage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void Back_Click(object sender, RoutedEventArgs e)
        {
            var currentFrame = (Frame)Window.Current.Content;
            currentFrame.Navigate(typeof(MainPage));
        }

        /// <summary>
        /// Print a message to the UI
        /// </summary>
        /// <param name="message"></param>
        protected void NotifyUser(String message)
        {
            if (Dispatcher.HasThreadAccess)
            {
                UIMessageTextBlockText = message;
            }
            else
            {
                Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => UIMessageTextBlockText = message).AsTask().Wait();
            }
        }

        /// <summary>
        /// Triggered when UIButtonFilePick is clicked, grabs a frame from an image file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected async void UIFilePickerButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            // Add common video file extensions
            picker.FileTypeFilter.Add(".mp4");
            picker.FileTypeFilter.Add(".avi");
            // Add common image file extensions
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".bmp");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                await ConfigureFrameSourceAsync(file);
                NotifyUser("Loading file: " + file.Path);
            }

            // Re-enable the top menu once done handling the click
            await UpdateMediaSourceButtonsAsync(true);
        }

        /// <summary>
        /// Triggered when UIButtonCamera is clicked, initializes frame grabbing from the camera stream
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected async void UICameraButton_Click(object sender, RoutedEventArgs e)
        {
            // Disable the top menu while handling the click
            await UpdateMediaSourceButtonsAsync(false);

            // Create a device picker
            var devicePicker = new DevicePicker();
            devicePicker.Filter.SupportedDeviceClasses.Add(DeviceClass.VideoCapture);

            // Get UIButtonCamera
            var UIButtonCamera = sender as AppBarButton;

            // Calculate the position to show the picker (right below the buttons)
            GeneralTransform ge = UIButtonCamera.TransformToVisual(null);
            Point point = ge.TransformPoint(new Point());
            Rect rect = new Rect(point, new Point(point.X + UIButtonCamera.ActualWidth, point.Y + UIButtonCamera.ActualHeight));

            // Show the picker and obtain user selection
            DeviceInformation di = await devicePicker.PickSingleDeviceAsync(rect);
            if (di != null)
            {
                try
                {
                    NotifyUser("Attaching to camera " + di.Name);
                    await ConfigureFrameSourceAsync(di, m_inputImageFeatureDescriptor);
                }
                catch (Exception ex)
                {
                    NotifyUser("Error occurred while initializating MediaCapture:\n" + ex.Message);
                }
            }

            // Re-enable the top menu once done handling the click
            await UpdateMediaSourceButtonsAsync(true);
        }

        /// <summary>
        /// Update media source buttons (top row)
        /// </summary>
        /// <param name="enableButtons"></param>
        /// <returns></returns>
        protected async Task UpdateMediaSourceButtonsAsync(bool enableButtons)
        {
            if (Dispatcher.HasThreadAccess)
            {
                m_enableButtons = enableButtons;
            }
            else
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => await UpdateMediaSourceButtonsAsync(enableButtons));
            }
        }

        /// <summary>
        /// Update skill status indicators' colors (inside the Status box)
        /// </summary>
        /// <param name="newBindSkillIndicatorColor"></param>
        /// <param name="newEvaluateSkillIndicatorColor"></param>
        /// <returns></returns>
        protected async Task UpdateIndicator(Indicator indicator, ExecutionState executionState)
        {
            if (Dispatcher.HasThreadAccess)
            {
                IndicatorsList[(int)indicator] = ColorsList[(int)executionState];
            }
            else
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => await UpdateIndicator(indicator, executionState));
            }
        }
    }
}
