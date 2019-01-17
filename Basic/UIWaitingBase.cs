using System;

public interface UIWaitingBase
{
    void Wait(string group, float threshold, float timeout,
        string note, string message, int buttonMask,
        DefaultHandler retryHandler, DefaultHandler okHandler);

    void Dialog(string group, string message, int buttonMask,
        DefaultHandler retryHandler, DefaultHandler okHandler);

    void Waiting(string group, string message);

    void Stop(string group);

    void StopAll();
}