using System;

namespace GalleryApp
{
    /// <summary>
    /// This class represents a skill and contains skill related information
    /// </summary>
    public class SkillView
    {
        private const string m_namespace = "GalleryApp";

        /// <value> Get the page type string from JSON file </value>
        public string PageTypeStr
        {
            get
            {
                return PageType.ToString();
            }
            set
            {
                PageType = System.Type.GetType(m_namespace + "." + value);
                InitializeWithSkillDescriptor();
            }
        }

        public string Name { get; private set; }
        public string Description { get; private set; }

        /// <value> Get the PageType for frame navigation </value>
        public Type PageType { get; private set; }

        /// <summary>
        /// Get skill information from the corresponding skill descriptor object and assign to the related fields
        /// </summary>
        private void InitializeWithSkillDescriptor()
        {
            var page = Activator.CreateInstance(PageType) as ISkillViewPage;
            var descriptor = page.GetSkillDescriptor();
            Name = descriptor.Name;
            Description = descriptor.Description;
        }

        private SkillView()
        {
            Name = "";
            Description = "";
        }
    }
}
