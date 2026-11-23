using NAudio.Wave;

namespace AlertSpeaker.Tts;

/// <summary>
/// 顺序播放队列：先合成再本机播放，同时多条不叠音。
/// </summary>
public sealed class PlaybackQueue : IDisposable
{
    private readonly QwenTtsClient _tts;
    private readonly ChannelItemQueue _queue = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _worker;

    public PlaybackQueue(QwenTtsClient tts)
    {
        _tts = tts;
        _worker = Task.Run(ProcessAsync);
    }

    public void Enqueue(string text, string apiKey, string voice)
    {
        _queue.Add(new WorkItem(text, apiKey, voice));
    }

    private async Task ProcessAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            WorkItem item;
            try
            {
                item = await _queue.TakeAsync(_cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            string? file = null;
            try
            {
                AppLog.Info($"开始合成并播放（字符数 {item.Text.Length}，音色 {item.Voice}）");
                var options = new VoiceOptions { Voice = item.Voice, Model = VoiceOptions.DefaultModel };
                file = await _tts.SynthesizeToFileAsync(item.ApiKey, item.Text, options, _cts.Token).ConfigureAwait(false);
                await PlayFileAsync(file, _cts.Token).ConfigureAwait(false);
                AppLog.Info("播放完成");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                AppLog.Error("合成或播放失败", ex);
            }
            finally
            {
                TryDelete(file);
            }
        }
    }

    private static async Task PlayFileAsync(string file, CancellationToken cancellationToken)
    {
        using var reader = new AudioFileReader(file);
        using var output = new WaveOutEvent();
        output.Init(reader);
        output.Play();
        while (output.PlaybackState == PlaybackState.Playing)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }
    }

    private static void TryDelete(string? file)
    {
        if (string.IsNullOrWhiteSpace(file))
            return;
        try { File.Delete(file); } catch { /* 临时文件删除失败可忽略 */ }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _worker.Wait(TimeSpan.FromSeconds(3)); } catch { /* 退出时忽略 */ }
        _cts.Dispose();
    }

    private sealed record WorkItem(string Text, string ApiKey, string Voice);

    private sealed class ChannelItemQueue
    {
        private readonly SemaphoreSlim _signal = new(0);
        private readonly Queue<WorkItem> _items = new();

        public void Add(WorkItem item)
        {
            lock (_items)
                _items.Enqueue(item);
            _signal.Release();
        }

        public async Task<WorkItem> TakeAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken).ConfigureAwait(false);
            lock (_items)
                return _items.Dequeue();
        }
    }
}
