#pragma once

#include "Reader.h"

namespace MediaExtension
{
	class InitMasterVoice
	{
	public:
		static InitMasterVoice &GetInstance();
		InitMasterVoice();
		~InitMasterVoice();

		Microsoft::WRL::ComPtr<IXAudio2> GetXAudio();

	private:
		Microsoft::WRL::ComPtr<IXAudio2> xAudio2;
		IXAudio2MasteringVoice *masterVoice;
	};
}

