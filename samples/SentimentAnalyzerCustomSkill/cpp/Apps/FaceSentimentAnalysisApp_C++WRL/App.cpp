// Copyright (c) Microsoft Corporation. All rights reserved.

#include "App.h"
#include <locale>
#include <codecvt>

// Function to handle the frame when it arrives from FrameReader
HRESULT App::FrameArrivedHandler(IMediaFrameReader* pFrameReader, IMediaFrameArrivedEventArgs*)
{
    HRESULT hr = S_OK;
    ComPtr<IMediaFrameReference> spFrameRef;
    ComPtr<IVideoMediaFrame> spVideoMediaFrame;
    ComPtr<IVideoFrame> spVideoFrame;
    ComPtr<ISkillBinding> spSkillBinding;
    ComPtr<IAsyncAction> spOp;

    // Lock context so multiple overlapping events from FrameReader do not race for the resources.
    AcquireSRWLockExclusive(&m_lock);

    // Try to get the actual Video Frame from the FrameReader
    CHECKHR_GOTO(pFrameReader->TryAcquireLatestFrame(&spFrameRef), cleanup);
    if (spFrameRef == nullptr)
    {
        hr = S_OK; goto cleanup;
    }

    CHECKHR_GOTO(spFrameRef->get_VideoMediaFrame(&spVideoMediaFrame), cleanup);
    if (spVideoMediaFrame == nullptr)
    {
        hr = S_OK; goto cleanup;
    }
    CHECKHR_GOTO(spVideoMediaFrame->GetVideoFrame(&spVideoFrame), cleanup);

    // Set the video frame on the skill binding.
    CHECKHR_GOTO(m_spFaceSentimentSkillBinding->SetInputImageAsync(spVideoFrame.Get(), &spOp), cleanup);
    CHECKHR_GOTO(AwaitAction(spOp), cleanup);

    // QI the binding to generic base interface as we are using the base interface for the skill to evaluate
    CHECKHR_GOTO(m_spFaceSentimentSkillBinding.As(&spSkillBinding), cleanup);

    // Evaluate sentiments in video frame using the skill
    CHECKHR_GOTO(m_spSkill->EvaluateAsync(spSkillBinding.Get(), spOp.ReleaseAndGetAddressOf()), cleanup);
    CHECKHR_GOTO(AwaitAction(spOp), cleanup);

    boolean bIsFaceFound;
    std::cout << "Frame arrived:";
    CHECKHR_GOTO(m_spFaceSentimentSkillBinding->get_IsFaceFound(&bIsFaceFound), cleanup);
    if (bIsFaceFound)
    {
        static const std::string classifiedSentiments[] = {
            "neutral ",
            "happiness ",
            "surprise ",
            "sadness ",
            "anger ",
            "disgust",
            "fear",
            "contempt"
        };
        ComPtr<IVectorView<SentimentType>> sentiments;
        SentimentType sentiment;
        CHECKHR_GOTO(m_spFaceSentimentSkillBinding->get_PredominantSentiments(&sentiments), cleanup);
        CHECKHR_GOTO(sentiments->GetAt(0, &sentiment), cleanup);
        std::string outText = " Your sentiment looks like:  " + classifiedSentiments[(int)(sentiment)] + "\t\t\t\t\r";

        std::cout << outText;
    }
    else
    {
        std::cout << "no face\t\t\t\t\t\t\r";
    }


cleanup:
    ReleaseSRWLockExclusive(&m_lock);
    if (hr != S_OK)
    {
        std::cout << "Failed:" << std::hex << hr;
    }
    return hr;
}

