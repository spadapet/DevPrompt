#pragma once

#ifdef DEVINJECT_EXPORTS
#define DEV_INJECT_API __declspec(dllexport)
#else
#define DEV_INJECT_API __declspec(dllimport)
#endif
