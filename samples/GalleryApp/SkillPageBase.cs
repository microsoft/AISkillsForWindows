using Microsoft.AI.Skills.SkillInterfacePreview;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace GalleryApp
{
    /// <summary>
    /// This class contains methods that are all skill pages have
    /// </summary>
    public abstract class SkillPageBase : Page, INotifyPropertyChanged
    {
        // Data binding related variables
        private string m_UIMessageTextBlockText = "Select an image source to start";
        private bool m_enableButtons = true;

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
                //UIMessageTextBlock.Text = message;
                UIMessageTextBlockText = message;
                //GetUIMessageTextBlock().Text = message;
            }
            else
            {
                //Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => UIMessageTextBlock.Text = message).AsTask().Wait();
                Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => UIMessageTextBlockText = message).AsTask().Wait();
                //Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => GetUIMessageTextBlock().Text = message).AsTask().Wait();
            }
        }

        /// <summary>
        /// Triggered when UIButtonFilePick is clicked, grabs a frame from an image file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected async void UIButtonFilePicker_Click(object sender, RoutedEventArgs e)
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
        /// Update media source buttons (top row)
        /// </summary>
        /// <param name="enableButtons"></param>
        /// <returns></returns>
        protected async Task UpdateMediaSourceButtonsAsync(bool enableButtons)
        {
            if (Dispatcher.HasThreadAccess)
            {
                m_enableButtons = enableButtons;
                m_enableButtons = enableButtons;
            }
            else
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => await UpdateMediaSourceButtonsAsync(enableButtons));
            }
        }
    }
}
