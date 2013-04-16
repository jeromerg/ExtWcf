using System;
using System.Reflection;
using System.Threading;

namespace ExtWcf {

    /// <summary>generic type used as marker between the Begin-method and the End-method</summary>
    /// <typeparam name="TResult"></typeparam>
    public class ExtAsyncResult<TResult> : ExtAsyncResult {
        public ExtAsyncResult(IAsyncResult wcfAsyncResult, MethodInfo syncMethod) : base(wcfAsyncResult, syncMethod) {}
    }

    public class ExtAsyncResult
    {
        private readonly IAsyncResult wcfAsyncResult;
        private readonly MethodInfo syncMethod;

        public ExtAsyncResult(IAsyncResult wcfAsyncResult, MethodInfo syncMethod)
        {
            this.wcfAsyncResult = wcfAsyncResult;
            this.syncMethod = syncMethod;
        }

        public object AsyncState
        {
            get { return this.wcfAsyncResult.AsyncState; }
        }

        public WaitHandle AsyncWaitHandle
        {
            get { return this.wcfAsyncResult.AsyncWaitHandle; }
        }

        public bool CompletedSynchronously
        {
            get { return this.wcfAsyncResult.CompletedSynchronously; }
        }

        public bool IsCompleted
        {
            get { return this.wcfAsyncResult.IsCompleted; }
        }

        public MethodInfo SyncMethod
        {
            get { return this.syncMethod; }
        }

        public IAsyncResult WcfAsyncResult
        {
            get { return this.wcfAsyncResult; }
        }
    }
}