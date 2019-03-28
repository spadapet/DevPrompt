#include "stdafx.h"
#include "Pipe.h"

static const DWORD PIPE_BUFFER_SIZE = 65536;

static std::wstring GetPipeName(HANDLE serverProcess, HANDLE clientProcess)
{
    std::wstringstream pipeName;
    pipeName << L"\\\\.\\pipe\\DevPrompt.{D770A8BC-A238-4D90-B906-840EFF3918DA}.";
    pipeName << ::GetProcessId(serverProcess) << L".";
    pipeName << ::GetProcessId(clientProcess);

    return pipeName.str();
}

Pipe::Pipe()
    : Pipe(nullptr, nullptr, nullptr)
{
}

Pipe::Pipe(HANDLE pipe, HANDLE disposeEvent, HANDLE otherProcess)
    : pipe(pipe)
    , disposeEvent(disposeEvent)
    , otherProcess(otherProcess)
{
}

Pipe::Pipe(Pipe&& rhs)
    : Pipe(rhs.pipe, rhs.disposeEvent, rhs.otherProcess)
{
    rhs.pipe = nullptr;
}

Pipe::~Pipe()
{
    this->Dispose();
}

Pipe& Pipe::operator=(Pipe&& rhs)
{
    if (this != &rhs)
    {
        this->Dispose();

        this->pipe = rhs.pipe;
        this->disposeEvent = rhs.disposeEvent;
        this->otherProcess = rhs.otherProcess;

        rhs.pipe = nullptr;
    }

    return *this;
}

Pipe::operator bool() const
{
    return this->pipe != nullptr;
}

Pipe::operator HANDLE() const
{
    return this->pipe;
}

Pipe Pipe::Create(HANDLE clientProcess, HANDLE disposeEvent)
{
    SECURITY_DESCRIPTOR sd;
    ::InitializeSecurityDescriptor(&sd, SECURITY_DESCRIPTOR_REVISION);
    ::SetSecurityDescriptorDacl(&sd, TRUE, nullptr, FALSE);

    SECURITY_ATTRIBUTES sa;
    sa.nLength = sizeof(sa);
    sa.lpSecurityDescriptor = &sd;
    sa.bInheritHandle = FALSE;

    HANDLE pipe = ::CreateNamedPipe(::GetPipeName(::GetCurrentProcess(), clientProcess).c_str(),
        PIPE_ACCESS_DUPLEX | FILE_FLAG_OVERLAPPED | FILE_FLAG_FIRST_PIPE_INSTANCE,
        PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_REJECT_REMOTE_CLIENTS,
        1, ::PIPE_BUFFER_SIZE, ::PIPE_BUFFER_SIZE, 0, &sa);

    return Pipe(pipe, disposeEvent, clientProcess);
}

Pipe Pipe::Connect(HANDLE serverProcess, HANDLE disposeEvent)
{
    HANDLE pipe = ::CreateFile(::GetPipeName(serverProcess, ::GetCurrentProcess()).c_str(),
        GENERIC_READ | GENERIC_WRITE, 0, nullptr, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, nullptr);

    if (pipe && pipe != INVALID_HANDLE_VALUE)
    {
        DWORD mode = PIPE_READMODE_MESSAGE;
        ::SetNamedPipeHandleState(pipe, &mode, nullptr, nullptr);
    }

    if (pipe != INVALID_HANDLE_VALUE)
    {
        DWORD pipeServerId;
        if (!::GetNamedPipeServerProcessId(pipe, &pipeServerId) || pipeServerId != ::GetProcessId(serverProcess))
        {
            // Connected to wrong server
            assert(false);
            ::CloseHandle(pipe);
            pipe = INVALID_HANDLE_VALUE;
        }
    }

    if (pipe != INVALID_HANDLE_VALUE)
    {
        return Pipe(pipe, disposeEvent, serverProcess);
    }

    return Pipe();
}

void Pipe::Dispose()
{
    if (this->pipe)
    {
        ::CloseHandle(this->pipe);
        this->pipe = nullptr;
    }
}

