using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GalleryApp
{
    public class SkillView
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        private const string m_namespace = "GalleryApp";
        public Type PageType => System.Type.GetType(m_namespace + "." + Type);
    }
}
