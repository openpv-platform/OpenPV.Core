#define _CRT_SECURE_NO_WARNINGS 1

#include "Ahsoka.Services.h"
#include "PlatformInterface.h"

#include <iostream>
#include <string.h>

int EchoValue(int value)
{
	return value;
}

void SetUsbRoleMode(int mode)
{
	SetPlatformUsbRoleMode(mode);
}
