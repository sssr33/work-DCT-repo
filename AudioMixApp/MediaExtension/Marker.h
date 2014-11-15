#pragma once

#include <windows.h>

struct Marker
{
	Marker() : startPos(0) {}

	void SetMarker(LONGLONG pos, int number)
	{
		this->startPos = pos;
		this->markerForTrack = number;
	}

	void ResetMarker()
	{
		this->activate = false;
	}

	LONGLONG GetMarkerPosition()
	{
		return this->startPos;
	}

	int GetTrackIndex()
	{
		return this->markerForTrack;
	}

	bool activate = true;

private:
	LONGLONG startPos;
	uint16 markerForTrack;
};