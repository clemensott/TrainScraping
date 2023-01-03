using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrainScrapingWorkerService
{
    abstract class RegularTask : IDisposable
    {
        private bool isExecuting;
        private DateTime lastRun;
        private readonly TimeSpan interval;

        public RegularTask(TimeSpan interval)
        {
            this.interval = interval;
            lastRun = interval > TimeSpan.Zero ? DateTime.MinValue : DateTime.MaxValue;
        }

        public abstract Task Execute();

        public async Task Tick()
        {
            if (!isExecuting && DateTime.Now > lastRun + interval)
            {
                try
                {
                    isExecuting = true;
                    await Execute();
                }
                finally
                {
                    isExecuting = false;
                    lastRun = DateTime.Now;
                }
            }
        }

        public virtual void Dispose()
        {
            isExecuting = true;
            lastRun = DateTime.Now;
        }
    }
}
