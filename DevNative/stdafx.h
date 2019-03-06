#pragma once

// Windows
#define WIN32_LEAN_AND_MEAN
#include <SDKDDKVer.h>
#include <windows.h>
#include <wrl.h>
#include <Psapi.h>
#include <TlHelp32.h>

// C++
#include <array>
#include <atomic>
#include <cassert>
#include <chrono>
#include <functional>
#include <memory>
#include <mutex>
#include <sstream>
#include <string>
#include <thread>

// Defines
#undef MAX_PATH
#define MAX_PATH 2048

// Custom system commands from conhost.exe
#define SC_CONHOST_PROPERTIES 0xFFF7
#define SC_CONHOST_DEFAULTS 0xFFF8
