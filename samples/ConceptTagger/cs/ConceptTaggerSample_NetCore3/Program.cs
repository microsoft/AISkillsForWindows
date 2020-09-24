// Copyright (C) Microsoft Corporation. All rights reserved.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Microsoft.AI.Skills.Vision.ConceptTagger;
using System.Diagnostics;
using Windows.Media.MediaProperties;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace ConceptTaggerSample_NetCore3
{


    class Program
    {
        //
        // Load a VideoFrame from a specified image file path
        //
        static async Task<VideoFrame> LoadVideoFrameFromImageFileAsync(string imageFilePath)
        {
            VideoFrame resultFrame = null;
            
            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(imageFilePath);
                var fileExtension = file.FileType;
                if (fileExtension != ".jpg" && fileExtension != ".png")
                {
                    throw new ArgumentException("This app parses only .png and .jpg image files");
                }

                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
                {
                    SoftwareBitmap softwareBitmap = null;

                    // Create the decoder from the stream 
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                    // Get the SoftwareBitmap representation of the file in BGRA8 format
                    softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                    // Convert to friendly format for UI display purpose
                    softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

                    // Encapsulate the image in a VideoFrame instance
                    resultFrame = VideoFrame.CreateWithSoftwareBitmap(softwareBitmap);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not load VideoFrame from file: {imageFilePath}");
                throw ex;
            }
            return resultFrame;
        }

        /// <summary>
        /// Entry point of program
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                int topX = 5;
                float threshold = 0.7f;
                string fileName;

                Console.WriteLine("Concept Tagger .NetCore 3.0 Console App");

                try
                {
                    // Parse arguments
                    if (args.Length < 1)
                    {
                        Console.WriteLine("Allowed command arguments: <file path to .jpg or .png> <optional top X concept tag count> <optional concept tag filter ranging between 0 and 1>");
                        Console.WriteLine("i.e.: \n> ConceptTaggerSample_NetCore3.exe test.jpg 5 0.7\n\n");
                        return;
                    }

                    // Load image from specified file path
                    fileName = args[0];
                    var videoFrame = await LoadVideoFrameFromImageFileAsync(fileName);

                    if (args.Length > 1)
                    {
                        topX = int.Parse(args[1]);
                    }
                    if (args.Length > 2)
                    {
                        threshold = float.Parse(args[2]);
                    }

                    // Create the ConceptTagger skill descriptor
                    var skillDescriptor = new ConceptTaggerDescriptor();

                    // Create instance of the skill
                    var skill = await skillDescriptor.CreateSkillAsync() as ConceptTaggerSkill;
                    var skillDevice = skill.Device;
                    Console.WriteLine("Running Skill on : " + skillDevice.ExecutionDeviceKind.ToString() + ": " + skillDevice.Name);
                    Console.WriteLine($"Image file: {fileName}");
                    Console.WriteLine($"TopX: {topX}");
                    Console.WriteLine($"Threshold: {threshold}");

                    // Create instance of the skill binding
                    var binding = await skill.CreateSkillBindingAsync() as ConceptTaggerBinding;

                    // Set the input image retrieved from file earlier
                    await binding.SetInputImageAsync(videoFrame);

                    // Evaluate the binding
                    await skill.EvaluateAsync(binding);

                    // Retrieve results and display time
                    var results = binding.GetTopXTagsAboveThreshold(topX, threshold);
                    foreach(var result in results)
                    {
                        Console.WriteLine($"\t- {result.Name} : {result.Score}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e.Message.ToString() + e.TargetSite.ToString() + e.Source.ToString() + e.StackTrace.ToString());
                    Console.WriteLine("To get more insight on the parameter format, call the executable without any parameters");
                    Environment.Exit(e.HResult);
                }
            }).Wait();
        }       
    }
}
