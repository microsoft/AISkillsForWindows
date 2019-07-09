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

            return $"Description: {desc.Description}" +
                $"\nAuthor: {desc.Version.Author}" +
                $"\nPublisher: {desc.Version.Publisher}" +
                $"\nVersion: {desc.Version.Major}.{desc.Version.Minor}" +
                $"\nUnique ID: {desc.Id}";
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
            string result = $"Name: {desc.Name}" +
                    $"\nDescription: {desc.Description}" +
                    $"\nIsRequired: {desc.IsRequired}" +
                    $"\nType: {desc.FeatureKind}";
                    

            if (desc is ISkillFeatureImageDescriptor)
            {
                ISkillFeatureImageDescriptor imageDesc = desc as ISkillFeatureImageDescriptor;
                result += $"\nWidth: {imageDesc.Width}" +
                    $"\nHeight: {imageDesc.Height}" +
                    $"\nSupportedBitmapPixelFormat: {imageDesc.SupportedBitmapPixelFormat}" +
                    $"\nSupportedBitmapAlphaMode: {imageDesc.SupportedBitmapAlphaMode}";
            }
            else if (desc is ISkillFeatureTensorDescriptor)
            {
                ISkillFeatureTensorDescriptor tensorDesc = desc as ISkillFeatureTensorDescriptor;
                result += $"\nElementKind: {tensorDesc.ElementKind}" +
                    "\nShape: [";
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
                result += $"\nKeyElementKind: {mapDesc.KeyElementKind}" +
                    $"\nValueElementKind: {mapDesc.ValueElementKind}" +
                    $"\nValidKeys:";
                foreach (var validKey in mapDesc.ValidKeys)
                {
                    result += $"\n\t{validKey}";
                }
            }

            return result;
        }
    }
}