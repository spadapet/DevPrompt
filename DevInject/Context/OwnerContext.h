#pragma once

#include "Context/BaseContext.h"

// Context of the owner application (aka DevPrompt)
class OwnerContext : public BaseContext
{
public:
    OwnerContext();
    virtual ~OwnerContext() override;
};
