// FaceSentimentAnalysis_cpp.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <string>
#include <Windows.Foundation.h>
#include <windows.foundation.collections.h>
#include <wrl\implements.h>
#include <wrl\wrappers\corewrappers.h>
#include <wrl/client.h>
#include <wrl/event.h>
#include <windows.system.threading.h>

#include <windows.media.h>
#include <windows.media.capture.h>
#include <windows.media.capture.frames.h>


#include "Contoso.FaceSentimentAnalyzer.h"
using namespace ABI::Windows::Foundation;
using namespace ABI::Windows::Foundation::Collections;
using namespace Microsoft::WRL;
using namespace Microsoft::WRL::Wrappers;
using namespace ABI::Contoso::FaceSentimentAnalyzer;
using namespace ABI::Microsoft::AI::Skills::SkillInterfacePreview;
using namespace ABI::Windows::System::Threading;
using namespace ABI::Windows::Media;
using namespace ABI::Windows::Media::Capture;
using namespace ABI::Windows::Media::Capture::Frames;

#define AwaitTypedResult(op,type,result) [&op, &result]() -> HRESULT                              \
{                                                                                                                               \
    HRESULT hr;                                                                                                                 \
    Event threadCompleted(CreateEventEx(nullptr, nullptr, CREATE_EVENT_MANUAL_RESET, WRITE_OWNER | EVENT_ALL_ACCESS));          \
    ComPtr<IAsyncOperationCompletedHandler<type>> cb                                                       \
    = Callback<Implements<RuntimeClassFlags<ClassicCom>, IAsyncOperationCompletedHandler<type>, FtmBase>>( \
        [&threadCompleted](IAsyncOperation<type>* asyncOperation, AsyncStatus status)->HRESULT                                   \
    {                                                                                                                           \
    SetEvent(threadCompleted.Get());                                                                                            \
    return S_OK;                                                                                                                \
    });                                                                                                                         \
    op->put_Completed(cb.Get());                                                                                             \
    WaitForSingleObject(threadCompleted.Get(), INFINITE);                                                                       \
    hr = op->GetResults(&result);                                                                                         \
    return hr;                                                                                                                  \
} ();

#define Await(op,result) [&op, &result]() -> HRESULT                              \
{                                                                                                                               \
    HRESULT hr;                                                                                                                 \
    Event threadCompleted(CreateEventEx(nullptr, nullptr, CREATE_EVENT_MANUAL_RESET, WRITE_OWNER | EVENT_ALL_ACCESS));          \
    ComPtr<IAsyncOperationCompletedHandler<decltype(result.Get())>> cb                                                       \
    = Callback<Implements<RuntimeClassFlags<ClassicCom>, IAsyncOperationCompletedHandler<decltype(result.Get())>, FtmBase>>( \
        [&threadCompleted](IAsyncOperation<decltype(result.Get())>* asyncOperation, AsyncStatus status)->HRESULT                                   \
    {                                                                                                                           \
        SetEvent(threadCompleted.Get());                                                                                            \
        return S_OK;                                                                                                                \
    });                                                                                                                         \
    op->put_Completed(cb.Get());                                                                                             \
    WaitForSingleObject(threadCompleted.Get(), INFINITE);                                                                       \
    hr = op->GetResults(&result);                                                                                         \
    return hr;                                                                                                                  \
} ();


#define AwaitAction(op) [&op]() -> HRESULT                              \
{                                                                                                                               \
    HRESULT hr;                                                                                                                 \
    Event threadCompleted(CreateEventEx(nullptr, nullptr, CREATE_EVENT_MANUAL_RESET, WRITE_OWNER | EVENT_ALL_ACCESS));          \
    ComPtr<IAsyncActionCompletedHandler> cb                                                       \
    = Callback<Implements<RuntimeClassFlags<ClassicCom>, IAsyncActionCompletedHandler, FtmBase>>( \
        [&threadCompleted](decltype(op.Get()) asyncAction, AsyncStatus status)->HRESULT                                   \
    {                                                                                                                           \
        SetEvent(threadCompleted.Get());                                                                                            \
        return S_OK;                                                                                                                \
    });                                                                                                                         \
    op->put_Completed(cb.Get());                                                                                             \
    WaitForSingleObject(threadCompleted.Get(), INFINITE);                                                                       \
    hr = op->GetResults();                                                                                         \
    return hr;                                                                                                                  \
} ();

#define CHECKHR_GOTO( _hr, _lbl ) { hr = _hr; if( FAILED( hr ) ){ std::cout << std::endl << "Error on line=" << __LINE__ << "hr: " << std::hex << hr; goto _lbl; } }


ComPtr<IMediaCapture> spMediaCapture;
ComPtr<FaceSentimentAnalyzerBinding2> spFaceSentimentSkillBinding;
ComPtr<ISkill> g_spSkill;
EventRegistrationToken token;
SRWLOCK lock;
ComPtr<IMediaFrameReader> g_spFrameReader;

