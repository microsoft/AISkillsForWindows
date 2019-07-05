using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace GalleryApp
{
    public class UIHelper
    {
        /// <summary>
        /// Backward navigation to MainPage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void Back_Click(object sender, RoutedEventArgs e)
        {
            var frame = Window.Current.Content as Frame;
            frame.Navigate(typeof(MainPage));
        }

    }
}
