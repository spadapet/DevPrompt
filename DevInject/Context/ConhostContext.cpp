#include "stdafx.h"
#include "Context/ConhostContext.h"
#include "Main.h"
#include "Pipe.h"
#include "Utility.h"

static HANDLE disposeEvent = nullptr;
static HANDLE ownerProcess = nullptr;
static HANDLE initializeThread = nullptr;
static HWND consoleHwnd = nullptr;
static HWND consoleParentHwnd = nullptr;
static WNDPROC oldWndProc = nullptr;
static std::mutex wndProcMutex;
static std::mutex ownerPipeMutex;
static Pipe ownerPipe;

ConhostContext::ConhostContext()
{
}

ConhostContext::~ConhostContext()
{
}

static void SendToOwner(const Json::Dict& message, std::function<void(const Json::Dict& dict)> handler = nullptr)
{
    std::scoped_lock<std::mutex> lock(::ownerPipeMutex);
    if (::ownerPipe)
    {
        Json::Dict output;
        if (::ownerPipe.Transact(message, output) && handler != nullptr)
        {
            handler(output);
        }
    }
}

static void DetachWindowProc()
{
    std::scoped_lock<std::mutex> lock(::wndProcMutex);

    if (::consoleHwnd)
    {
        ::SetWindowLongPtr(::consoleHwnd, GWLP_WNDPROC, reinterpret_cast<LONG_PTR>(::oldWndProc));
        ::oldWndProc = nullptr;
        ::consoleHwnd = nullptr;
        ::consoleParentHwnd = nullptr;
    }
}

static LRESULT __stdcall ConhostWindowProc(HWND hwnd, UINT msg, WPARAM wp, LPARAM lp)
{
    WNDPROC baseProc = ::oldWndProc;
    HWND parentHwnd = ::consoleParentHwnd;
    if (!baseProc)
    {
        std::scoped_lock<std::mutex> lock(::wndProcMutex);
        baseProc = ::oldWndProc;
        parentHwnd = ::consoleParentHwnd;
    }

    if (parentHwnd)
    {
        // Need to ignore any accelerators that will be processed by MainWindow.xaml
        // Keep in sync with: MainWindow.InputBindings
        enum class MsgHandler { Here, ParentKeyboard, Both } handler = MsgHandler::Here;
        UINT parentMsg = msg;
        WPARAM parentWP = wp;
        LPARAM parentLP = lp;

        switch (msg)
        {
        case WM_CHAR:
            switch (wp)
            {
            case 2: //  Ctrl-B
            case 4: //  Ctrl-D
            case 5: //  Ctrl-E
            case 7: //  Ctrl-G
            case 11: // Ctrl-K
            case 12: // Ctrl-L
            case 14: // Ctrl-N
            case 15: // Ctrl-O
            case 16: // Ctrl-P
            case 17: // Ctrl-Q
            case 18: // Ctrl-R
            case 19: // Ctrl-S
            case 20: // Ctrl-T
            case 21: // Ctrl-U
            case 23: // Ctrl-W
            case 24: // Ctrl-X
            case 25: // Ctrl-Y
            case 26: // Ctrl-Z
                handler = MsgHandler::ParentKeyboard;
                parentMsg = (lp & 0x80000000) ? WM_KEYUP : WM_KEYDOWN;
                parentWP = 'A' + wp - 1;
                break;

            // These only work with the SHIFT key down too
            case 1: // Ctrl-A: Select all
            case 6: // Ctrl-F: Find
            case 22: // Ctrl-V: Paste
                if (::GetKeyState(VK_SHIFT) < 0)
                {
                    handler = MsgHandler::ParentKeyboard;
                }
                break;

            // These can't be sent away
            // case 3:  Ctrl-C: Cancel/Copy
            // case 8:  Ctrl-H: Backspace
            // case 9:  Ctrl-I: Autocomplete
            // case 10: Ctrl-J: ???
            // case 13: Ctrl-M: Mark
            }
            break;

        case WM_KEYDOWN:
        case WM_KEYUP:
            switch (wp)
            {
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            case '0':
            case VK_F4:
            case VK_TAB:
                if (::GetKeyState(VK_CONTROL) < 0)
                {
                    handler = MsgHandler::ParentKeyboard;
                }
                break;

            case VK_APPS:
                handler = MsgHandler::ParentKeyboard;
                break;

            case VK_CONTROL:
                handler = MsgHandler::Both;
                break;
            }
            break;

        case WM_SYSKEYDOWN:
        case WM_SYSKEYUP:
        case WM_SYSCHAR:
            handler = MsgHandler::ParentKeyboard;
            break;

        default:
            if (msg == DevInject::GetDetachMessage())
            {
                ::DetachWindowProc();
                ::SetForegroundWindow(hwnd);
                DevInject::BeginDetach(hwnd);
                return 0;
            }
            else if (msg == DevInject::GetAttachedMessage())
            {
                HWND root = ::GetAncestor(hwnd, GA_ROOT);
                if (::IsIconic(root))
                {
                    ::ShowWindow(root, SW_RESTORE);
                }

                ::SetForegroundWindow(root);
                return 0;
            }
            break;
        }

        if (handler == MsgHandler::ParentKeyboard || handler == MsgHandler::Both)
        {
            ::PostMessage(parentHwnd, parentMsg, parentWP, parentLP);

            if (handler == MsgHandler::ParentKeyboard)
            {
                return 0;
            }
        }
    }

    return ::CallWindowProc(baseProc ? baseProc : ::DefWindowProc, hwnd, msg, wp, lp);
}

