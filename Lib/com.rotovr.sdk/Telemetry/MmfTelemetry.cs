
using System.Threading;
using System.Threading.Tasks;
using System.IO.MemoryMappedFiles;


using System.Runtime.InteropServices;
using System.IO;
using System;

namespace com.rotovr.sdk.Telemetry
{
    internal class MmfTelemetryConfig 
    {
        public string Name { get; set; } = "MmfTelemetry"; 
        
        public bool Create { get; set; } = false;

        public string MutexName { get; set; } = null;

        public bool IsGlobal { get; set; } = true;

        public MmfTelemetryConfig() { }

        public MmfTelemetryConfig(string name, bool isCreator = false)
        {
            this.Name = name; this.Create = isCreator;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]

    internal class MmfTelemetry<TData> : TelemetryBase<TData, MmfTelemetryConfig>
        where TData : struct
    {

        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _accessor;
        private int _dataSize = Marshal.SizeOf<TData>();

        Mutex _mutex = null;

        public MmfTelemetry(MmfTelemetryConfig config) : base(config)
        {
        }

        protected override void Configure(MmfTelemetryConfig config)
        {
            if (config.Create)
            {
                var res = CreateOrOpen()
                    .ContinueWith(t =>
                    {
                        if (t.Result == 0)
                        {
                            _accessor = _mmf.CreateViewAccessor();
                        }
                        else
                        {
                            throw new Exception($"Failed to create or open memory mapped file. Error code: {t.Result}");
                        }
                    });
            }
        }



        public override TData Receive()
        {
            if (Config.MutexName != null)
            {
                if (_mutex == null)
                {
                    bool mutexCreated = false;
                    if (Config.Create)
                        _mutex = new Mutex(true, Config.MutexName, out mutexCreated);
                    else
                        mutexCreated = Mutex.TryOpenExisting(Config.MutexName, out _mutex);
                }

                if (_mutex != null)
                {
                    _mutex.WaitOne();
                }
            }
            TData data = default;

            _accessor?.Read(0, out  data);
            if (Config.MutexName != null && _mutex != null)
            {
                _mutex.ReleaseMutex();

            }
            return data;

        }

        public override Task<TData> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Receive());
        }


        public override int Send(TData data)
        {
            if ((_accessor != null))
            {
                if(Config.MutexName != null )
                {
                    if (_mutex == null)
                    {
                        bool mutexCreated = false;
                        if (Config.Create)
                            _mutex = new Mutex(true, Config.MutexName, out mutexCreated);
                        else
                            mutexCreated = Mutex.TryOpenExisting(Config.MutexName, out _mutex);
                    }

                    if(_mutex != null)
                    {
                        _mutex.WaitOne();
                    }
                }
                _accessor?.Write(0, ref data);

                if(Config.MutexName != null && _mutex != null)
                {
                    _mutex.ReleaseMutex();                    
                }

                return _dataSize;
            }

            return 0;
        }

        public override void Dispose()
        {
            _mmf?.Dispose();
            _accessor?.Dispose();
            _mutex?.Dispose();
            _mutex = null;
        }

        public Task<int> CreateOrOpen()
        {
            if (_accessor != null)
            {
                return Task.FromResult(0);
            }

            return Task.Run(() =>
            {
                try
                {
                    string scope = Config.IsGlobal ? "Global\\" : "";
                    _mmf = MemoryMappedFile.CreateOrOpen(scope + Config.Name, Marshal.SizeOf<TData>());
                    _accessor = _mmf.CreateViewAccessor();
                    return 0;
                }
                catch (UnauthorizedAccessException)
                {
                    return 2;
                }
                catch (FileNotFoundException)
                {
                    return 1;
                }
            });
        }

        private int TryOpen()
        {
            try
            {
                _mmf = MemoryMappedFile.OpenExisting(Config.Name);
                _accessor = _mmf.CreateViewAccessor();
                return 0;
            }
            catch (UnauthorizedAccessException)
            {
                return 2;
            }
            catch (FileNotFoundException)
            {
                return 1;
            }
        }

        public Task<int> TryOpenAsync(int timeout = 0, CancellationToken cancellationToken = default)
        {

            return Task.Run(async () =>
            {
                int result = 1;
                using var cts = new CancellationTokenSource(timeout);
                try
                {
                    do
                    {
                        result = TryOpen();
                        switch (result)
                        {
                            case 1:
                                await Task.Delay(4000, cancellationToken);
                                break;
                            case 2:
                                cts.Cancel();
                                break;


                        }

                    } while (result != 0 && (!cancellationToken.IsCancellationRequested || !cts.Token.IsCancellationRequested));
                }
                catch (TaskCanceledException)
                {
                    // Handle the cancellation exception if needed
                }
                catch (OperationCanceledException)
                {
                    // Handle the cancellation exception if needed
                }

                return result;


            }, cancellationToken);
        }

    }
}
