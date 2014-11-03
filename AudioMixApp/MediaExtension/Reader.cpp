#include "pch.h"
#include "Reader.h"
#include <mfapi.h>
#include <mfidl.h>
#include <mfreadwrite.h>
#include <mftransform.h>
#include <mferror.h>
#include <wrl.h>
#include <vector>
#include <xaudio2.h>
#include <xaudio2fx.h>
#include <propvarutil.h>
#include <collection.h>
#include "AudioReader.h"
#include "MFAudioReader.h"
#include "MFAudioEvents.h"

using namespace MediaExtension;
using namespace Platform;

Reader::Reader()
{
	this->player = std::shared_ptr<XAudio2Player>(new XAudio2Player());
}

void Reader::Play(IPlayList ^playList)
{
	InitMasterVoice::GetInstance();
	Windows::Storage::Streams::IRandomAccessStream ^stream;
	MFAudioReader *reader = new MFAudioReader();
	this->currentPlayList = playList;

	this->trackList = this->currentPlayList->CreatePlayList();
	this->SortPlaylist();
	this->FindGlobalDuration();
	this->FindMarkers();

	{
		MFAudioEvents *tmppEvents = new MFAudioEvents();
		tmppEvents->InitEvent(this);
		this->events = std::shared_ptr<AudioEvents>(tmppEvents);
	}	
	
	stream = this->currentPlayList->GetStream(trackNumber);
	this->xAudio2 = InitMasterVoice::GetInstance().GetXAudio();

	reader->Initialize(stream);
	std::vector<Marker> markers = this->markersList[trackNumber];
	this->player->Initialize(reader, this->xAudio2, this->events, markers);	//create new player and play	//send marker in player

	this->playersList.push_back(this->player);
}

void Reader::Rewinding(double setPosition)
{
	this->player->SetPosition(Rational::SEC, setPosition);
}

void Reader::SetMarker(int64 startPos, int trackNum)
{

}

Windows::Foundation::TimeSpan Reader::Duration::get()
{
	Windows::Foundation::TimeSpan duration;
	duration.Duration = this->player->GetDuration();
	return duration;
}

void Reader::Volume(float setVolume)
{
	this->player->SetVolume(setVolume);
}

LONGLONG Reader::CurrPos()
{
	return this->player->GetCurrentPosition();
}

void Reader::Stop()
{
	this->player->Stop();
}

void Reader::FindGlobalDuration()
{
	this->globalDuration += (this->trackList->GetAt(0)->GetPosition() + this->FindSongDurationFromPlayList(0));

	for (int i = 1; i < this->currentPlayList->GetPlayListLength(); i++)
	{
		int64_t tmp = this->trackList->GetAt(i)->GetPosition() + this->FindSongDurationFromPlayList(i) - this->globalDuration;
		if (tmp > 0)
			this->globalDuration += tmp;
	}
}

void Reader::EndOfRewindingTrack()
{

}

void Reader::EndOfPlayingTrack()	//begin playing new track in same player
{
	Windows::Storage::Streams::IRandomAccessStream ^stream;
	MFAudioReader *reader = new MFAudioReader();
	int playlistLength = this->currentPlayList->GetPlayListLength();

	if (this->currentPlayList->CheckNext(this->trackNumber) && this->trackNumber<playlistLength)
	{
		stream = this->currentPlayList->GetStream(++this->trackNumber);
		reader->Initialize(stream);
		this->player->SetAudioData(reader, this->xAudio2);
	}
}

void Reader::IfMarkerMet()
{
	InitMasterVoice::GetInstance();
	Windows::Storage::Streams::IRandomAccessStream ^stream;
	MFAudioReader *reader = new MFAudioReader();
	auto p = std::shared_ptr<XAudio2Player>(new XAudio2Player());
	this->xAudio2 = InitMasterVoice::GetInstance().GetXAudio();
	stream = this->currentPlayList->GetStream(++this->trackNumber);

	reader->Initialize(stream);
	std::vector<Marker> markers = this->markersList[trackNumber-1];
	p->Initialize(reader, this->xAudio2, this->events, markers);		//create new player and play since new position	//send marker in player

	this->playersList.push_back(p);
}

//sorting playlist by global song's positions in playlist
void Reader::SortPlaylist()
{
	std::sort(begin(this->trackList), end(this->trackList), compare());
}

int64_t Reader::FindSongDurationFromPlayList(int numSong)
{
	Windows::Storage::Streams::IRandomAccessStream ^stream;
	MFAudioReader *reader = new MFAudioReader();
	stream = this->currentPlayList->GetStream(numSong);
	reader->Initialize(stream);
	Int64Rational songDuration = reader->GetAudioDuration();
	int64_t convertSongDuration = songDuration.Convert(Rational::HNS).value;
	return convertSongDuration;
}

// i - song index, j - marker in song
void Reader::FindMarkers()
{
	Marker m;
	for (int i = 0; i < this->currentPlayList->GetPlayListLength(); )
	{
		int j = i + 1;
		std::vector<Marker> marker;
		int64_t currentEnd = this->trackList->GetAt(i)->GetPosition() + this->FindSongDurationFromPlayList(i);
		while (this->trackList->GetAt(j)->GetPosition() < currentEnd)
		{
			int posRelPrev = this->trackList->GetAt(j)->GetPosition() - this->trackList->GetAt(i)->GetPosition();
			m.SetMarker(posRelPrev, j);
			marker.push_back(m);
			j++;
			if (j >= this->currentPlayList->GetPlayListLength())
				break;
		}
		this->markersList.push_back(marker);
		i = j;
	}
}