#pragma once

#include "Context/BaseContext.h"

// Context of cmd.exe or powershell.exe (or some command line app)
class AppContext : public BaseContext
{
public:
    AppContext();
    virtual ~AppContext() override;

    virtual void Initialize() override;
    virtual void Dispose() override;
};
