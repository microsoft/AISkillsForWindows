using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GalleryApp
{
    public class Skill
    {
        public string Name { get; }
        public string Description { get; }
        public string Type { get; }
        private const string m_namespace = "GalleryApp";
        public Type PageType => System.Type.GetType(m_namespace + "." + Type);
    }
}
