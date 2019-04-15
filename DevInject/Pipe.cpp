#include "stdafx.h"
#include "Json/Persist.h"
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

        DWORD pipeServerId;
        if (!::GetNamedPipeServerProcessId(pipe, &pipeServerId) || pipeServerId != ::GetProcessId(serverProcess))
        {
            // Connected to wrong server
            assert(false);
            ::CloseHandle(pipe);
            pipe = INVALID_HANDLE_VALUE;
        }
    }

    if (pipe && pipe != INVALID_HANDLE_VALUE)
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

bool Pipe::ReadMessage(Json::Dict& input) const
{
    HANDLE oioEvent = ::CreateEvent(nullptr, TRUE, FALSE, nullptr);
    std::vector<BYTE> buffer;
    size_t readBufferSize = 0;
    bool done = false;

    while (!done)
    {
        buffer.resize(readBufferSize + ::PIPE_BUFFER_SIZE);

        bool moreData = false;
        DWORD bytesRead = 0;
        OVERLAPPED oio{};
        oio.hEvent = oioEvent;

        if (::ReadFile(this->pipe, buffer.data() + readBufferSize, ::PIPE_BUFFER_SIZE, nullptr, &oio) || ::GetLastError() == ERROR_MORE_DATA)
        {
            if (::GetOverlappedResult(this->pipe, &oio, &bytesRead, TRUE))
            {
                readBufferSize += bytesRead;
                done = true;
            }
            else if (::GetLastError() == ERROR_MORE_DATA)
            {
                readBufferSize += bytesRead;
                moreData = true;
            }
        }
        else if (::GetLastError() == ERROR_IO_PENDING)
        {
            auto handles = this->GetWaitHandles(oio);
            if (::WaitForMultipleObjects(static_cast<DWORD>(handles.size()), handles.data(), FALSE, INFINITE) == WAIT_OBJECT_0)
            {
                if (::GetOverlappedResult(this->pipe, &oio, &bytesRead, TRUE))
                {
                    readBufferSize += bytesRead;
                    done = true;
                }
                else if (::GetLastError() == ERROR_MORE_DATA)
                {
                    readBufferSize += bytesRead;
                    moreData = true;
                }
            }
        }

        if (!done && !moreData)
        {
            break;
        }
    }

    if (done)
    {
        input = Json::Parse(reinterpret_cast<const wchar_t*>(buffer.data()), readBufferSize - sizeof(wchar_t));
    }

    ::CloseHandle(oioEvent);

    return done;
}

bool Pipe::WriteMessage(const Json::Dict& output) const
{
    bool status = false;
    std::wstring buffer = Json::Write(output);
    DWORD byteSize = static_cast<DWORD>((buffer.size() + 1) * sizeof(wchar_t));
    OVERLAPPED oio{};
    oio.hEvent = ::CreateEvent(nullptr, TRUE, FALSE, nullptr);

    if (::WriteFile(this->pipe, buffer.c_str(), byteSize, nullptr, &oio))
    {
        status = true;
    }
    else if (::GetLastError() == ERROR_IO_PENDING)
    {
        auto handles = this->GetWaitHandles(oio);
        status = (::WaitForMultipleObjects(static_cast<DWORD>(handles.size()), handles.data(), FALSE, INFINITE) == WAIT_OBJECT_0);
    }

    if (status)
    {
        DWORD bytesWritten = 0;
        status = (::GetOverlappedResult(this->pipe, &oio, &bytesWritten, TRUE) != FALSE);
        assert(!status || bytesWritten == byteSize);
    }

    ::CloseHandle(oio.hEvent);

    return status;
}

void Pipe::RunServer(const Json::MessageHandler& handler) const
{
    for (bool status = (this->pipe != nullptr); status; )
    {
        Json::Dict input;
        if ((status = this->ReadMessage(input)) != false)
        {
            Json::Dict output = handler(input);
            output.Set(PIPE_PROPERTY_ID, input.Get(PIPE_PROPERTY_ID));
            output.Set(PIPE_PROPERTY_COMMAND, input.Get(PIPE_PROPERTY_COMMAND));

            status = this->WriteMessage(output);
        }
    }

    if (this->pipe)
    {
        ::CancelIo(this->pipe);
    }
}

bool Pipe::Transact(const Json::Dict& input, Json::Dict& output) const
{
    Json::Dict inputCopy = input;
    if (inputCopy.Get(PIPE_PROPERTY_ID).IsUnset())
    {
        static long TRANSACTION_ID = 0;
        int id = ::InterlockedIncrement(&TRANSACTION_ID);
        inputCopy.Set(PIPE_PROPERTY_ID, Json::Value(id));
    }

    if (this->WriteMessage(inputCopy) && this->ReadMessage(output))
    {
        assert(inputCopy.Get(PIPE_PROPERTY_ID) == output.Get(PIPE_PROPERTY_ID));
        assert(inputCopy.Get(PIPE_PROPERTY_COMMAND) == output.Get(PIPE_PROPERTY_COMMAND));
        return true;
    }

    return false;
}

bool Pipe::Send(const Json::Dict& input) const
{
    Json::Dict response;
    return this->Transact(input, response);
}

std::array<HANDLE, 3> Pipe::GetWaitHandles(const OVERLAPPED& oio) const
{
    return std::array<HANDLE, 3>
    {
        oio.hEvent, this->disposeEvent, this->otherProcess,
    };
}
