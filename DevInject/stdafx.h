#pragma once

#define WIN32_LEAN_AND_MEAN
#include <SDKDDKVer.h>
#include <Windows.h>
#include <Psapi.h>

// C++
#include <array>
#include <cassert>
#include <functional>
#include <mutex>
#include <thread>
#include <sstream>
#include <string>

// Defines
#undef MAX_PATH
#define MAX_PATH 2048
