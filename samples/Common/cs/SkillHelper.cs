// Copyright (C) Microsoft Corporation. All rights reserved.

using Microsoft.AI.Skills.SkillInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkillHelper
{
    /// <summary>
    /// Helper class to encapsulate an ISkillDescriptor, ISkill and ISkillBinding
    /// </summary>
    public class SkillWrapper
    {
        public ISkill Skill { get; private set; }
        public ISkillDescriptor Descriptor { get; private set; }
        public ISkillBinding Binding { get; private set; }
        public IReadOnlyList<ISkillExecutionDevice> ExecutionDevices { get; private set; }

        /// <summary>
        /// SkillRuntimeEntry constructor
        /// </summary>
        /// <param name="descriptor"></param>
        public SkillWrapper(ISkillDescriptor descriptor)
        {
            Descriptor = descriptor;
            ExecutionDevices = descriptor.GetSupportedExecutionDevicesAsync().GetResults();
            Binding = null;
        }

        /// <summary>
        /// Initialize the ISkill member instance if does not exists or conform to the specified device as well as the ISkillBinding member instance
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public async Task InitializeSkillAsync(ISkillExecutionDevice device = null)
        {
            if(device == null)
            {
                Skill = await Descriptor.CreateSkillAsync();
            }
            else if (device != null && (Skill == null || Skill.Device != device))
            {
                Skill = await Descriptor.CreateSkillAsync(device);
            }
            Binding = await Skill.CreateSkillBindingAsync();
        }
    }

    /// <summary>
    /// Simple wrapper class to help in binding ISkillFeatureDescriptor to UI in XAML
    /// </summary>
    public class SkillFeatureDescriptorWrappper
    {
        public SkillFeatureDescriptorWrappper(ISkillFeatureDescriptor descriptor)
        {
            Descriptor = descriptor;
        }
        public ISkillFeatureDescriptor Descriptor { get; private set; }
    }

    /// <summary>
    /// Simple wrapper class to help in binding ISkillExecutionDevice to UI in XAML
    /// </summary>
    public class SkillExecutionDeviceWrappper
    {
        public SkillExecutionDeviceWrappper(ISkillExecutionDevice device)
        {
            Device = device;
        }
        public ISkillExecutionDevice Device { get; private set; }
    }

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
        public static List<KeyValuePair<string, string>> GetSkillInformationStrings(SkillInformation info)
        {
            if (info == null)
            {
                return new List<KeyValuePair<string, string>>();
            }

            return new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("Description", $"{info.Description}"),
                new KeyValuePair<string, string>("Author", $"{info.Author}"),
                new KeyValuePair<string, string>("Publisher", $"{info.Publisher}"),
                new KeyValuePair<string, string>("Version", $"{info.Version.Major}.{info.Version.Minor}.{info.Version.Build}.{info.Version.Revision}"),
                new KeyValuePair<string, string>("Unique ID", $"{ info.Id}")
            };
        }

        /// <summary>
        /// Get a list of strings extracted from the ISkillExecutionDevice describing it
        /// </summary>
        /// <param name="desc"></param>
        /// <returns></returns>
        public static List<KeyValuePair<string, string>> GetSkillExecutionDeviceStrings(ISkillExecutionDevice device)
        {
            if (device == null)
            {
                return new List<KeyValuePair<string, string>>();
            }
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("Name", $"{device.Name}"),
                new KeyValuePair<string, string>("Kind", $"{device.ExecutionDeviceKind}"),
            };

            if (device is SkillExecutionDeviceCPU)
            {
                SkillExecutionDeviceCPU cpuDevice = device as SkillExecutionDeviceCPU;
                result.Add(new KeyValuePair<string, string>("CoreCount", $"{cpuDevice.CoreCount}"));
            }
            else if (device is SkillExecutionDeviceDirectX)
            {
                SkillExecutionDeviceDirectX directXDevice = device as SkillExecutionDeviceDirectX;
                result.Add(new KeyValuePair<string, string>("DedicatedVideoMemory", $"{directXDevice.DedicatedVideoMemory}"));
                result.Add(new KeyValuePair<string, string>("MaxSupportedFeatureLevel", $"{directXDevice.MaxSupportedFeatureLevel}"));
                result.Add(new KeyValuePair<string, string>("IsDefault", $"{directXDevice.IsDefault}"));
                result.Add(new KeyValuePair<string, string>("HighPerformanceIndex", $"{directXDevice.HighPerformanceIndex}"));
                result.Add(new KeyValuePair<string, string>("PowerSavingIndex", $"{directXDevice.PowerSavingIndex}"));
                result.Add(new KeyValuePair<string, string>("AdapterId", $"{directXDevice.AdapterId}"));
            }

            return result;
        }

        /// <summary>
        /// Retrieve wrappers around SkillFeatureDescriptor
        /// </summary>
        /// <param name="featureDescriptors"></param>
        /// <returns></returns>
        public static IEnumerable<SkillFeatureDescriptorWrappper> GetFeatureDescriptorWrappers(IReadOnlyList<ISkillFeatureDescriptor> featureDescriptors)
        {
            return featureDescriptors.Select((x) => new SkillFeatureDescriptorWrappper(x));
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
                result.Add(new KeyValuePair<string, string>("Width", $"{(imageDesc.Width == -1 ? "Free Dimension" : imageDesc.Width.ToString())}"));
                result.Add(new KeyValuePair<string, string>("Height", $"{(imageDesc.Height == -1 ? "Free Dimension" : imageDesc.Height.ToString())}"));
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
                    shape += $"{ (tensorDesc.Shape[i] == -1 ? "Free Dimension" : tensorDesc.Shape[i].ToString())}";
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