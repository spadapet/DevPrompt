# Dev Prompt
Tabbed command prompts for developers

## Features
* Hosts cmd.exe and powershell.exe command prompts in multiple tabs
* Quickly clone a command prompt
* Save the state of all tabs on restart
* Grab other command prompts into a tab
* Quickly access developer tools and links that you use often

## Project overview
* Open __DevPrompt.sln__ in Visual Studio 2019 on Windows 10
* __DevPrompt.exe__: C# project with UI written in WPF. This is the shell that hosts processes owned by DevNative.dll.
* __DevNative.dll__: C++ project that contains the global app and state of running processes. It uses COM interfaces to communicate with the managed UI in DevPrompt.exe.
* __DevInject.dll__: C++ project that gets injected into every hosted command prompt process. Threads are created to communicate with DevNative.dll through pipes.

## Coding Standards
* Use default formatting for C# and C++ in VS2019 (as if Format Document command was run)
* Use this. or this-> for any members in C# and C++ code
* Don't use "var" in C# or "auto" in C++
