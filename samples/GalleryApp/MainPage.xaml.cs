using System;
using System.Collections.Generic;
using System.IO;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using GalleryApp.Models;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GalleryApp
{
    /// <summary>
    /// Application's main page
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public ObservableCollection<SkillView> SkillPages { get; } = new ObservableCollection<SkillView>();

        private static string m_filePath = "\\Pages\\SkillViewGlossary.json";

        private static List<SkillCategory> m_allCategories;

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
        private async void LoadAllSkills()
        {
            var SkillsCategory = GetSkillCategories();
            if (SkillsCategory != null)
            {
                foreach (var category in SkillsCategory)
                {
                    foreach (var skill in category.SkillViews)
                    {
                        SkillPages.Add(skill);
                    }
                }
            }
        }

        /// <summary>
        /// Navigate to a specific skill page
        /// </summary>
        private void SampleGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            NavigateToSkillPage(e.ClickedItem as SkillView);
        }

        /// <summary>
        /// Navigate to the specific skill page
        /// </summary>
        /// <param name="skill"> Skill passed from the thumbnail item </param>
        public void NavigateToSkillPage(SkillView skill)
        {
            this.Frame.Navigate(skill.PageType);
        }

        public List<SkillCategory> GetSkillCategories()
        {
            if (m_allCategories == null)
            {
                // NOTE: Investigate on how to package JSON file for non-visual studio execution
                // Task #: 
                try
                {
                    var directoryPath = Directory.GetCurrentDirectory();

                    using (StreamReader file = File.OpenText(directoryPath + m_filePath))
                    {
                        var jsonString = file.ReadToEnd();
                        m_allCategories = JsonConvert.DeserializeObject<List<SkillCategory>>(jsonString);
                    }
                }
                catch (Exception exception)
                {
                    NotifyUser(exception.Message);
                }
            }
            return m_allCategories;
        }

        private void NotifyUser(string message)
        {
            ExceptionTextBlock.Visibility = Visibility.Visible;
            ExceptionTextBlock.Text = message;
        }
    }
}
