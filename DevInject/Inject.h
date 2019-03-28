#pragma once

#include "Api.h"

namespace DevInject
{
    DEV_INJECT_API bool InjectDll(HANDLE process, HANDLE stopEvent, bool allowDifferentBitness);
}
