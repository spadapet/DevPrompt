#include "stdafx.h"
#include "App.h"
#include "WindowProc.h"

HWND IWindowProc::Create(IWindowProc* windowProc, WNDCLASSEX& windowClass, DWORD style, RECT pos, HWND parent)
{
    WNDCLASSEX existingWindowClass{};
    if (!::GetClassInfoEx(windowClass.hInstance, windowClass.lpszClassName, &existingWindowClass))
    {
        windowClass.lpfnWndProc = &IWindowProc::StaticWindowProc;
        ::RegisterClassEx(&windowClass);
    }

    return ::CreateWindowEx(
        0, windowClass.lpszClassName, nullptr,
        style, pos.left, pos.top,
        pos.right == CW_USEDEFAULT ? CW_USEDEFAULT : ((pos.left == CW_USEDEFAULT) ? pos.right : pos.right - pos.left),
        pos.bottom == CW_USEDEFAULT ? CW_USEDEFAULT : ((pos.top == CW_USEDEFAULT) ? pos.bottom : pos.bottom - pos.top),
        parent, nullptr, windowClass.hInstance,
        windowProc);
}

LRESULT IWindowProc::StaticWindowProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam)
{
    if (message == WM_CREATE)
    {
        void* userData = reinterpret_cast<CREATESTRUCT*>(lParam)->lpCreateParams;
        ::SetWindowLongPtr(hwnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(userData));
    }

    IWindowProc* window = reinterpret_cast<IWindowProc*>(::GetWindowLongPtr(hwnd, GWLP_USERDATA));

    if (message == WM_NCDESTROY)
    {
        ::SetWindowLongPtr(hwnd, GWLP_USERDATA, 0);
    }

    return window
        ? window->WindowProc(hwnd, message, wParam, lParam)
        : ::DefWindowProc(hwnd, message, wParam, lParam);
}

void WindowProc::ResizeChildren(HWND hwnd)
{
    assert(App::IsMainThread());

    int count = 0;
    for (HWND childHwnd = ::GetWindow(hwnd, GW_CHILD); childHwnd; childHwnd = ::GetWindow(childHwnd, GW_HWNDNEXT))
    {
        count++;
    }

    RECT rect;
    if (count && ::GetClientRect(hwnd, &rect))
    {
        HDWP hdwp = ::BeginDeferWindowPos(count);

        for (HWND childHwnd = ::GetWindow(hwnd, GW_CHILD); childHwnd && hdwp; childHwnd = ::GetWindow(childHwnd, GW_HWNDNEXT))
        {
            hdwp = ::DeferWindowPos(hdwp, childHwnd, nullptr, rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top,
                SWP_NOZORDER | SWP_NOOWNERZORDER | SWP_NOACTIVATE | SWP_NOCOPYBITS);
        }

        if (hdwp)
        {
            ::EndDeferWindowPos(hdwp);
        }
    }
}

void WindowProc::FocusFirstChild(HWND hwnd)
{
    HWND child = ::GetTopWindow(hwnd);
    if (child)
    {
        ::SetFocus(child);
    }
}

void WindowProc::IgnorePaint(HWND hwnd)
{
    PAINTSTRUCT ps;
    ::BeginPaint(hwnd, &ps);
    ::EndPaint(hwnd, &ps);
}

void WindowProc::PaintMessage(HWND hwnd, HFONT font, const wchar_t* str)
{
    PAINTSTRUCT ps;
    HDC dc = ::BeginPaint(hwnd, &ps);
    if (dc)
    {
        HGDIOBJ oldFont = ::SelectObject(dc, font);

        int dpi = static_cast<int>(::GetDpiForWindow(hwnd));
        RECT rect{ 20 * dpi / 96, 20 * dpi / 96, 0, 0 };
        ::DrawText(dc, str, -1, &rect, DT_CALCRECT | DT_SINGLELINE);

        if (ps.rcPaint.right > rect.left && ps.rcPaint.left < rect.right && ps.rcPaint.bottom > rect.top && ps.rcPaint.top < rect.bottom)
        {
            COLORREF oldColor = ::SetTextColor(dc, ::GetSysColor(COLOR_BTNTEXT));
            COLORREF oldBkColor = ::SetBkColor(dc, ::GetSysColor(COLOR_BTNFACE));
            int oldBkMode = ::SetBkMode(dc, OPAQUE);

            ::DrawText(dc, str, -1, &rect, DT_SINGLELINE);

            ::SetBkMode(dc, oldBkMode);
            ::SetBkColor(dc, oldBkColor);
            ::SetTextColor(dc, oldColor);
        }

        ::SelectObject(dc, oldFont);

        ::EndPaint(hwnd, &ps);
    }
}
