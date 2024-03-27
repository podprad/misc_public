namespace PdfButcher.Tests.Common
{
    using System;
    using System.Diagnostics;

    [DebuggerDisplay("{" + nameof(DisplayText) + "}")]
    public sealed class MemoryWatcher : IDisposable
    {
        private readonly Process _currentProcess = Process.GetCurrentProcess();

        public long BeginBytes { get; set; }

        public long BeginMegabytes => BeginBytes / 1024 / 1024;

        public long PeakBytes { get; set; }

        public long PeakMegabytes => PeakBytes / 1024 / 1024;

        public long LastBytes { get; set; }

        public long LastMegabytes => LastBytes / 1024 / 1024;

        public long FromBeginBytes => this.LastBytes - this.BeginBytes;

        public long FromBeginMegabytes => FromBeginBytes / 1024 / 1024;

        public string DisplayText => $"Begin: {BeginMegabytes}MB, Peak: {PeakMegabytes}MB, Last: {LastMegabytes}MB";

        public void CollectMemory(long thresholdMegabytes = 500, int maxTries = 4)
        {
            for (int i = 0; i < maxTries; i++)
            {
                _currentProcess.Refresh();

                if (_currentProcess.PrivateMemorySize64 / 1024 / 1024 < thresholdMegabytes)
                {
                    break;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        public void Reset()
        {
            _currentProcess.Refresh();
            this.BeginBytes = _currentProcess.PrivateMemorySize64;
            this.PeakBytes = this.BeginBytes;
            this.LastBytes = this.BeginBytes;
        }

        public void Update()
        {
            _currentProcess.Refresh();
            this.LastBytes = _currentProcess.PrivateMemorySize64;
            this.PeakBytes = Math.Max(this.PeakBytes, this.LastBytes);
        }

        public void Dispose()
        {
            _currentProcess?.Dispose();
        }
    }
}