// Copyright (c) Microsoft Corporation. All rights reserved.

#include <iomanip>
#include <iostream>
#include <string>
#include <winrt/Windows.Foundation.h>
#include <winrt/windows.foundation.collections.h>
#include <winrt/windows.media.h>
#include <winrt/windows.system.threading.h>

#include "CameraHelper_cppwinrt.h"
#include "WindowsVersionHelper.h"
#include "winrt/Microsoft.AI.Skills.SkillInterface.h"
#include "winrt/Microsoft.AI.Skills.Vision.ObjectDetector.h"

using namespace winrt;
using namespace winrt::Windows::Foundation;
using namespace winrt::Windows::Foundation::Collections;
using namespace winrt::Windows::System::Threading;
using namespace winrt::Windows::Media;

using namespace Microsoft::AI::Skills::SkillInterface;
using namespace Microsoft::AI::Skills::Vision::ObjectDetector;

// enum to string lookup table for ObjectKind
static const std::map<ObjectKind, std::string> ObjectKindLookup = {
    { ObjectKind::Undefined, "Undefined" },
    { ObjectKind::Person, "Person" },
    { ObjectKind::Bicycle, "Bicycle" },
    { ObjectKind::Car, "Car" },
    { ObjectKind::Motorbike, "Motorbike" },
    { ObjectKind::Aeroplane, "Aeroplane" },
    { ObjectKind::Bus, "Bus" },
    { ObjectKind::Train, "Train" },
    { ObjectKind::Truck, "Truck" },
    { ObjectKind::Boat, "Boat" },
    { ObjectKind::TrafficLight, "TrafficLight" },
    { ObjectKind::FireHydrant, "FireHydrant" },
    { ObjectKind::StopSign, "StopSign" },
    { ObjectKind::ParkingMeter, "ParkingMeter" },
    { ObjectKind::Bench, "Bench" },
    { ObjectKind::Bird, "Bird" },
    { ObjectKind::Cat, "Cat" },
    { ObjectKind::Dog, "Dog" },
    { ObjectKind::Horse, "Horse" },
    { ObjectKind::Sheep, "Sheep" },
    { ObjectKind::Cow, "Cow" },
    { ObjectKind::Elephant, "Elephant" },
    { ObjectKind::Bear, "Bear" },
    { ObjectKind::Zebra, "Zebra" },
    { ObjectKind::Giraffe, "Giraffe" },
    { ObjectKind::Backpack, "Backpack" },
    { ObjectKind::Umbrella, "Umbrella" },
    { ObjectKind::Handbag, "Handbag" },
    { ObjectKind::Tie, "Tie" },
    { ObjectKind::Suitcase, "Suitcase" },
    { ObjectKind::Frisbee, "Frisbee" },
    { ObjectKind::Skis, "Skis" },
    { ObjectKind::Snowboard, "Snowboard" },
    { ObjectKind::SportsBall, "SportsBall" },
    { ObjectKind::Kite, "Kite" },
    { ObjectKind::BaseballBat, "BaseballBat" },
    { ObjectKind::BaseballGlove, "BaseballGlove" },
    { ObjectKind::Skateboard, "Skateboard" },
    { ObjectKind::Surfboard, "Surfboard" },
    { ObjectKind::TennisRacket, "TennisRacket" },
    { ObjectKind::Bottle, "Bottle" },
    { ObjectKind::WineGlass, "WineGlass" },
    { ObjectKind::Cup, "Cup" },
    { ObjectKind::Fork, "Fork" },
    { ObjectKind::Knife, "Knife" },
    { ObjectKind::Spoon, "Spoon" },
    { ObjectKind::Bowl, "Bowl" },
    { ObjectKind::Banana, "Banana" },
    { ObjectKind::Apple, "Apple" },
    { ObjectKind::Sandwich, "Sandwich" },
    { ObjectKind::Orange, "Orange" },
    { ObjectKind::Broccoli, "Broccoli" },
    { ObjectKind::Carrot, "Carrot" },
    { ObjectKind::HotDog, "HotDog" },
    { ObjectKind::Pizza, "Pizza" },
    { ObjectKind::Donut, "Donut" },
    { ObjectKind::Cake, "Cake" },
    { ObjectKind::Chair, "Chair" },
    { ObjectKind::Sofa, "Sofa" },
    { ObjectKind::PottedPlant, "PottedPlant" },
    { ObjectKind::Bed, "Bed" },
    { ObjectKind::DiningTable, "DiningTable" },
    { ObjectKind::Toilet, "Toilet" },
    { ObjectKind::Tvmonitor, "Tvmonitor" },
    { ObjectKind::Laptop, "Laptop" },
    { ObjectKind::Mouse, "Mouse" },
    { ObjectKind::Remote, "Remote" },
    { ObjectKind::Keyboard, "Keyboard" },
    { ObjectKind::CellPhone, "CellPhone" },
    { ObjectKind::Microwave, "Microwave" },
    { ObjectKind::Oven, "Oven" },
    { ObjectKind::Toaster, "Toaster" },
    { ObjectKind::Sink, "Sink" },
    { ObjectKind::Refrigerator, "Refrigerator" },
    { ObjectKind::Book, "Book" },
    { ObjectKind::Clock, "Clock" },
    { ObjectKind::Vase, "Vase" },
    { ObjectKind::Scissors, "Scissors" },
    { ObjectKind::TeddyBear, "TeddyBear" },
    { ObjectKind::HairDryer, "HairDryer" },
    { ObjectKind::Toothbrush, "Toothbrush" },
};