auto frcb = Microsoft::WRL::Callback<ITypedEventHandler<MediaFrameReader*, MediaFrameArrivedEventArgs*>>
([](IMediaFrameReader* pFrameReader, IMediaFrameArrivedEventArgs*)
    {
        HRESULT hr;
        ComPtr<IMediaFrameReference> spFrameRef;
        ComPtr<IVideoMediaFrame> spVideoMediaFrame;
        ComPtr<IVideoFrame> spVideoFrame;
        ComPtr<ISkillBinding> spSkillBinding;
        ComPtr<IAsyncAction> spOp;
        AcquireSRWLockExclusive(&lock);
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
        
        CHECKHR_GOTO(spFaceSentimentSkillBinding->SetInputImageAsync(spVideoFrame.Get(),&spOp), cleanup);
        CHECKHR_GOTO(AwaitAction(spOp), cleanup);
        CHECKHR_GOTO(spFaceSentimentSkillBinding.As(&spSkillBinding), cleanup);
        CHECKHR_GOTO(g_spSkill->EvaluateAsync(spSkillBinding.Get(), spOp.ReleaseAndGetAddressOf()), cleanup);
        CHECKHR_GOTO(AwaitAction(spOp), cleanup);
        boolean bIsFaceFound;
        std::cout << "Frame arrived:";
        CHECKHR_GOTO(spFaceSentimentSkillBinding->get_IsFaceFound(&bIsFaceFound), cleanup);
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
            CHECKHR_GOTO(spFaceSentimentSkillBinding->get_PredominantSentiment(&sentiment), cleanup);
            std::string outText = " You look " + sentiments[sentiment]+ "                          \r";

            std::cout << outText;
        }
        else
        {
            std::cout << "no face                                    \r";// << std::endl;
        }

        //std::cout << "Frame arrived";
        
    cleanup:
        ReleaseSRWLockExclusive(&lock);
        if (hr != S_OK)
        {
            std::cout << "Failed:" << std::hex << hr;
        }
        return hr;
    });
HRESULT initMediaCapture()
{

    ComPtr<IMediaCapture5> spMediaCaptureFS;
    HRESULT hr;
    ComPtr<IAsyncAction> op;
    ComPtr<IMapView<HSTRING,MediaFrameSource*>> fsList;
    ComPtr<IIterable<IKeyValuePair<HSTRING, MediaFrameSource*>*>> spIterable;
    ComPtr<IIterator<IKeyValuePair<HSTRING, MediaFrameSource*>*>> spIterator;
    ComPtr<IKeyValuePair<HSTRING, MediaFrameSource*>>             spKeyValue;
    CHECKHR_GOTO(ActivateInstance(HStringReference(RuntimeClass_Windows_Media_Capture_MediaCapture).Get(), &spMediaCapture), cleanup);
    CHECKHR_GOTO(spMediaCapture->InitializeAsync(&op), cleanup);
    CHECKHR_GOTO(AwaitAction(op), cleanup);
    
    CHECKHR_GOTO(spMediaCapture.As(&spMediaCaptureFS), cleanup);
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
        ComPtr<IMediaFrameReader> spFrameReader;
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
        CHECKHR_GOTO(AwaitTypedResult(spOp,MediaFrameReader*, spFrameReader), cleanup);
        //
        CHECKHR_GOTO(spFrameReader->add_FrameArrived(frcb.Get(), &token), cleanup);
        CHECKHR_GOTO(spFrameReader->StartAsync(&spOp1), cleanup);
        CHECKHR_GOTO(AwaitTypedResult(spOp1, MediaFrameReaderStartStatus, Stat), cleanup);

        g_spFrameReader = spFrameReader;
    }

cleanup:
    return hr;
}

int main()
{
    //int line;
    RoInitializeWrapper initialize(RO_INIT_MULTITHREADED);
    if (FAILED(initialize))
    {
        std::cout << __LINE__  <<  std::hex << initialize;
        return HRESULT(initialize);
    }
    std::cout << "Hello World!\n"; 
    ComPtr<ISkillDescriptor> spSkillDesc;
    ComPtr<IAsyncOperation<ISkill*> > spOp;
    ComPtr<ISkillBinding> spSkillBinding;
    ComPtr<IActivationFactory> spFactory;
    ComPtr<ISkill> spSkill;
    ComPtr<IAsyncOperation<ISkillBinding*>> spOp1;
    HRESULT hr;
    CHECKHR_GOTO(ActivateInstance(HStringReference(RuntimeClass_Contoso_FaceSentimentAnalyzer_FaceSentimentAnalyzerDescriptor).Get(), &spSkillDesc), cleanup);
    CHECKHR_GOTO(spSkillDesc->CreateSkillAsync(&spOp), cleanup);
    CHECKHR_GOTO(Await(spOp, spSkill), cleanup);
    {
        CHECKHR_GOTO(spSkill->CreateSkillBindingAsync(&spOp1), cleanup);
        CHECKHR_GOTO(Await(spOp1, spSkillBinding), cleanup);
        CHECKHR_GOTO(spSkillBinding.As(&spFaceSentimentSkillBinding), cleanup);
    }
    g_spSkill = spSkill;
    CHECKHR_GOTO(initMediaCapture(), cleanup);
    int i;
    std::cout << "press any key and enter to Stop";
    std::cin >> i;
    
cleanup:
    //if (hr != S_OK)
    //{
    //    
    //    //std::cout << std::hex << hr << "Line num:" << line;
    //    return hr;
    //}
    return 0;
}

// Run program: Ctrl + F5 or Debug > Start Without Debugging menu
// Debug program: F5 or Debug > Start Debugging menu

// Tips for Getting Started: 
//   1. Use the Solution Explorer window to add/manage files
//   2. Use the Team Explorer window to connect to source control
//   3. Use the Output window to see build output and other messages
//   4. Use the Error List window to view errors
//   5. Go to Project > Add New Item to create new code files, or Project > Add Existing Item to add existing code files to the project
//   6. In the future, to open this project again, go to File > Open > Project and select the .sln file