bool Pipe::WaitForClient() const
{
    bool status = false;

    OVERLAPPED oio{};
    oio.hEvent = ::CreateEvent(nullptr, TRUE, FALSE, nullptr);

    if (this->pipe)
    {
        if (::ConnectNamedPipe(this->pipe, &oio) || ::GetLastError() == ERROR_PIPE_CONNECTED)
        {
            status = true;
        }
        else if (::GetLastError() == ERROR_IO_PENDING)
        {
            auto handles = this->GetWaitHandles(oio);
            if (::WaitForMultipleObjects(static_cast<DWORD>(handles.size()), handles.data(), FALSE, INFINITE) == WAIT_OBJECT_0)
            {
                DWORD result = 0;
                status = (::GetOverlappedResult(this->pipe, &oio, &result, TRUE) != FALSE);
            }
        }

        DWORD pipeClientId;
        if (status && (!::GetNamedPipeClientProcessId(this->pipe, &pipeClientId) || pipeClientId != ::GetProcessId(this->otherProcess)))
        {
            // Bad client connected
            assert(false);
            status = false;
        }
    }
    else
    {
        // No client will ever connect, so just wait until shutdown
        auto handles = this->GetWaitHandles(oio);
        ::WaitForMultipleObjects(static_cast<DWORD>(handles.size()), handles.data(), FALSE, INFINITE);
    }

    ::CloseHandle(oio.hEvent);

    return status;
}

bool Pipe::ReadMessage(Message & input) const
{
    OVERLAPPED oio{};
    oio.hEvent = ::CreateEvent(nullptr, TRUE, FALSE, nullptr);

    std::vector<BYTE> buffer;
    size_t readBufferSize = 0;
    bool status = false;

    while (!status)
    {
        buffer.resize(readBufferSize + ::PIPE_BUFFER_SIZE);

        DWORD bytesRead = 0;
        BYTE * mem = buffer.data() + readBufferSize;

        if ((status = ::ReadFile(this->pipe, mem, ::PIPE_BUFFER_SIZE, &bytesRead, &oio)) || ::GetLastError() == ERROR_MORE_DATA)
        {
            // Sync read is done
            readBufferSize += bytesRead;
        }
        else if (::GetLastError() == ERROR_IO_PENDING)
        {
            // Wait for reading to finish

            auto handles = this->GetWaitHandles(oio);
            if (::WaitForMultipleObjects(static_cast<DWORD>(handles.size()), handles.data(), FALSE, INFINITE) == WAIT_OBJECT_0)
            {
                // Async read is done
                DWORD bytesRead = 0;
                if ((status = ::GetOverlappedResult(this->pipe, &oio, &bytesRead, TRUE)) || ::GetLastError() == ERROR_MORE_DATA)
                {
                    readBufferSize += bytesRead;
                    continue;
                }
            }

            // Error
            break;
        }
    }

    if (status)
    {
        input = Message::Parse(buffer.data(), readBufferSize);
    }

    ::CloseHandle(oio.hEvent);

    return status;
}

bool Pipe::WriteMessage(const Message & output) const
{
    bool status = false;
    std::vector<BYTE> buffer = output.Convert();

    DWORD bytesWritten = 0;
    OVERLAPPED oio{};
    oio.hEvent = ::CreateEvent(nullptr, TRUE, FALSE, nullptr);

    if (::WriteFile(this->pipe, buffer.data(), static_cast<DWORD>(buffer.size()), &bytesWritten, &oio))
    {
        // Finished immediately
        status = true;
    }
    else if (::GetLastError() == ERROR_IO_PENDING)
    {
        // Handle the results of any completed read/write operations

        auto handles = this->GetWaitHandles(oio);
        if (::WaitForMultipleObjects(static_cast<DWORD>(handles.size()), handles.data(), FALSE, INFINITE) == WAIT_OBJECT_0)
        {
            // Async write is done
            status = (::GetOverlappedResult(this->pipe, &oio, &bytesWritten, TRUE) != FALSE);
        }
    }

    ::CloseHandle(oio.hEvent);

    assert(!status || bytesWritten == buffer.size());
    return status;
}

void Pipe::RunServer(const MessageHandler & handler) const
{
    for (bool status = (this->pipe != nullptr); status; )
    {
        Message request;
        status = this->ReadMessage(request) && this->WriteMessage(handler(request));
    }

    if (this->pipe)
    {
        ::CancelIo(this->pipe);
    }
}

bool Pipe::Transact(const Message & request, Message & response) const
{
    return this->WriteMessage(request) && this->ReadMessage(response);
}

bool Pipe::Send(const Message & request) const
{
    Message response;
    return this->Transact(request, response);
}

std::array<HANDLE, 3> Pipe::GetWaitHandles(const OVERLAPPED & oio) const
{
    return std::array<HANDLE, 3>
    {
        oio.hEvent, this->disposeEvent, this->otherProcess,
    };
}
