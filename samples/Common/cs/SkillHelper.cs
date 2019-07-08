// Copyright (C) Microsoft Corporation. All rights reserved.

using Microsoft.AI.Skills.SkillInterfacePreview;

namespace SkillHelper
{
    public static class SkillHelperMethods
    {
        /// <summary>
        /// Construct a string from the ISkillDescriptor specified that can be used to display its content
        /// </summary>
        /// <param name="desc"></param>
        /// <returns></returns>
        public static string GetSkillDescriptorString(ISkillDescriptor desc)
        {
            if (desc == null)
            {
                return null;
            }

            return $"{desc.Description}" +
                $"\n\tAuthor: {desc.Version.Author}" +
                $"\n\tPublisher: {desc.Version.Publisher}" +
                $"\n\tVersion: {desc.Version.Major}.{desc.Version.Minor}" +
                $"\n\tUnique ID: {desc.Id}";
        }

        /// <summary>
        /// Construct a string from the ISkillFeatureDescriptor specified that can be used to display its content
        /// </summary>
        /// <param name="desc"></param>
        /// <returns></returns>
        public static string GetSkillFeatureDescriptorString(ISkillFeatureDescriptor desc)
        {
            if (desc == null)
            {
                return null;
            }
            string result = $"\tName: {desc.Name}" +
                    $"\n\tDescription: {desc.Description}" +
                    $"\n\tType: {desc.FeatureKind}" +
                    $"\n\tIsRequired: {desc.IsRequired}";

            if (desc is ISkillFeatureImageDescriptor)
            {
                ISkillFeatureImageDescriptor imageDesc = desc as ISkillFeatureImageDescriptor;
                result += $"\n\tWidth: {imageDesc.Width}" +
                    $"\n\tHeight: {imageDesc.Height}" +
                    $"\n\tSupportedBitmapPixelFormat: {imageDesc.SupportedBitmapPixelFormat}" +
                    $"\n\tSupportedBitmapAlphaMode: {imageDesc.SupportedBitmapAlphaMode}";
            }
            else if (desc is ISkillFeatureTensorDescriptor)
            {
                ISkillFeatureTensorDescriptor tensorDesc = desc as ISkillFeatureTensorDescriptor;
                result += $"\n\tElementKind: {tensorDesc.ElementKind}" +
                    "\n\tShape: [";
                for (int i = 0; i < tensorDesc.Shape.Count; i++)
                {
                    result += $"{tensorDesc.Shape[i]}";
                    if (i < tensorDesc.Shape.Count - 1)
                    {
                        result += ", ";
                    }
                }
                result += "]";
            }
            else if (desc is ISkillFeatureMapDescriptor)
            {
                ISkillFeatureMapDescriptor mapDesc = desc as ISkillFeatureMapDescriptor;
                result += $"\n\tKeyElementKind: {mapDesc.KeyElementKind}" +
                    $"\n\tValueElementKind: {mapDesc.ValueElementKind}" +
                    $"\n\tValidKeys:";
                foreach (var validKey in mapDesc.ValidKeys)
                {
                    result += $"\n\t{validKey}";
                }
            }

            return result;
        }
    }
}