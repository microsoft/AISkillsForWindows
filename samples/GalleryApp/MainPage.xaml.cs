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
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public ObservableCollection<Skill> SkillPages { get; } = new ObservableCollection<Skill>();

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (SkillPages.Count == 0)
            {
                GetItem();
            }
            base.OnNavigatedTo(e);
        }

        private void GetItem()
        {
            SkillPages.Add(new Skill("Skeletal Detector Page", "Description", typeof(SkeletalDetectorPage)));
            SkillPages.Add(new Skill("Object Detector Page", "Description", typeof(ObjectDetectorPage)));
        }

        private void SampleGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            NavigateToSkillPage(e.ClickedItem as Skill);
        }

        /// <summary>
        /// Navigation to Skill Page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void NavigateToSkillPage(Skill sample)
        {
            Type type = sample.PageType;
            this.Frame.Navigate(type);
        }
    }
}
