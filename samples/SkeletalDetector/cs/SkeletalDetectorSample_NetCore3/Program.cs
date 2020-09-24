// Copyright (C) Microsoft Corporation. All rights reserved.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Microsoft.AI.Skills.Vision.SkeletalDetector;
using System.Diagnostics;
using Windows.Media.MediaProperties;
using CameraHelper_NetCore3;

namespace SkeletalDetectorSample_NetCore3
{
    class Program
    {
        private static CameraHelper m_cameraHelper = null;
        private static Stopwatch m_evalPerfStopwatch = new Stopwatch();
        private static int m_lock = 0;

        /// <summary>
        /// Entry point of program
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static void Main(string[] args)
        {
            Console.WriteLine("Skeletal Detector .NetCore 3.0 Console App: Please place your body in front of the camera");

            Task.Run(async () =>
            {

                var skillDescriptor = new SkeletalDetectorDescriptor();
                var skill = await skillDescriptor.CreateSkillAsync() as SkeletalDetectorSkill;
                var skillDevice = skill.Device;
                Console.WriteLine("Running Skill on : " + skillDevice.ExecutionDeviceKind.ToString() + ": " + skillDevice.Name);

                var binding = await skill.CreateSkillBindingAsync() as SkeletalDetectorBinding;

                m_cameraHelper = await CameraHelper.CreateCameraHelperAsync(

                    // Register a failure callback
                    new CameraHelper.CameraHelperFailedHandler(message =>
                    {
                        var failureException = new Exception(message);
                        Console.WriteLine(message);
                        Environment.Exit(failureException.HResult);
                    }),

                    // Register the main loop callback to handlke each frame as they come in
                    new CameraHelper.NewFrameArrivedHandler(async (videoFrame) =>
                    {
                        try
                        {
                            // Process 1 frame at a time, if busy return right away
                            if (0 == Interlocked.Exchange(ref m_lock, 1))
                            {
                                m_evalPerfStopwatch.Restart();

                                // Update input image and run the skill against it
                                await binding.SetInputImageAsync(videoFrame);

                                var inputBindTime = (float)m_evalPerfStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000f;
                                m_evalPerfStopwatch.Restart();

                                await skill.EvaluateAsync(binding);

                                var detectionRunTime = (float)m_evalPerfStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000f;
                                m_evalPerfStopwatch.Stop();

                                // Display bind and eval time
                                string outText = DateTime.Now.ToString() + $" | bind: {inputBindTime.ToString("F2")}ms, eval: {detectionRunTime.ToString("F2")}ms | ";
                                if (binding.Bodies == null)
                                {
                                    // If no face found, hide the rectangle in the UI
                                    outText += "No body found";
                                }
                                else // Display the objects detected on the console
                                {
                                    outText += $"Found {binding.Bodies.Count} bodies:";
                                    for(int i = 0; i < binding.Bodies.Count; i++)
                                    {
                                        outText += $"<-B{i + 1}->";
                                        var result = binding.Bodies[i];
                                        foreach (var limb in result.Limbs)
                                        {
                                            outText += $"{limb.Joint1.Label}-{limb.Joint2.Label}|";
                                        }
                                    }
                                }

                                Console.Write("\r" + outText);

                                // Release the lock
                                Interlocked.Exchange(ref m_lock, 0);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error:: " + e.Message.ToString() + e.TargetSite.ToString() + e.Source.ToString() + e.StackTrace.ToString());
                            Environment.Exit(e.HResult);
                        }
                    }));
            }).Wait();


            Console.WriteLine("\nPress Any key to stop\n\n");

            var key = Console.ReadKey();

            Console.WriteLine("\n\n\nExiting...\n\n\n");

            m_cameraHelper.CleanupAsync().Wait();
        }
    }
}
