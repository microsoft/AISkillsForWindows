using GalleryApp.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

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
        private void LoadAllSkills()
        {
            GetSkillCategories();
            if (m_allCategories != null)
            {
                foreach (var category in m_allCategories)
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

        /// <summary>
        /// Load all skills from the JSON file
        /// </summary>
        private void GetSkillCategories()
        {
            if (m_allCategories == null)
            {
                //Task 22514335: Investigate on how to package JSON file for non - visual studio execution
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
        }

        /// <summary>
        /// Notify user with message
        /// </summary>
        /// <param name="message"></param>
        private void NotifyUser(string message)
        {
            ExceptionTextBlock.Visibility = Visibility.Visible;
            ExceptionTextBlock.Text = message;
        }
    }
}
