using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public class CommandQueue : IDisposable
{
    private BlockingCollection<Action> queue = new BlockingCollection<Action>();
    private Thread worker;

    public CommandQueue()
    {
        worker = new Thread(Process);
        worker.Start();
    }

    public void Enqueue(Action cmd)
    {
        queue.Add(cmd);
    }

    private void Process()
    {
        foreach (var cmd in queue.GetConsumingEnumerable())
        {
            int retries = 3;

            while (retries-- > 0)
            {
                try
                {
                    cmd();
                    break;
                }
                catch
                {
                    System.Threading.Thread.Sleep(1000);
                    if (retries == 0) throw;
                }
            }
        }
    }

    public void Dispose()
    {
        queue.CompleteAdding();
    }
}