// enum to string lookup table for SkillExecutionDeviceKind
static const std::map<SkillExecutionDeviceKind, std::string> SkillExecutionDeviceKindLookup = {
    { SkillExecutionDeviceKind::Undefined, "Undefined" },
    { SkillExecutionDeviceKind::Cpu, "Cpu" },
    { SkillExecutionDeviceKind::Gpu, "Gpu" },
    { SkillExecutionDeviceKind::Vpu, "Vpu" },
    { SkillExecutionDeviceKind::Fpga, "Fpga" },
    { SkillExecutionDeviceKind::Cloud, "Cloud" }
};

//
// App main loop
//
int main()
{
    try
    {
        // Check if we are running Windows 10.0.18362.x or above as required
        HRESULT hr = WindowsVersionHelper::EqualOrAboveWindows10Version(18362);
        if (FAILED(hr))
        {
            throw_hresult(hr);
        }
        std::cout << "Object Detector C++/WinRT Non-packaged(win32) console App: Place something to detect in front of the camera" << std::endl;

        // Set and run skill
        try
        {
            // Create the ObjectDetector skill descriptor
            auto skillDescriptor = ObjectDetectorDescriptor().as<ISkillDescriptor>();

            // Create instance of the skill
            auto skill = skillDescriptor.CreateSkillAsync().get().as<ObjectDetectorSkill>();
            std::cout << "Running Skill on : " << SkillExecutionDeviceKindLookup.at(skill.Device().ExecutionDeviceKind());
            std::wcout << L" : " << skill.Device().Name().c_str() << std::endl;
            std::cout << std::fixed;
            std::cout.precision(3);

            // Create instance of the skill binding
            auto binding = skill.CreateSkillBindingAsync().get().as<ObjectDetectorBinding>();

            // Create a mutex to orchestrate skill evaluation one at a time
            winrt::slim_mutex lock;

            // Initialize Camera and register a frame callback handler
            auto cameraHelper = std::shared_ptr<CameraHelper>(
                CameraHelper::CreateCameraHelper(
                    [&](std::string failureMessage) // lambda function that acts as callback for failure event
                    {
                        std::cerr << failureMessage;
                        return 1;
                    },
                    [&](VideoFrame const& videoFrame) // lambda function that acts as callback for new frame event
                    {
                        // Lock context so multiple overlapping events from FrameReader do not race for the resources.
                        if (!lock.try_lock())
                        {
                            return;
                        }

                        // measure time spent binding and evaluating
                        auto begin = std::chrono::high_resolution_clock::now();

                        // Set the video frame on the skill binding.
                        binding.SetInputImageAsync(videoFrame).get();

                        auto end = std::chrono::high_resolution_clock::now();
                        auto bindTime = std::chrono::duration_cast<std::chrono::nanoseconds>(end - begin).count() / 1000000.0f;
                        begin = std::chrono::high_resolution_clock::now();

                        // Detect objects in video frame using the skill
                        skill.EvaluateAsync(binding).get();

                        end = std::chrono::high_resolution_clock::now();
                        auto evalTime = std::chrono::duration_cast<std::chrono::nanoseconds>(end - begin).count() / 1000000.0f;

                        auto detectedObjects = binding.DetectedObjects();

                        // Display bind and eval time
                        std::cout << "bind: " << bindTime << "ms | ";
                        std::cout << "eval: " << evalTime << "ms | ";

                        // Refresh the displayed line with detection result
                        if (detectedObjects.Size() > 0)
                        {
                            for (auto&& obj : detectedObjects)
                            {
                                std::cout << ObjectKindLookup.at(obj.Kind()) << " ";
                            }
                        }
                        else
                        {
                            std::cout << "---------------- No object detected ----------------";
                        }
                        std::cout << "\r";

                        videoFrame.Close();

                        lock.unlock();
                    }));

            std::cout << "\t\t\t\t\t\t\t\t...press enter to Stop" << std::endl;

            // Wait for enter keypress
            while (std::cin.get() != '\n');

            std::cout << std::endl << "Key pressed.. exiting";

            // De-initialize the MediaCapture and FrameReader
            cameraHelper->Cleanup();
        }
        catch (hresult_error const& ex)
        {
            std::wcerr << "Error:" << ex.message().c_str() << ":" << std::hex << ex.code().value << std::endl;
            return ex.code().value;
        }
        return 0;
    }
    catch (hresult_error const& ex)
    {
        std::cerr << "Error:" << std::hex << ex.code() << ":" << ex.message().c_str();
    }
}