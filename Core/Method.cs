using AsyncMethods.Interfaces;
using LogManager;
using LogManager.Interfaces;
using LogManager.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncMethods.Core
{
    public class Method : IMethod
    {
        public ILog log;
        Message message;
        TaskScheduler scheduler;
        SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(Environment.ProcessorCount * 7);

        public Method()
        {
            scheduler = TaskScheduler.FromCurrentSynchronizationContext();
        }

        public Method(ILog log)
        {
            this.log = log;
            scheduler = TaskScheduler.FromCurrentSynchronizationContext();
        }

        public Method(string pathlog)
        {
            this.log = new Log(pathlog);
            scheduler = TaskScheduler.FromCurrentSynchronizationContext();
        }

        #region "Execution Methods"

        public void MethodExecute<T>(Func<T> method, bool waitReturn, TimeSpan timeout)
        {
            MethodExecute<T>(method, null, null, waitReturn, timeout, concurrencySemaphore);
        }

        public void MethodExecute<T>(Func<T> method, Action<Task<T>> callback, bool waitReturn, TimeSpan timeout)
        {
            MethodExecute<T>(method, callback, null, waitReturn, timeout, concurrencySemaphore);
        }

        public void MethodExecute<T>(Func<T> method, Action<Task<T>> callback, Action<Task<T>> errorcallback, bool waitReturn, TimeSpan timeout)
        {
            MethodExecute(method, callback, errorcallback, waitReturn, timeout, concurrencySemaphore);
        }

        private void MethodExecute<T>(Func<T> method, Action<Task<T>> callback, Action<Task<T>> errorcallback, bool waitReturn, TimeSpan timeout, SemaphoreSlim semaphore)
        {
            CancellationTokenSource cts = new CancellationTokenSource(timeout);
            TaskCreationOptions CreationTask = TaskCreationOptions.None;            

            if (!waitReturn)
            {
                CreationTask = TaskCreationOptions.LongRunning;
            }
            else
            {
                CreationTask = TaskCreationOptions.None;
            }

            var task = Task.Factory.StartNew<T>(() =>
            {
                try
                {
                    using (cts.Token.Register(Thread.CurrentThread.Abort, true))
                    {
                        semaphore.Wait();
                        return method();
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }, cts.Token, CreationTask, TaskScheduler.Current).ContinueWith(tsk =>
            {
                if (log != null && errorcallback == null)
                {
                    tsk.ContinueWith(ExecutarCallBackErrorLog, TaskContinuationOptions.NotOnRanToCompletion);
                }
                else if (errorcallback != null)
                    tsk.ContinueWith(errorcallback, TaskContinuationOptions.OnlyOnFaulted);

                if (callback != null)
                    tsk.ContinueWith(callback, TaskContinuationOptions.OnlyOnRanToCompletion);

                if (waitReturn)
                {
                    try
                    {
                        tsk.Wait(timeout);
                    }
                    catch
                    {

                    }
                }
            });
        }

        private void ExecutarCallBackErrorLog<T>(Task<T> ret)
        {
            message = new Message(0);
            message.Msg = "ThreadId:" + ret.Id + " cancelada";
            log.LogError(message);
        }

        #endregion
    }
}