HRESULT App::InitCameraAndFrameSource()
{
    HRESULT hr = S_OK;
    ComPtr<IMediaCapture5> spMediaCaptureFS;
    ComPtr<IAsyncAction> op;
    ComPtr<IMapView<HSTRING, MediaFrameSource*>> fsList;
    ComPtr<IIterable<IKeyValuePair<HSTRING, MediaFrameSource*>*>> spIterable;
    ComPtr<IIterator<IKeyValuePair<HSTRING, MediaFrameSource*>*>> spIterator;
    ComPtr<IKeyValuePair<HSTRING, MediaFrameSource*>>             spKeyValue;
    ComPtr<IMediaCaptureInitializationSettings> spMediaCaptureInitializationSettings;
    ComPtr<IMediaCaptureInitializationSettings5> spMediaCaptureInitializationSettings5;

    // Get an instance of the Windows MediaCapture runtime class
    CHECKHR_GOTO(ActivateInstance(HStringReference(RuntimeClass_Windows_Media_Capture_MediaCapture).Get(), &m_spMediaCapture), cleanup);

    // Initialize media capture with default settings in video-only streaming mode and in shared mode so that multiple instances can access the camera concurrently
    CHECKHR_GOTO(ActivateInstance(HStringReference(RuntimeClass_Windows_Media_Capture_MediaCaptureInitializationSettings).Get(), &spMediaCaptureInitializationSettings5), cleanup);
    CHECKHR_GOTO(spMediaCaptureInitializationSettings5->put_SharingMode(ABI::Windows::Media::Capture::MediaCaptureSharingMode::MediaCaptureSharingMode_SharedReadOnly), cleanup);
    CHECKHR_GOTO(spMediaCaptureInitializationSettings5.As(&spMediaCaptureInitializationSettings), cleanup);
    CHECKHR_GOTO(spMediaCaptureInitializationSettings->put_StreamingCaptureMode(ABI::Windows::Media::Capture::StreamingCaptureMode::StreamingCaptureMode_Video), cleanup);
    CHECKHR_GOTO(m_spMediaCapture->InitializeWithSettingsAsync(spMediaCaptureInitializationSettings.Get(), &op), cleanup);
    CHECKHR_GOTO(AwaitAction(op), cleanup);

    // QueryInterface to the IMediaCapture5 interface which gives us the ability to create a MediaFrameReader 
    CHECKHR_GOTO(m_spMediaCapture.As(&spMediaCaptureFS), cleanup);

    // Get a list of available Frame source and iterate through them to find a Video preview/record source with Color images ( and not IR/depth etc)
    CHECKHR_GOTO(spMediaCaptureFS->get_FrameSources(&fsList), cleanup);
    CHECKHR_GOTO(fsList.As(&spIterable), cleanup);
    CHECKHR_GOTO(spIterable->First(&spIterator), cleanup);
    boolean bHasCurrent;
    CHECKHR_GOTO(spIterator->get_HasCurrent(&bHasCurrent), cleanup);
    if (bHasCurrent)
    {
        ComPtr<IKeyValuePair<HSTRING, MediaFrameSource*>> spKeyValue;
        ComPtr<IMediaFrameSource> spFrameSource;
        ComPtr<IAsyncOperation<MediaFrameReader*>> spOp;
        ComPtr<IAsyncOperation<MediaFrameReaderStartStatus>> spOp1;
        MediaFrameReaderStartStatus Stat;
        CHECKHR_GOTO(spIterator->get_Current(&spKeyValue), cleanup);
        CHECKHR_GOTO(spKeyValue->get_Value(&spFrameSource), cleanup);
        ComPtr<IMediaFrameSourceInfo> spInfo;
        ComPtr<IDeviceInformation> spDeviceInformation;
        HSTRING deviceName;
        HString deviceName2;
        spFrameSource->get_Info(&spInfo);
        MediaStreamType streamType;
        MediaFrameSourceKind sourceKind;

        do
        {
            if (!bHasCurrent)
            {
                std::cout << "No valid video frame sources were found with source type color.";
                CHECKHR_GOTO(MF_E_INVALIDMEDIATYPE, cleanup);
            }
            CHECKHR_GOTO(spIterator->get_Current(spKeyValue.ReleaseAndGetAddressOf()), cleanup);
            CHECKHR_GOTO(spKeyValue->get_Value(spFrameSource.ReleaseAndGetAddressOf()), cleanup);
            CHECKHR_GOTO(spFrameSource->get_Info(spInfo.ReleaseAndGetAddressOf()), cleanup);
            CHECKHR_GOTO(spInfo->get_MediaStreamType(&streamType), cleanup);
            CHECKHR_GOTO(spInfo->get_SourceKind(&sourceKind), cleanup);
            CHECKHR_GOTO(spInfo->get_DeviceInformation(&spDeviceInformation), cleanup);
            CHECKHR_GOTO(spDeviceInformation->get_Name(&deviceName), cleanup);
            CHECKHR_GOTO(deviceName2.Set(deviceName), cleanup);

            std::wcout << deviceName2.GetRawBuffer(nullptr) << " | FrameSourceType:" << streamType << std::endl;

            CHECKHR_GOTO(spIterator->MoveNext(&bHasCurrent), cleanup);

        } while (((streamType != MediaStreamType::MediaStreamType_VideoPreview) && (streamType != MediaStreamType::MediaStreamType_VideoRecord)) || (sourceKind != MediaFrameSourceKind::MediaFrameSourceKind_Color));

        // Create FrameReader with the FrameSource that we selected in the loop above.
        CHECKHR_GOTO(spMediaCaptureFS->CreateFrameReaderAsync(spFrameSource.Get(), &spOp), cleanup);
        CHECKHR_GOTO(AwaitTypedResult(spOp, MediaFrameReader*, m_spFrameReader), cleanup);

        // Set up a delegate to handle the frames when they are ready
        m_spFrameArrivedHandlerDelegate = 
            Microsoft::WRL::Callback<ITypedEventHandler<MediaFrameReader*, MediaFrameArrivedEventArgs*>>
            ([&](IMediaFrameReader * pFrameReader, IMediaFrameArrivedEventArgs* eventArgs)
                {
                    return FrameArrivedHandler(pFrameReader, eventArgs);
                }
            );

        CHECKHR_GOTO(m_spFrameReader->add_FrameArrived(m_spFrameArrivedHandlerDelegate.Get(), &m_token), cleanup);

        // Finally start the FrameReader
        CHECKHR_GOTO(m_spFrameReader->StartAsync(&spOp1), cleanup);
        CHECKHR_GOTO(AwaitTypedResult(spOp1, MediaFrameReaderStartStatus, Stat), cleanup);
    }

cleanup:
    return hr;
}

