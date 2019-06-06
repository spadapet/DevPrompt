#pragma once

class Pipe;

class BaseContext
{
public:
    BaseContext();
    virtual ~BaseContext();

    virtual void Initialize();
    virtual void Dispose();

protected:
    HANDLE OpenOwnerProcess(HANDLE disposeEvent, Pipe& ownerPipe);
};
