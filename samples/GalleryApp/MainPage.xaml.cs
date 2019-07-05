using System;
using System.Collections.Generic;
using System.IO;
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
        public ObservableCollection<Skill> SkillPages { get; } = new ObservableCollection<Skill>();

        private static List<SkillCategory> m_allCategories;
        private static SemaphoreSlim m_semaphore = new SemaphoreSlim(1);

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
            // Task: bind category to UI tab
            var SkillsCategory = await GetCategoriesAsync();
            foreach (var category in SkillsCategory)
            {
                foreach (var skill in category.Skills)
                {
                    SkillPages.Add(skill);
                }
            }
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

        public static async Task<List<SkillCategory>> GetCategoriesAsync()
        {
            await m_semaphore.WaitAsync();
            if (m_allCategories == null)
            {
                // NOTE: Investigate on how to package JSON file for non-visual studio execution
                // Task #: 
                var path = Directory.GetCurrentDirectory();

                using (StreamReader file = File.OpenText(path + "\\Pages\\Skills.json"))
                {
                    var jsonString = file.ReadToEnd();
                    m_allCategories = JsonConvert.DeserializeObject<List<SkillCategory>>(jsonString);
                }
            }

            m_semaphore.Release();
            return m_allCategories;
        }
    }
}
