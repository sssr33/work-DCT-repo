#pragma once

#include <initguid.h>
#include <mfapi.h>
#include <mfidl.h>
#include <mfreadwrite.h>
#include <mftransform.h>
#include <mferror.h>
#include <wrl.h>
#include <xaudio2.h>
#include <xaudio2fx.h>
#include "AudioReader.h"
#include <memory>
#include <mutex>
#include <thread>
#include <queue>
#include <condition_variable>
#include "AudioEvents.h"
#include <list>

namespace MediaExtension
{
	struct SourceVoiceDeleter
	{
		void operator()(IXAudio2SourceVoice *obj);
	};

	struct Marker
	{
		Marker() : startPos(0) {}

		void SetMarker(LONGLONG pos, int number, Rational ratio)
		{
			this->startPos = pos;
			this->trackNumber = number;
			this->ratio = ratio;
		}

		void ResetMarker()
		{
			this->activate = false;
		}

		LONGLONG GetMarkerPosition()
		{
			return this->startPos;
		}

		int GetNextTrack()
		{
			return this->trackNumber;
		}

	private:
		Rational ratio;
		LONGLONG startPos;
		uint16 trackNumber;
		bool activate = true;
	};
	
	class XAudio2Player : public IXAudio2VoiceCallback
	{
	public:
		XAudio2Player();
		~XAudio2Player();
		LONGLONG GetCurrentPosition();
		LONGLONG GetDuration();
		void SetVolume(float volume);
		void SetPosition(Rational ratio, double setPosition);
		void SetAudioData(AudioReader *reader, Microsoft::WRL::ComPtr<IXAudio2> xAudio2);
		void SetMarker(LONGLONG pos, int number, Rational ratio);
		void Stop();
		void Initialize(AudioReader *reader, Microsoft::WRL::ComPtr<IXAudio2> xAudio2, std::shared_ptr<AudioEvents> e);

		LONGLONG marker = -0;

	private:
		Microsoft::WRL::ComPtr<IXAudio2> xAudio2;
		std::shared_ptr<IXAudio2SourceVoice> sourceVoice;
		AudioReader *reader = nullptr;
		LONGLONG currentPosition;
		bool notifiedRewinding = false;
		std::condition_variable condVar;
		std::shared_ptr<AudioEvents> events;
		std::mutex samplesMutex;
		std::queue<std::unique_ptr<AudioSample>> samples;
		Marker markeR;
		//std::list<Marker> markersList;
		bool stopped;

		void SubmitBuffer();
		void DeleteSamples();
		bool IfStartPosAchieved();
		void FlushSourceVoice();

		virtual void STDMETHODCALLTYPE OnVoiceProcessingPassStart(UINT32 bytesRequired) override {}
		virtual void STDMETHODCALLTYPE OnVoiceProcessingPassEnd(){}
		virtual void STDMETHODCALLTYPE OnStreamEnd(){}
		virtual void STDMETHODCALLTYPE OnBufferStart(void* pContext){}
		virtual void STDMETHODCALLTYPE OnBufferEnd(void* pContext) override;
		virtual void STDMETHODCALLTYPE OnLoopEnd(void*){}
		virtual void STDMETHODCALLTYPE OnVoiceError(void*, HRESULT){}
	};
}

