using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp;
using System.Collections.ObjectModel;

namespace GalleryApp
{
    /// <summary>
    /// Application's main page
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public ObservableCollection<Skill> SkillPages { get; } = new ObservableCollection<Skill>();

        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the Page is loaded and becomes the current source of a parent Frame.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (SkillPages.Count == 0)
            {
                LoadAllSkills();
            }
            base.OnNavigatedTo(e);
        }

        /// <summary>
        /// Load all the skills available
        /// </summary>
        private void LoadAllSkills()
        {
            SkillPages.Add(new Skill("Skeletal Detector Page", "A set of sample apps that can detect and identify the poses of the bodies of individuals in a video or image.", typeof(SkeletalDetectorPage)));
            SkillPages.Add(new Skill("Object Detector Page", "A set of sample apps that detect and classify 80 objects using the Object Detector NuGet package.", typeof(ObjectDetectorPage)));
        }

        /// <summary>
        /// Navigate to a specific skill page
        /// </summary>
        private void SampleGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            NavigateToSkillPage(e.ClickedItem as Skill);
        }

        /// <summary>
        /// Navigate to the specific skill page
        /// </summary>
        /// <param name="skill"> Skill passed from the thumbnail item </param>
        public void NavigateToSkillPage(Skill skill)
        {
            this.Frame.Navigate(skill.PageType);
        }
    }
}
