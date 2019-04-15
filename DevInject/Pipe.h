#pragma once

#include "Api.h"
#include "Json/Message.h"

// Helper class for sending info back and forth through pipes. When a dispose event
// gets set, then the pipe will stop doing work.
class Pipe
{
public:
    DEV_INJECT_API Pipe();
    DEV_INJECT_API Pipe(Pipe&& rhs);
    DEV_INJECT_API ~Pipe();

    DEV_INJECT_API Pipe& operator=(Pipe&& rhs);
    DEV_INJECT_API operator bool() const;
    DEV_INJECT_API operator HANDLE() const;

    DEV_INJECT_API static Pipe Create(HANDLE clientProcess, HANDLE disposeEvent);
    DEV_INJECT_API static Pipe Connect(HANDLE serverProcess, HANDLE disposeEvent);
    DEV_INJECT_API void Dispose();

    DEV_INJECT_API bool WaitForClient() const;
    DEV_INJECT_API void RunServer(const Json::MessageHandler& handler) const;
    DEV_INJECT_API bool Transact(const Json::Dict& input, Json::Dict& output) const;
    DEV_INJECT_API bool Send(const Json::Dict& input) const;

private:
    Pipe(HANDLE pipe, HANDLE disposeEvent, HANDLE otherProcess);

    std::array<HANDLE, 3> GetWaitHandles(const OVERLAPPED& oio) const;
    bool ReadMessage(Json::Dict& input) const;
    bool WriteMessage(const Json::Dict& output) const;

    HANDLE pipe;
    HANDLE disposeEvent;
    HANDLE otherProcess;
};
