
#include "stdafx.h"
#include "SystemTrayHook.h"

#pragma data_seg(".shared")
#pragma comment(linker, "/SECTION:.shared,RWS")

HWND applicationHandle = NULL;

#pragma data_seg()

static HHOOK hook = NULL;
HINSTANCE hInstance = NULL;

static LRESULT CALLBACK HookCallback(int code, WPARAM wParam, LPARAM lParam);

BOOL RegisterSystemTrayHook(HWND hWnd)
{
	HWND hShell = FindWindow(L"Shell_TrayWnd", NULL);
	DWORD shellThread = GetWindowThreadProcessId(hShell, NULL);

	applicationHandle = hWnd;

	hook = SetWindowsHookEx(WH_CALLWNDPROCRET, (HOOKPROC) HookCallback, hInstance, shellThread);
	return hook != NULL;
}

BOOL UnregisterSystemTrayHook()
{
	if (hook != NULL)
	{
		return UnhookWindowsHookEx(hook);
	}
	else
	{
		return TRUE;
	}
}

#define SH_TRAY_DATA 1

typedef struct
{
    DWORD dwHz;
    DWORD dwMessage;
    NOTIFYICONDATA nid; // this is a 64-bit structure, when running in 64-bit mode, but should be 32!
} SHELLTRAYDATA;

static LRESULT CALLBACK HookCallback(int code, WPARAM wParam, LPARAM lParam)
{
    if (code >= 0)
    {
		CWPRETSTRUCT *pInfo = (CWPRETSTRUCT*) lParam;

		if (pInfo->message == WM_COPYDATA)
		{
			COPYDATASTRUCT* copyDataStruct = (COPYDATASTRUCT*) pInfo->lParam;
			if (copyDataStruct->dwData == SH_TRAY_DATA)
			{
				if (((SHELLTRAYDATA*) copyDataStruct->lpData)->dwHz == 0x34753423)
				{
					DWORD_PTR result;
					SendMessageTimeout(applicationHandle, WM_COPYDATA, pInfo->wParam, pInfo->lParam,
						SMTO_NORMAL, 2000, &result);
				}
			}
		}
	}

	return CallNextHookEx(NULL, code, wParam, lParam);
}
