// Copyright (c) Microsoft Corporation. All rights reserved.

#include "App.h"

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

    // QI the binding to geneneric base interface as we are using the base interface for the skill to evaluate
    CHECKHR_GOTO(m_spFaceSentimentSkillBinding.As(&spSkillBinding), cleanup);

    // Evaluate sentiments in video frame using the skill
    CHECKHR_GOTO(m_spSkill->EvaluateAsync(spSkillBinding.Get(), spOp.ReleaseAndGetAddressOf()), cleanup);
    CHECKHR_GOTO(AwaitAction(spOp), cleanup);

    boolean bIsFaceFound;
    std::cout << "Frame arrived:";
    CHECKHR_GOTO(m_spFaceSentimentSkillBinding->get_IsFaceFound(&bIsFaceFound), cleanup);
    if (bIsFaceFound)
    {
        SentimentType sentiment;
        std::string sentiments[] = {
            "neutral ",
            "happiness ",
            "surprise ",
            "sadness ",
            "anger ",
            "disgust",
            "fear",
            "contempt"
        };
        CHECKHR_GOTO(m_spFaceSentimentSkillBinding->get_PredominantSentiment(&sentiment), cleanup);
        std::string outText = " Your sentiment looks like:  " + sentiments[sentiment] + "\t\t\t\t\r";

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

    // Get an instance of the Windows Media Capture runtime class
    CHECKHR_GOTO(ActivateInstance(HStringReference(RuntimeClass_Windows_Media_Capture_MediaCapture).Get(), &m_spMediaCapture), cleanup);

    // Initialize Media capture with default settings
    CHECKHR_GOTO(m_spMediaCapture->InitializeAsync(&op), cleanup);
    CHECKHR_GOTO(AwaitAction(op), cleanup);

    // QueryInterface to the IMediaCapture5 interface which gives us the ability to create a media frame reader 
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
        spFrameSource->get_Info(&spInfo);
        MediaStreamType streamType;
        MediaFrameSourceKind sourceKind;
        spInfo->get_MediaStreamType(&streamType);
        spInfo->get_SourceKind(&sourceKind);
        std::cout << "FrameSourceType:" << streamType << std::endl;
        while (((streamType != MediaStreamType::MediaStreamType_VideoPreview) && (streamType != MediaStreamType::MediaStreamType_VideoRecord)) || (sourceKind != MediaFrameSourceKind::MediaFrameSourceKind_Color))
        {
            boolean bHasCurrent1;
            CHECKHR_GOTO(spIterator->MoveNext(&bHasCurrent1), cleanup);
            if (!bHasCurrent1)
            {
                continue;
            }
            CHECKHR_GOTO(spIterator->get_Current(spKeyValue.ReleaseAndGetAddressOf()), cleanup);
            CHECKHR_GOTO(spKeyValue->get_Value(spFrameSource.ReleaseAndGetAddressOf()), cleanup);
            spFrameSource->get_Info(spInfo.ReleaseAndGetAddressOf());
            spInfo->get_MediaStreamType(&streamType);
            spInfo->get_SourceKind(&sourceKind);
            std::cout << "FrameSourceType:" << streamType << std::endl;
        }

        // Create frame reader with the FrameSource that we selected in the loop above.
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

        // Finally start the frame reader
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

    // Remove the frame handler and stop frame reader
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
    std::cout << "Face it!\n";

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

    // Initialize media capture and frame reader
    CHECKHR_GOTO(InitCameraAndFrameSource(), cleanup);
    std::cout << "\t\t\t\t\t\t\t\t...press enter to Stop" << std::endl;

    // Wait for enter keypress and let the frame event handler do the work for each frame 
    while (std::cin.get() != '\n');

    std::cout << std::endl << "Key pressed.. exiting";

    // De-initialize the media capture and frame reader
    CHECKHR_GOTO(DeInitCameraAndFrameSource(), cleanup);

cleanup:
    return hr;
}

