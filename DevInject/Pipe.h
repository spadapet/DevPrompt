#pragma once

#include "Api.h"
#include "Message.h"

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
    DEV_INJECT_API void RunServer(const MessageHandler& handler) const;
    DEV_INJECT_API bool Transact(const Message& request, Message& response) const;
    DEV_INJECT_API bool Send(const Message& request) const;

private:
    Pipe(HANDLE pipe, HANDLE disposeEvent, HANDLE otherProcess);

    std::array<HANDLE, 3> GetWaitHandles(const OVERLAPPED& oio) const;
    bool ReadMessage(Message& input) const;
    bool WriteMessage(const Message& output) const;

    HANDLE pipe;
    HANDLE disposeEvent;
    HANDLE otherProcess;
};
