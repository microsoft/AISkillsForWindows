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
        public Type PageType { get; set; }

        public Skill(string Name, string Description, Type PageType)
        {
            this.Name = Name;
            this.Description = Description;
            this.PageType = PageType;
        }
    }
}
