using Microsoft.AI.Skills.SkillInterfacePreview;

namespace GalleryApp
{
    /// <summary>
    /// Standard SkillView interface to wrap skill-related objects
    /// </summary>
    interface ISkillViewPage
    {
        /// <summary>
        /// Get the SkillDescriptor object that can be used to display its content on UI
        /// </summary>
        /// <returns> ISkillDescriptor </returns>
        ISkillDescriptor GetSkillDescriptor();
    }
}
