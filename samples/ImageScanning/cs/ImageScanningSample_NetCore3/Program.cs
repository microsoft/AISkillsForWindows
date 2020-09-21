// Copyright (C) Microsoft Corporation. All rights reserved.

using System;
using System.Threading.Tasks;
using Windows.Media;
using Microsoft.AI.Skills.SkillInterface;
using Microsoft.AI.Skills.Vision.ImageScanning;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace ImageScanningSample_NetCore3
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

        //
        // Save a modified VideoFrame using an existing image file path with an appended suffix
        //
        static async Task<string> SaveModifiedVideoFrameToFileAsync(string imageFilePath, VideoFrame frame)
        {
            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(imageFilePath);
                StorageFolder folder = await file.GetParentAsync();
                imageFilePath = file.Name.Replace(file.FileType, "_mod.jpg");
                StorageFile modifiedFile = await folder.CreateFileAsync(imageFilePath, CreationCollisionOption.GenerateUniqueName);
                imageFilePath = modifiedFile.Path;
                // Create the encoder from the stream
                using (IRandomAccessStream stream = await modifiedFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                    SoftwareBitmap softwareBitmap = frame.SoftwareBitmap;
                    encoder.SetSoftwareBitmap(softwareBitmap);
                    await encoder.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not save modified VideoFrame from file: {imageFilePath}");
                throw ex;
            }
            return imageFilePath;
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
                string fileName;
                ImageInterpolationKind imageInterpolationKind = ImageInterpolationKind.Bilinear; // default value if none specified as argument
                ImageCleaningKind imageCleaningPreset = ImageCleaningKind.WhiteboardOrDocument; // default value if none specified as argument
                bool skipQuadDetectionImageRectification = false;

                Console.WriteLine("Image Scanning .NetCore 3.0 Console App - This app executes a common productivity scenario using an input image and saving the result image to file:\n" +
                    "1. finds the predominant quadrangle\n" +
                    "2. uses this quadrangle to rectify and crop the image\n" +
                    "3. cleans the rectified image\n\n");

                try
                {
                    // Parse arguments
                    if (args.Length < 1)
                    {
                        Console.WriteLine($"Allowed command arguments: <file path to .jpg or .png>" +
                                            " <optional image rectifier interpolation to apply to the rectified image:\n" +
                                            $"\t1. {ImageInterpolationKind.Bilinear}\n" +
                                            $"\t2. {ImageInterpolationKind.Bicubic}>\n" +
                                            $"\t3. {ImageInterpolationKind.HighQuality}>\n" +
                                            "<optional image cleaning preset to apply to the rectified image:\n" +
                                            $"\t1. {ImageCleaningKind.WhiteboardOrDocument}\n" +
                                            $"\t2. {ImageCleaningKind.Whiteboard}\n" +
                                            $"\t3. {ImageCleaningKind.Document}\n" +
                                            $"\t4. {ImageCleaningKind.Picture}> ");
                        Console.WriteLine("i.e.: \n> ImageScanningSample_NetCore3.exe c:\\test\\test.jpg 1 1\n\n");
                        return;
                    }

                    // Load image from specified file path
                    fileName = args[0];
                    var videoFrame = await LoadVideoFrameFromImageFileAsync(fileName);

                    // Parse optional image interpolation preset argument
                    int selection = 0;
                    if (args.Length < 2)
                    {
                        while (selection < 1 || selection > 3)
                        {
                            Console.WriteLine($"Select the image rectifier interpolation to apply to the rectified image:\n" +
                                            $"\t1. {ImageInterpolationKind.Bilinear}\n" +
                                            $"\t2. {ImageInterpolationKind.Bicubic}\n" +
                                            $"\t2. {ImageInterpolationKind.HighQuality}\n" +
                                            $"\t3. Skip Quad Detection and Image Rectification\n");
                            selection = int.Parse(Console.ReadLine());
                        }
                    }
                    else
                    {
                        selection = int.Parse(args[1]);
                        if (selection < 1 || selection > 3)
                        {
                            Console.WriteLine($"Invalid image rectifier interpolation specified, defaulting to {ImageInterpolationKind.Bilinear.ToString()}");
                            selection = (int)ImageInterpolationKind.Bilinear + 1;
                        }
                    }
                    skipQuadDetectionImageRectification = (selection == 3);
                    if (!skipQuadDetectionImageRectification)
                    {
                        imageInterpolationKind = (ImageInterpolationKind)(selection - 1);
                    }

                    // Parse optional image cleaning preset argument
                    selection = 0;
                    if (args.Length < 3)
                    {
                        while (selection < 1 || selection > 4)
                        {
                            Console.WriteLine($"Select the image cleaning preset to apply to the rectified image:\n" +
                                            $"\t1. {ImageCleaningKind.WhiteboardOrDocument}\n" +
                                            $"\t2. { ImageCleaningKind.Whiteboard}\n" +
                                            $"\t3. { ImageCleaningKind.Document}\n" +
                                            $"\t4. { ImageCleaningKind.Picture}");
                            selection = int.Parse(Console.ReadLine());
                        }
                    }
                    else
                    {
                        selection = int.Parse(args[1]);
                        if (selection < 1 || selection > 4)
                        {
                            Console.WriteLine($"Invalid image cleaning preset specified, defaulting to {ImageCleaningKind.WhiteboardOrDocument.ToString()}");
                            selection = (int)ImageCleaningKind.WhiteboardOrDocument + 1;
                        }
                    }
                    imageCleaningPreset = (ImageCleaningKind)(selection - 1);

                    // Create the skill descriptors
                    QuadDetectorDescriptor quadDetectorSkillDescriptor = null;
                    ImageRectifierDescriptor imageRectifierSkillDescriptor = null;
                    ImageCleanerDescriptor imageCleanerSkillDescriptor = new ImageCleanerDescriptor();
                    if (!skipQuadDetectionImageRectification)
                    {
                        quadDetectorSkillDescriptor = new QuadDetectorDescriptor();
                        imageRectifierSkillDescriptor = new ImageRectifierDescriptor();
                    }


                    // Create instance of the skills
                    QuadDetectorSkill quadDetectorSkill = null;
                    ImageRectifierSkill imageRectifierSkill = null;
                    ImageCleanerSkill imageCleanerSkill = await imageCleanerSkillDescriptor.CreateSkillAsync() as ImageCleanerSkill;
                    if (!skipQuadDetectionImageRectification)
                    {
                        quadDetectorSkill = await quadDetectorSkillDescriptor.CreateSkillAsync() as QuadDetectorSkill;
                        imageRectifierSkill = await imageRectifierSkillDescriptor.CreateSkillAsync() as ImageRectifierSkill;
                    }
                    var skillDevice = imageCleanerSkill.Device;
                    Console.WriteLine("Running Skill on : " + skillDevice.ExecutionDeviceKind.ToString() + ": " + skillDevice.Name);
                    Console.WriteLine($"Image file: {fileName}");

                    VideoFrame imageCleanerInputImage = videoFrame;
                    if (!skipQuadDetectionImageRectification)
                    {
                        // ### 1. Quad detection ###
                        // Create instance of QuadDetectorBinding and set features
                        var quadDetectorBinding = await quadDetectorSkill.CreateSkillBindingAsync() as QuadDetectorBinding;
                        await quadDetectorBinding.SetInputImageAsync(videoFrame);

                        // Run QuadDetectorSkill
                        await quadDetectorSkill.EvaluateAsync(quadDetectorBinding);

                        // ### 2. Image rectification ###
                        // Create instance of ImageRectifierBinding and set input features
                        var imageRectifierBinding = await imageRectifierSkill.CreateSkillBindingAsync() as ImageRectifierBinding;
                        await imageRectifierBinding.SetInputImageAsync(videoFrame);
                        await imageRectifierBinding.SetInputQuadAsync(quadDetectorBinding.DetectedQuads);
                        imageRectifierBinding.SetInterpolationKind(imageInterpolationKind);

                        // Run ImageRectifierSkill
                        await imageRectifierSkill.EvaluateAsync(imageRectifierBinding);

                        // Use the image rectification result as input image to the image cleaner skill
                        imageCleanerInputImage = imageRectifierBinding.OutputImage;
                    }
                    // ### 3. Image cleaner ###
                    // Create instance of QuadDetectorBinding and set features
                    var imageCleanerBinding = await imageCleanerSkill.CreateSkillBindingAsync() as ImageCleanerBinding;
                    await imageCleanerBinding.SetImageCleaningKindAsync(imageCleaningPreset);
                    await imageCleanerBinding.SetInputImageAsync(imageCleanerInputImage);

                    // Run ImageCleanerSkill
                    await imageCleanerSkill.EvaluateAsync(imageCleanerBinding);

                    // Retrieve result and save it to file
                    var results = imageCleanerBinding.OutputImage;

                    string outputFilePath = await SaveModifiedVideoFrameToFileAsync(fileName, results);
                    Console.WriteLine($"Written output image to {outputFilePath}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error: {e.TargetSite.ToString()}\n{e.Source.ToString()}\n{e.StackTrace.ToString()}\n{e.Message.ToString()}");
                    Console.WriteLine("To get more insight on the parameter format, call the executable without any parameters");
                    Environment.Exit(e.HResult);
                }
            }).Wait();
        }
    }
}
