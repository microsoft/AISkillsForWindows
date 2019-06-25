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

namespace GalleryApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
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
            this.Frame.Navigate(typeof(SkeletalDetectorPage));
        }

        private void NavigateToObjectDetectorPage(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ObjectDetectorPage));
        }
    }
}
