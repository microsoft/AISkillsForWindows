namespace GalleryApp.Models
{
    /// <summary>
    /// This class represents a skill category and contains a list of skills that belongs to this category
    /// </summary>
    public class SkillCategory
    {
        public string Name { get; set; }
        public SkillView[] SkillViews { get; set; }
    }
}
