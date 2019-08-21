// Copyright (C) Microsoft Corporation. All rights reserved.

using Microsoft.AI.Skills.SkillInterfacePreview;
using System;
using System.Collections.Generic;

namespace SkillHelper
{
    /// <summary>
    /// Class that exposes several facilities to handle skill information
    /// </summary>
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
                return "";
            }

            return $"Description: {desc.Information.Description}" +
                $"\nAuthor: {desc.Information.Author}" +
                $"\nPublisher: {desc.Information.Publisher}" +
                $"\nVersion: {desc.Information.Version.Major}.{desc.Information.Version.Minor}.{desc.Information.Version.Build}.{desc.Information.Version.Revision}" +
                $"\nUnique ID: {desc.Information.Id}";
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
                return "";
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

        /// <summary>
        /// Get a list of information strings extracted from the ISkillDescriptor
        /// </summary>
        /// <param name="desc"></param>
        /// <returns></returns>
        public static List<KeyValuePair<string, string>> GetSkillInformationStrings(ISkillDescriptor desc)
        {
            if (desc == null)
            {
                return new List<KeyValuePair<string, string>>();
            }

            return new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("Description", $"{desc.Information.Description}"),
                new KeyValuePair<string, string>("Author", $"{desc.Information.Author}"),
                new KeyValuePair<string, string>("Publisher", $"{desc.Information.Publisher}"),
                new KeyValuePair<string, string>("Version", $"{desc.Information.Version.Major}.{desc.Information.Version.Minor}.{desc.Information.Version.Build}.{desc.Information.Version.Revision}"),
                new KeyValuePair<string, string>("Unique ID", $"{ desc.Information.Id}")
            };
        }

        /// <summary>
        /// Get a list of information strings extracted from the ISkillFeatureDescriptor
        /// </summary>
        /// <param name="desc"></param>
        /// <returns></returns>
        public static List<KeyValuePair<string, string>> GetSkillFeatureDescriptorStrings(ISkillFeatureDescriptor desc)
        {
            if (desc == null)
            {
                return new List<KeyValuePair<string, string>>();
            }
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("Name", $"{desc.Name}"),
                new KeyValuePair<string, string>("Description", $"{desc.Description}"),
                new KeyValuePair<string, string>("IsRequired", $"{desc.IsRequired}"),
                new KeyValuePair<string, string>("Type", $"{desc.FeatureKind}"),
            };
           
            if (desc is ISkillFeatureImageDescriptor)
            {
                ISkillFeatureImageDescriptor imageDesc = desc as ISkillFeatureImageDescriptor;
                result.Add(new KeyValuePair<string, string>("Width", $"{imageDesc.Width}"));
                result.Add(new KeyValuePair<string, string>("Height", $"{imageDesc.Height}"));
                result.Add(new KeyValuePair<string, string>("SupportedBitmapPixelFormat", $"{imageDesc.SupportedBitmapPixelFormat}"));
                result.Add(new KeyValuePair<string, string>("SupportedBitmapAlphaMode", $"{imageDesc.SupportedBitmapAlphaMode}"));
            }
            else if (desc is ISkillFeatureTensorDescriptor)
            {
                ISkillFeatureTensorDescriptor tensorDesc = desc as ISkillFeatureTensorDescriptor;
                result.Add(new KeyValuePair<string, string>("ElementKind", $"{tensorDesc.ElementKind}"));
                string shape = "[";
                for (int i = 0; i < tensorDesc.Shape.Count; i++)
                {
                    shape += $"{tensorDesc.Shape[i]}";
                    if (i < tensorDesc.Shape.Count - 1)
                    {
                        shape += ", ";
                    }
                }
                shape += "]";
                result.Add(new KeyValuePair<string, string>("Shape", shape));
            }
            else if (desc is ISkillFeatureMapDescriptor)
            {
                ISkillFeatureMapDescriptor mapDesc = desc as ISkillFeatureMapDescriptor;
                result.Add(new KeyValuePair<string, string>("KeyElementKind", $"{mapDesc.KeyElementKind}"));
                result.Add(new KeyValuePair<string, string>("ValueElementKind", $"{mapDesc.ValueElementKind}"));
                string validKeys = "";
                foreach (var validKey in mapDesc.ValidKeys)
                {
                    validKeys += $"{validKey}\n";
                }
                result.Add(new KeyValuePair<string, string>("ValidKeys", validKeys));
            }

            return result;
        }
    }
}