using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;


namespace GalleryApp
{
    /// <summary>
    /// A wrapper for the Skill View Page
    /// </summary>
    public sealed partial class SkillViewController : Page
    {
        public SkillViewController()
        {
            this.InitializeComponent();
        }

        public SkillView CurrentSkillView { get; private set; }
        private bool m_enableButtons = true;

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

        /// <summary>
        /// Raise the PropertyChanged event, passing the name of the property whose value has changed
        /// </summary>
        /// <param name="propertyName"></param>
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }



        /// <summary>
        /// Create the skill page
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is SkillView skillView)
            {
                CurrentSkillView = skillView;
            }

            if (CurrentSkillView != null)
            {

                if (CurrentSkillView.Name != null)
                {
                    SkillName.Text = CurrentSkillView.Name;
                }
                if (CurrentSkillView.GitHubUrl != null)
                {
                    UIGitHubButton.NavigateUri = new Uri(CurrentSkillView.GitHubUrl);
                }


            }
            else
            {
                // Skill view does not exist throw an exception
            }
        }

        /// <summary>
        /// Backward navigation to MainPage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Back_Click(object sender, RoutedEventArgs e)
        {
            var currentFrame = (Frame)Window.Current.Content;
            currentFrame.Navigate(typeof(MainPage));
        }

        /// <summary>
        /// Called when page is loaded
        /// Initialize app assets such as skills
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Disable buttons while we initialize
            await UpdateMediaSourceButtonsAsync(false);

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

        protected async void UICameraButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO
        }

        protected async void UIFilePickerButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO
        }
    }
}