static void AttachWindowProc(HWND hwnd, HWND hwndParent)
{
    assert(!::consoleHwnd);

    std::scoped_lock<std::mutex> lock(::wndProcMutex);

    if (!::consoleHwnd)
    {
        WNDPROC newWndProc = ::ConhostWindowProc;
        LONG_PTR oldWndProcLong = ::SetWindowLongPtr(hwnd, GWLP_WNDPROC, reinterpret_cast<LONG_PTR>(newWndProc));
        ::oldWndProc = reinterpret_cast<WNDPROC>(oldWndProcLong);
        ::consoleHwnd = hwnd;
        ::consoleParentHwnd = hwndParent;
    }
}

static unsigned int __stdcall InitializeThread(void*)
{
    ::SendToOwner(Json::CreateMessage(PIPE_COMMAND_CONHOST_INJECTED), [](const Json::Dict& dict)
    {
        HWND hwnd = dict.Get(PIPE_PROPERTY_HWND).TryGetHwndFromString();
        HWND hwndParent = hwnd ? ::GetParent(hwnd) : nullptr;

        if (hwndParent)
        {
            ::AttachWindowProc(hwnd, hwndParent);
            ::PostMessage(hwnd, DevInject::GetAttachedMessage(), 0, 0);
        }
    });

    return 0;
}

void ConhostContext::Initialize()
{
    ::disposeEvent = ::CreateEvent(nullptr, TRUE, FALSE, nullptr);
    ::ownerProcess = this->OpenOwnerProcess(::disposeEvent, ::ownerPipe);

    if (::ownerProcess)
    {
        assert(::ownerPipe);

        ::initializeThread = reinterpret_cast<HANDLE>(::_beginthreadex(nullptr, 0, ::InitializeThread, nullptr, 0, nullptr));
    }
}

void ConhostContext::Dispose()
{
    ::SetEvent(::disposeEvent);

    if (::initializeThread)
    {
        ::WaitForSingleObject(::initializeThread, INFINITE);
        ::CloseHandle(::initializeThread);
        ::initializeThread = nullptr;
    }

    if (::ownerPipe)
    {
        std::scoped_lock<std::mutex> lock(::ownerPipeMutex);
        ::ownerPipe.Dispose();
    }

    if (::ownerProcess)
    {
        ::CloseHandle(::ownerProcess);
        ::ownerProcess = nullptr;
    }

    ::DetachWindowProc();
    ::CloseHandle(::disposeEvent);
    ::disposeEvent = nullptr;
}
