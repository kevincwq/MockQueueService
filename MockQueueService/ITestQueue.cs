using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockQueueService
{
    public interface ITestQueue<T>
    {
        int QueuedCount { get; }

        int HiddenCount { get; }

        IEnumerable<T> QueuedItems { get; }

        IEnumerable<T> HiddenItems { get; }

        Action<string> OnMethodExecuting { get; set; }

        Task WhenEmptied();
    }
}
