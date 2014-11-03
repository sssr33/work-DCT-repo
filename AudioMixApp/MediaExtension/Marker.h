#pragma once

#include <windows.h>

struct Marker
{
	Marker() : startPos(0) {}

	void SetMarker(LONGLONG pos, int number)
	{
		this->startPos = pos;
		this->trackNumber = number;
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

	bool activate = true;

private:
	LONGLONG startPos;
	uint16 trackNumber;
};