HRESULT App::DeInitCameraAndFrameSource()
{
    HRESULT hr = S_OK;
    ComPtr<IAsyncAction> spOp;

    // Remove the frame handler and stop FrameReader
    CHECKHR_GOTO(m_spFrameReader->remove_FrameArrived(m_token), cleanup);
    CHECKHR_GOTO(m_spFrameReader->StopAsync(&spOp), cleanup);
    CHECKHR_GOTO(AwaitAction(spOp), cleanup);
    m_spFrameReader = nullptr;
    m_spMediaCapture = nullptr;
    
cleanup:
    return hr;

}

int App::AppMain()
{
    HRESULT hr;
    ComPtr<ISkillDescriptor> spSkillDesc;
    ComPtr<IAsyncOperation<ISkill*> > spOp;
    ComPtr<ISkillBinding> spSkillBinding;
    ComPtr<IActivationFactory> spFactory;
    ComPtr<IAsyncOperation<ISkillBinding*>> spOp1;
    std::cout << "C++ WRL Non-packaged(win32) console APP: Please face your camera" << std::endl;

    // Initialize Runtime enviroment
    RoInitializeWrapper initialize(RO_INIT_MULTITHREADED);
    CHECKHR_GOTO(initialize, cleanup);

    // Activate instance of the FaceSentimentAnalyzer skill descriptor runtimeclass
    CHECKHR_GOTO(ActivateInstance(HStringReference(RuntimeClass_Contoso_FaceSentimentAnalyzer_FaceSentimentAnalyzerDescriptor).Get(), &spSkillDesc), cleanup);

    // Create instance of the skill
    CHECKHR_GOTO(spSkillDesc->CreateSkillAsync(&spOp), cleanup);
    CHECKHR_GOTO(Await(spOp, m_spSkill), cleanup);

    // Create instance of the skill binding
    CHECKHR_GOTO(m_spSkill->CreateSkillBindingAsync(&spOp1), cleanup);
    CHECKHR_GOTO(Await(spOp1, spSkillBinding), cleanup);
    // Query the face sentiment analyzer specialized interface from the base interface
    CHECKHR_GOTO(spSkillBinding.As(&m_spFaceSentimentSkillBinding), cleanup);

    // Initialize MediaCapture and FrameReader
    CHECKHR_GOTO(InitCameraAndFrameSource(), cleanup);
    std::cout << "\t\t\t\t\t\t\t\t...press enter to Stop" << std::endl;

    // Wait for enter keypress and let the frame event handler do the work for each frame 
    while (std::cin.get() != '\n');

    std::cout << std::endl << "Key pressed.. exiting";

    // De-initialize the MediaCapture and FrameReader
    CHECKHR_GOTO(DeInitCameraAndFrameSource(), cleanup);

cleanup:
    return hr;
}

