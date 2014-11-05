#pragma once

#include "MFAudioReader.h"
#include "MFAudioSample.h"
#include "XAudio2Player.h"
#include "InitMasterVoice.h"
#include "Marker.h"
#include <list>

namespace MediaExtension
{
	public interface class ITrack
	{
	public:
		int GetPosition();
		Platform::String ^GetName();
	};

	public interface class IPlayList
	{
	public:
		Windows::Foundation::Collections::IVector<ITrack^> ^CreatePlayList();
		Windows::Storage::Streams::IRandomAccessStream ^GetStream(int trackNumber);
		bool CheckNext(int currentNumber);
		int GetPlayListLength();
	};

	public ref class Reader sealed
	{

		struct compare
		{
			bool operator() (ITrack ^song1, ITrack ^song2) const
			{
				return song1->GetPosition() < song2->GetPosition();
			}
		};

	public:
		Reader();
		void Play(IPlayList ^playList);
		void Rewinding(double setPosition);
		void SetMarker(int64 startPos, int trackNum);
		property Windows::Foundation::TimeSpan Duration	{ Windows::Foundation::TimeSpan get(); }
		void Volume(float setVolume);
		LONGLONG CurrPos();
		void Stop();
		void FindGlobalDuration();
		int GetGlobalDuration();

		void EndOfRewindingTrack();
		void EndOfPlayingTrack();
		void IfMarkerMet(int i);

	private:
		std::shared_ptr<XAudio2Player> player;
		Microsoft::WRL::ComPtr<IXAudio2> xAudio2;
		IPlayList ^currentPlayList;
		Windows::Foundation::Collections::IVector<ITrack^> ^trackList;
		std::shared_ptr<AudioEvents> events;
		int trackNumber = 0;
		std::vector<std::shared_ptr<XAudio2Player>> playersList;
		int64_t globalDuration = 0;
		std::vector<std::vector<Marker>> markersList;
		std::mutex lockPlayList;

		void SortPlaylist();
		int64_t FindSongDurationFromPlayList(int numSong);
		void FindMarkers();
	};
}

