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
		void CreatePlayList();
		Windows::Storage::Streams::IRandomAccessStream ^GetStream(int trackNumber);
		bool CheckNext(int currentNumber);
		int GetPlayListLength();
	};


	public ref class Reader sealed
	{
	public:
		Reader();
		//void Play(Windows::Foundation::Collections::IVector<int> ^playlist);	transfer C# List in C++
		void Play(IPlayList ^playList);
		void Rewinding(double setPosition);
		void SetMarker(int64 startPos, int trackNum);
		property Windows::Foundation::TimeSpan Duration	{ Windows::Foundation::TimeSpan get(); }
		void Volume(float setVolume);
		LONGLONG CurrPos();
		void Stop();
		void FindGlobalDuration();
		void FindGlobalTrackPosition();

		void EndOfRewindingTrack();
		void EndOfPlayingTrack();
		void IfMarkerMet();

	private:
		std::shared_ptr<XAudio2Player> player;
		Microsoft::WRL::ComPtr<IXAudio2> xAudio2;
		IPlayList ^currentPlayList;
		std::shared_ptr<AudioEvents> events;
		int trackNumber = 0;
		std::list<std::shared_ptr<XAudio2Player>> playersList;

		void SortPlaylist();
	};
}

