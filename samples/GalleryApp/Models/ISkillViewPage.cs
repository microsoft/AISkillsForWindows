using Microsoft.AI.Skills.SkillInterfacePreview;

namespace GalleryApp
{
    interface ISkillViewPage
    {
        /// <summary>
        /// Get SkillDescriptor of a skill
        /// </summary>
        /// <returns></returns>
        ISkillDescriptor GetSkillDescriptor();
    }
}
