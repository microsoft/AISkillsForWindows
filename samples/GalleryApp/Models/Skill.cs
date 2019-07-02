using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GalleryApp
{
    public class Skill
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public Type PageType => System.Type.GetType("GalleryApp." + Type);
    }
}
