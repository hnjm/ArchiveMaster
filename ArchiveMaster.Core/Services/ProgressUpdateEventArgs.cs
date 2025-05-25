using System;
using System.Numerics;

namespace ArchiveMaster.Services
{
    public class ProgressUpdateEventArgs : EventArgs
    {
        public double Progress { get; }

        public ProgressUpdateEventArgs(double progress)
        {
            if (double.IsInfinity(progress))
            {
                throw new ArgumentException("进度应当为0-1的实数或使用NaN表示不确定", nameof(progress));
            }

            if (progress < 0)
            {
                progress = 0;
            }
            else if (progress > 1)
            {
                progress = 1;
            }

            Progress = progress;
        }
    }
}