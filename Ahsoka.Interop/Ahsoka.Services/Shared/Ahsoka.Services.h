#pragma once

#if defined(_WIN32) || defined(_WIN64)
#ifdef AHSOKASERVICESWINDOWS_EXPORTS
#define API __declspec(dllexport)
#else
#define API __declspec(dllimport)
#endif
#else
#define API
#endif

#include <string>
#include <vector>

extern "C" API int EchoValue(int value);

// Input: 0 = Host, 1 = Device
extern "C" API void SetUsbRoleMode(int mode);