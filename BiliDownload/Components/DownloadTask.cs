using BiliDownload.Exceptions;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BiliDownload.Components;

public class DownloadTask
{
    private HttpClient _client;
    public Guid TaskGuid { get; init; }
    public string Url { get; set; }
    public string Path { get; set; }
    private Stream _fileStream;
    public ulong DownloadedBytes { get; set; } = 0;
    public ulong TotalBytes { get; set; } = 0;

    private ulong _deltaBytes = 0;

    /// <summary>
    /// When got, the value will be set to 0.
    /// </summary>
    public ulong DeltaBytes
    {
        get
        {
            var ret = _deltaBytes;
            _deltaBytes = 0;
            return ret;
        }
    }

    public bool IsStarted { get; set; } = false;
    public bool IsRunning { get; set; } = false;
    public bool IsCompleted { get; set; } = false;
    /// <summary>
    /// If true, see FailedException.
    /// </summary>
    public bool IsFailed { get; set; } = false;
    public DownloadTaskFailedException FailedException { get; private set; }

    private string referer;
    private string ua;
    private bool inited = false;
    private bool isCanceled = false;
    private bool isInterrupted = false;
    private Stream _respStream;
    private HttpResponseMessage _resp;
    public event Action<DownloadTask> Completed;

    private DownloadTask()
    {
        TaskGuid = new Guid();
    }
    private DownloadTask(Guid guid)
    {
        TaskGuid = guid;
    }

    public static DownloadTask CreateNew(string url, string path, string? referer = null, string? ua = null)
    {
        var task = new DownloadTask()
        {
            Url = url,
            Path = path
        };
        task._client = new HttpClient();
        if (!string.IsNullOrEmpty(referer))
            task._client.DefaultRequestHeaders.Add("referer", referer);
        if (!string.IsNullOrEmpty(ua))
            task._client.DefaultRequestHeaders.Add("User-Agent", ua);
        task.referer = referer;
        task.ua = ua;
        return task;
    }

    public static DownloadTask Restore(DownloadTaskRestoreModel model)
    {
        var task = new DownloadTask(model.TaskGuid)
        {
            Url = model.Url,
            Path = model.Path,
            DownloadedBytes = model.DownloadedBytes,
            TotalBytes = model.TotalBytes,
            inited = true
        };
        task._client = new HttpClient();
        if (!string.IsNullOrEmpty(model.Referer))
            task._client.DefaultRequestHeaders.Add("referer", model.Referer);
        if (!string.IsNullOrEmpty(model.UA))
            task._client.DefaultRequestHeaders.Add("User-Agent", model.UA);
        task.referer = model.Referer;
        task.ua = model.UA;
        return task;
    }

    public DownloadTaskRestoreModel CreateRestoreModel()
    {
        return new DownloadTaskRestoreModel()
        {
            TaskGuid = TaskGuid,
            Path = Path,
            Referer = referer,
            UA = ua,
            Url = Url,
            DownloadedBytes = DownloadedBytes,
            TotalBytes = TotalBytes
        };
    }

    private async Task CompleteAsync()
    {
        IsCompleted = true;
        IsRunning = false;
        Clean();
        Completed?.Invoke(this);
    }

    private void Clean()
    {
        _fileStream?.Dispose();
        _respStream?.Dispose();
        _resp?.Dispose();
        _client?.Dispose();
    }

    private async Task FailAsync(DownloadTaskFailedException e)
    {
        Clean();
        FailedException = e;
        IsFailed = true;
    }

    private async void DownloadLoop()
    {
        IsStarted = true;
        var buf = new byte[1024];
        while (true)
        {
            if (isCanceled)
            {
                Clean();
                File.Delete(Path);
                return;
            }

            if (isInterrupted)
            {
                Clean();
                return;
            }

            if (IsCompleted)
                return;
            if (!IsRunning)
            {
                await Task.Delay(1000);
                continue;
            }

            try
            {
                var readed = await _respStream.ReadAsync(buf, 0, buf.Length).WaitAsync(TimeSpan.FromSeconds(10));
                await _fileStream.WriteAsync(buf, 0, readed);
                await _fileStream.FlushAsync();
                DownloadedBytes += (ulong)readed;
                _deltaBytes += (ulong)readed;
            }
            catch (TimeoutException)
            {
                while (!await ConnectAsync()) // Try to reconnect.
                    await Task.Delay(5000);
                continue;
            }
            catch (Exception e)
            {
                await FailAsync(new DownloadTaskFailedException("Download task failed, see inner exception.", e));
                return;
            }

            if (DownloadedBytes == TotalBytes)
            {
                await CompleteAsync();
                return;
            }
        }
    }

    private async Task AllocateAsync()
    {
        var buf = new byte[1024 * 1024];
        var written = 0;
        while (true)
        {
            var left = TotalBytes - (ulong)written;
            if (left <= (ulong)buf.Length)
            {
                await _fileStream.WriteAsync(buf, 0, (int)left);
                await _fileStream.FlushAsync();
                break;
            }
            else
            {
                await _fileStream.WriteAsync(buf, 0, buf.Length);
            }

            written += buf.Length;
        }
    }

    public async Task StartAsync()
    {
        if (inited && DownloadedBytes == TotalBytes)
        {
            await CompleteAsync();
            return;
        }

        if (!inited)
        {
            try
            {
                if (!await ConnectAsync()) throw new HttpRequestException();
                TotalBytes = Convert.ToUInt64(_resp.Content.Headers.GetValues("Content-Length").First());
                if (TotalBytes == 0) throw new HttpRequestException();
                _fileStream = File.Open(Path, FileMode.Create);
                await AllocateAsync();
                _fileStream.Seek(0, SeekOrigin.Begin);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            IsRunning = true;
        }
        else // restore
        {
            try
            {
                if (!await ConnectAsync()) throw new HttpRequestException();
                var info = new FileInfo(Path);
                if (!info.Exists) throw new FileNotFoundException();
                else
                {
                    _fileStream = File.OpenWrite(Path);
                    _fileStream.Seek((long)DownloadedBytes, SeekOrigin.Begin);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        DownloadLoop();
    }
    public async Task<bool> ConnectAsync()
    {
        if (DownloadedBytes != 0)
            _client.DefaultRequestHeaders.Range = new System.Net.Http.Headers.RangeHeaderValue((long)DownloadedBytes, null);
        try
        {
            _resp = await _client.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead);
            _resp.EnsureSuccessStatusCode();
        }
        catch
        {
            return false;
        }
        _respStream = await _resp.Content.ReadAsStreamAsync();
        return true;
    }
    public async Task WaitForCompleteAsync()
    {
        while (!IsCompleted)
        {
            if (isCanceled || isInterrupted) return;
            if (IsFailed) throw FailedException;
            await Task.Delay(100);
        }
    }

    public void Pause()
    {
        IsRunning = false;
    }

    public void Resume()
    {
        IsRunning = true;
    }

    public void Cancel()
    {
        isCanceled = true;
    }

    public void Interrupt()
    {
        isInterrupted = true;
    }
}

