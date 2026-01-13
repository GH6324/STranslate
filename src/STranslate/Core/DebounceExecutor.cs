namespace STranslate.Core;

public sealed class DebounceExecutor : IDisposable
{
    private CancellationTokenSource? _cts;
    private readonly Lock _lock = new();
    private bool _disposed = false;

    /// <summary>
    /// 取消当前待执行的防抖任务
    /// </summary>
    public void Cancel()
    {
        lock (_lock)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }

    /// <summary>
    /// 同步动作防抖执行
    /// </summary>
    public void Execute(Action action, TimeSpan delay)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
        }

        var token = _cts.Token;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay, token);

                if (!token.IsCancellationRequested)
                {
                    action.Invoke();
                }
            }
            catch (TaskCanceledException)
            {
                // 忽略取消
            }
        }, token);
    }

    /// <summary>
    /// 异步动作防抖执行 (支持 async/await)
    /// </summary>
    public void ExecuteAsync(Func<Task> asyncAction, TimeSpan delay)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
        }

        var token = _cts.Token;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay, token);

                if (!token.IsCancellationRequested)
                {
                    await asyncAction();
                }
            }
            catch (TaskCanceledException)
            {
                // 忽略取消
            }
        }, token);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Cancel(); // 复用 Cancel 方法
        _disposed = true;
    }
}