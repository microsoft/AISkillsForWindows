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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace GalleryApp
{
    public sealed partial class Thumbnail : UserControl
    {
        public string SkillName { get; }
        public string SkillDescription { get; }

        public Thumbnail()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Navigation to Skeletal Detector Page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NavigateToSkillPage(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(SkeletalDetectorPage));
        }
    }
}
