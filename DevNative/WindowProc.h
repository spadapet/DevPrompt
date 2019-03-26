#pragma once

class IWindowProc
{
public:
    virtual LRESULT WindowProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam) = 0;

    static HWND Create(IWindowProc* windowProc, WNDCLASSEX& windowClass, DWORD style, RECT pos, HWND parent);

private:
    static LRESULT __stdcall StaticWindowProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam);
};

namespace WindowProc
{
    void ResizeChildren(HWND hwnd);
    void FocusFirstChild(HWND hwnd);
    void IgnorePaint(HWND hwnd);
    void PaintMessage(HWND hwnd, HFONT font, const wchar_t* str);
}
