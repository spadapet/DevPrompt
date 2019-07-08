﻿# Dev Prompt
Tabbed command prompts for developers

## Download
[DevPrompt.msi](http://www.peterspada.com/Download/DevPrompt) - Latest version 1.4.0.0

The installation does not run with administrator rights.
Windows will warn you not to install it since the MSI file is unsigned.
It’s perfectly safe though, just click the More Info link and install anyway.

## Note for development
To build and run you MUST either:
* Run the registry file Build\publickey-verify.reg as admin to skip strong name verification for public key token 19bc3e3d1db2ab26. That public key is used to delay-sign DevPrompt during development.
* Or set a user environment variable DevPromptNoSign=1. DevPrompt will have no public key, which is fine unless a plugin requires it.

## Features
* Hosts cmd.exe and powershell.exe command prompts in multiple tabs
* Quickly clone a command prompt
* Save the state of all tabs on restart
* Grab other command prompts into a tab
* Quickly access developer tools and links that you use often

## Project overview
* Open __DevPrompt.sln__ in Visual Studio 2019 on Windows 10
* __DevPrompt__: C# project with UI written in WPF. This is the shell that hosts processes owned by DevNative.
* __DevNative__: C++ project that contains the global app and state of running processes. It uses COM interfaces to communicate with the managed UI in DevPrompt.exe.
* __DevInject__: C++ project that gets injected into every hosted command prompt process. Threads are created to communicate with DevNative through pipes.
* __DevInjector__: C++ project for a helper executable to inject DevInject into command prompt processes of opposite bitness.

## Coding Standards
* Use default formatting for C# and C++ in VS2019 (as if Format Document command was run)
* Use this. or this-> for any members in C# and C++ code
* Don't use "var" in C# or "auto" in C++
