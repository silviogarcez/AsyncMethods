using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncMethods.Interfaces
{
    public interface IMethod
    {
        void MethodExecute<T>(Func<T> method, bool waitReturn, TimeSpan timeout);
        void MethodExecute<T>(Func<T> method, Action<Task<T>> callback, bool waitReturn, TimeSpan timeout);
        void MethodExecute<T>(Func<T> method, Action<Task<T>> callback, Action<Task<T>> errorcallback, bool waitReturn, TimeSpan timeout);
    }
}
