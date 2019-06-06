#pragma once

#include "Context/BaseContext.h"

// Context of conhost.exe
class ConhostContext : public BaseContext
{
public:
    ConhostContext();
    virtual ~ConhostContext() override;

    virtual void Initialize() override;
    virtual void Dispose() override;
};
