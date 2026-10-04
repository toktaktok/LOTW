using System;
using UnityEngine;

public static class AwaitableExtensions
{
    /// <summary>
    /// Awaitable을 fire-and-forget으로 실행합니다.
    /// Cancel()과 달리 작업을 중단하지 않으며, 예외는 콘솔에 로깅됩니다.
    /// </summary>
    public static async void Forget(this Awaitable awaitable)
    {
        try
        {
            await awaitable;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
}
