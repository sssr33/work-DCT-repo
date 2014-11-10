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

	stream = this->currentPlayList->GetStream(0);
	this->xAudio2 = InitMasterVoice::GetInstance().GetXAudio();

	reader->Initialize(stream);
	std::vector<Marker> markers = this->markersList[0];
	this->player->Initialize(reader, this->xAudio2, this->events, markers);	//create new player and play

	std::lock_guard<std::mutex> lock(this->lockPlayList);
	this->playersList.push_back(this->player);
}

void Reader::Rewinding(double setPosition)
{
	
	for (int i = 0; i < this->playersList.size(); i++)
	{
		if ((this->trackList->GetAt(i)->GetPosition() <= (int)setPosition) && (this->playersList[i]->GetDuration() > (int)setPosition))
			this->playersList[i]->SetPosition(Rational::SEC, setPosition);	//if position inside track duration doing rewinding
		else
			if ((int)setPosition < this->trackList->GetAt(i)->GetPosition())
			{
				this->playersList[i-1]->ResetMarkerIndex();		//if position less than beginning track delete player, set previous marker
				std::lock_guard<std::mutex> lock(this->lockPlayList);
				this->playersList.pop_back();
			}
			else
				if ((int)setPosition >= this->playersList[i]->GetDuration())
				{
					this->playersList[i - 1]->ResetMarkerIndex();
					std::lock_guard<std::mutex> lock(this->lockPlayList);
					this->playersList.pop_back();		//if position greater than track duration delete player
				}

	}
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
	for (int i = 0; i < this->playersList.size(); i++)
	{
		this->playersList[i]->Stop();
	}
	//this->playersList.clear();
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

void Reader::EndOfPlayingTrack(int c)	//begin playing new track in same player
{
	Windows::Storage::Streams::IRandomAccessStream ^stream;
	MFAudioReader *reader = new MFAudioReader();
	int playlistLength = this->currentPlayList->GetPlayListLength();

	if (c < playlistLength - 1)
		if (this->currentPlayList->CheckNext(c - 1))
		{
			stream = this->currentPlayList->GetStream(c);
			reader->Initialize(stream);
			std::vector<Marker> markers = this->markersList[c];
			this->player->SetAudioData(reader, this->xAudio2, markers);
		}
}

void Reader::IfMarkerMet(int i)	// i - is a marker index, so equals index of next song
{
	InitMasterVoice::GetInstance();
	Windows::Storage::Streams::IRandomAccessStream ^stream;
	MFAudioReader *reader = new MFAudioReader();
	auto p = std::shared_ptr<XAudio2Player>(new XAudio2Player());
	this->xAudio2 = InitMasterVoice::GetInstance().GetXAudio();

	stream = this->currentPlayList->GetStream(i);
	reader->Initialize(stream);

	std::vector<Marker> markers = this->markersList[i];
	p->Initialize(reader, this->xAudio2, this->events, markers);		//create new player and play since new position

	if (this->playersList[i-1]->GetCurrentPosition() < p->GetDuration())
	{
		if (this->playersList[i-1]->GetCurrentPosition() > this->trackList->GetAt(i)->GetPosition())	//if immediately rewound it  a new track will be played from the new position, but not with a marker
		{
			std::lock_guard<std::mutex> lock(this->lockPlayList);
			this->playersList.push_back(p);
			this->playersList[i]->SetPosition(Rational::SEC, this->playersList[i-1]->GetCurrentPosition() - this->trackList->GetAt(i)->GetPosition());
		}
		else
		{
			std::lock_guard<std::mutex> lock(this->lockPlayList);
			this->playersList.push_back(p);
		}
	}
	else
	{
		this->playersList[i - 1]->ResetMarkerIndex();
		p->Stop();
	}
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
	Marker marker;
	for (int i = 0; i < this->currentPlayList->GetPlayListLength(); )
	{
		int j = i + 1;
		std::vector<Marker> markers;
		std::vector<Marker> voidMarkers;

		int64_t currentEnd = this->trackList->GetAt(i)->GetPosition() + this->FindSongDurationFromPlayList(i);

		while (this->trackList->GetAt(j)->GetPosition() < currentEnd)
		{
			int posRelPrev = this->trackList->GetAt(j)->GetPosition() - this->trackList->GetAt(i)->GetPosition();
			marker.SetMarker(posRelPrev, i);
			markers.push_back(marker);
			j++;
			if (j >= this->currentPlayList->GetPlayListLength())
				break;	
		}
		this->markersList.push_back(markers);

		while ((j-i-1) > 0)
		{
			this->markersList.push_back(voidMarkers);
			i++;
		}

		i = j;
	}
}

int Reader::GetGlobalDuration()
{
	return this->globalDuration;
}