#pragma once

#include "Api.h"

namespace DevInject
{
    static void Initialize(HMODULE module);
    HMODULE Dispose();
    HMODULE GetModule();
}
