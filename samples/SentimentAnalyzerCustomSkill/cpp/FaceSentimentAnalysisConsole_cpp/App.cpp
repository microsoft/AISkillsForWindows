// Copyright (c) Microsoft Corporation. All rights reserved.

#include "App.h"

HRESULT App::FrameArrivedHandler(IMediaFrameReader* pFrameReader, IMediaFrameArrivedEventArgs*)
{
    HRESULT hr = S_OK;
    ComPtr<IMediaFrameReference> spFrameRef;
    ComPtr<IVideoMediaFrame> spVideoMediaFrame;
    ComPtr<IVideoFrame> spVideoFrame;
    ComPtr<ISkillBinding> spSkillBinding;
    ComPtr<IAsyncAction> spOp;
    AcquireSRWLockExclusive(&m_lock);
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

    CHECKHR_GOTO(m_spFaceSentimentSkillBinding->SetInputImageAsync(spVideoFrame.Get(), &spOp), cleanup);
    CHECKHR_GOTO(AwaitAction(spOp), cleanup);
    CHECKHR_GOTO(m_spFaceSentimentSkillBinding.As(&spSkillBinding), cleanup);
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
        std::cout << "no face\t\t\t\t\t\t\r";// << std::endl;
    }


cleanup:
    ReleaseSRWLockExclusive(&m_lock);
    if (hr != S_OK)
    {
        std::cout << "Failed:" << std::hex << hr;
    }
    return hr;
}

HRESULT App::initMediaCapture()
{
    HRESULT hr = S_OK;
    ComPtr<IMediaCapture5> spMediaCaptureFS;
    ComPtr<IAsyncAction> op;
    ComPtr<IMapView<HSTRING, MediaFrameSource*>> fsList;
    ComPtr<IIterable<IKeyValuePair<HSTRING, MediaFrameSource*>*>> spIterable;
    ComPtr<IIterator<IKeyValuePair<HSTRING, MediaFrameSource*>*>> spIterator;
    ComPtr<IKeyValuePair<HSTRING, MediaFrameSource*>>             spKeyValue;
    CHECKHR_GOTO(ActivateInstance(HStringReference(RuntimeClass_Windows_Media_Capture_MediaCapture).Get(), &m_spMediaCapture), cleanup);
    CHECKHR_GOTO(m_spMediaCapture->InitializeAsync(&op), cleanup);
    CHECKHR_GOTO(AwaitAction(op), cleanup);

    CHECKHR_GOTO(m_spMediaCapture.As(&spMediaCaptureFS), cleanup);
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

        CHECKHR_GOTO(spMediaCaptureFS->CreateFrameReaderAsync(spFrameSource.Get(), &spOp), cleanup);
        CHECKHR_GOTO(AwaitTypedResult(spOp, MediaFrameReader*, m_spFrameReader), cleanup);

        m_spFrameArrivedHandlerDelegate = 
            Microsoft::WRL::Callback<ITypedEventHandler<MediaFrameReader*, MediaFrameArrivedEventArgs*>>
            ([&](IMediaFrameReader * pFrameReader, IMediaFrameArrivedEventArgs* eventArgs)
                {
                    return FrameArrivedHandler(pFrameReader, eventArgs);
                }
            );

        CHECKHR_GOTO(m_spFrameReader->add_FrameArrived(m_spFrameArrivedHandlerDelegate.Get(), &m_token), cleanup);
        CHECKHR_GOTO(m_spFrameReader->StartAsync(&spOp1), cleanup);
        CHECKHR_GOTO(AwaitTypedResult(spOp1, MediaFrameReaderStartStatus, Stat), cleanup);
    }

cleanup:
    return hr;
}

HRESULT App::deInitMediaCapture()
{
    HRESULT hr = S_OK;
    ComPtr<IAsyncAction> spOp;
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

    RoInitializeWrapper initialize(RO_INIT_MULTITHREADED);
    CHECKHR_GOTO(initialize, cleanup);
    CHECKHR_GOTO(ActivateInstance(HStringReference(RuntimeClass_Contoso_FaceSentimentAnalyzer_FaceSentimentAnalyzerDescriptor).Get(), &spSkillDesc), cleanup);
    CHECKHR_GOTO(spSkillDesc->CreateSkillAsync(&spOp), cleanup);
    CHECKHR_GOTO(Await(spOp, m_spSkill), cleanup);
    {
        CHECKHR_GOTO(m_spSkill->CreateSkillBindingAsync(&spOp1), cleanup);
        CHECKHR_GOTO(Await(spOp1, spSkillBinding), cleanup);
        CHECKHR_GOTO(spSkillBinding.As(&m_spFaceSentimentSkillBinding), cleanup);
    }

    CHECKHR_GOTO(initMediaCapture(), cleanup);
    std::cout << "\t\t\t\t\t\t\t\t...press enter to Stop" << std::endl;
    
    //wait for enter keypress
    while (std::cin.get() != '\n');

    CHECKHR_GOTO(deInitMediaCapture(), cleanup);

cleanup:
    return hr;
}

