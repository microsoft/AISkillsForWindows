using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GalleryApp
{
    public class SkillView
    {
        private const string m_namespace = "GalleryApp";
        public string Name { get; private set; }
        public string Description { get; private set; }
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
        
        public Type PageType { get; private set; }

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
