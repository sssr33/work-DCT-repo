#pragma once

#include "MFAudioReader.h"
#include "MFAudioSample.h"
#include "XAudio2Player.h"
#include "InitMasterVoice.h"
#include "AudioEvents.h"
#include <list>

namespace MediaExtension
{
	public interface class IPlayList
	{
	public:
		Windows::Storage::Streams::IRandomAccessStream ^GetStream(int trackNumber);
		int GetPlayPosition();	//пока заглушка. возвращает позицию 0, т.е. воспроизведение следующего начнется сразу после предыдущего.
		//если надо начать воспроизводить во время воспроизведения предыдущего то возвращает -pos, если надо после то +pos. currentPlayPos = previousTrackDuration + getPlayPos
		bool CheckNext(int currentNumber);
		int GetPlayListLength();
	};


	public ref class Reader sealed
	{
	public:
		Reader();
		void Play(IPlayList ^playList);
		void Rewinding(double setPosition);

		property Windows::Foundation::TimeSpan Duration
		{
			Windows::Foundation::TimeSpan get();
		}

		void EndOfRewindingTrack();
		void EndOfPlayingTrack();
		void IfMarkerMet();

		void Volume(float setVolume);
		LONGLONG CurrPos();
		void Stop();

	private:
		std::shared_ptr<XAudio2Player> player;	//unique_ptr нельзя копировать т.е. нельзя записать в список, т.к. после операции move() исходный указатель обнуляется
		Microsoft::WRL::ComPtr<IXAudio2> xAudio2;
		IPlayList ^currentPlayList;
		std::shared_ptr<AudioEvents> events;
		int trackNumber = 0;
		std::list<std::shared_ptr<XAudio2Player>> playersList;
	};
}

