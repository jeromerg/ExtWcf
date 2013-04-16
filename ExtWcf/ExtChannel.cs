using System;
using System.Reflection;

namespace ExtWcf {
    internal class ExtChannel<TWcfContract> : IExtChannel<TWcfContract> {
        private readonly CallDataCollector<TWcfContract> callDataCollector;
        private readonly Type asyncType;
        private readonly TWcfContract asyncChannel;

        #region private class

        #endregion

        /// <summary> Ctor </summary>
        /// <param name="asyncType">Assumed to be created by the ExtChannelFactory - it contains all async methods</param>
        /// <param name="asyncChannel">Assumed to be created by the ExtChannelFactory - it is a channel containing all async operations</param>
        internal ExtChannel(Type asyncType, TWcfContract asyncChannel) {
            // prepare call data collector
            this.callDataCollector = new CallDataCollector<TWcfContract>();

            this.asyncType = asyncType;
            this.asyncChannel = asyncChannel;
        }

        #region public Methods

        /// <summary>Sync Call </summary>
        /// <param name="action">action containing single call wcfContract.Operation(...) as lambda expression</param>
        public void Call(Action<TWcfContract> action) {
            action(asyncChannel);
        }

        /// <summary>Sync Call </summary>
        /// <param name="func">function containing single call wcfContract.Operation(...) as lambda expression</param>
        /// <returns>the result of the call</returns>
        public TResult Call<TResult>(Func<TWcfContract, TResult> func) {
            return func(asyncChannel);
        }


        /// <summary>Async Begin Call </summary>
        /// <param name="action">action containing single call wcfContract.Operation(...) as lambda expression</param>
        /// <param name="callback">async callback</param>
        /// <param name="state">async state </param>
        public ExtAsyncResult BeginCall(Action<TWcfContract> action, AsyncCallback callback, object state) {
            // collect call data
            MethodInfo syncMethod;
            object[] syncArguments;
            this.callDataCollector.Call(action, out syncMethod, out syncArguments);

            // call
            var returnValue = BeginCall(syncMethod, syncArguments, callback, state);

            // wrap result in order to identify which end method must be called
            var wcfAsyncCall = new ExtAsyncResult(returnValue, syncMethod);

            return wcfAsyncCall;
        }

        /// <summary>Async Begin Call </summary>
        /// <param name="func">action containing single call wcfContract.Operation(...) as lambda expression</param>
        /// <param name="callback">async callback</param>
        /// <param name="state">async state </param>
        public ExtAsyncResult<TResult> BeginCall<TResult>(Func<TWcfContract, TResult> func, AsyncCallback callback, object state) {
            // collect call data
            MethodInfo syncMethod;
            object[] syncArguments;
            this.callDataCollector.Call(func, out syncMethod, out syncArguments);

            // call
            var returnValue = BeginCall(syncMethod, syncArguments, callback, state);

            // wrap result in order to identify which end method must be called
            var wcfAsyncCall = new ExtAsyncResult<TResult>(returnValue, syncMethod);

            return wcfAsyncCall;
        }

        /// <summary>Async End Call </summary>
        /// <param name="asyncResult">the referenced given back by the Begin method</param>
        /// <returns>the result of the call</returns>
        public void EndCall(ExtAsyncResult asyncResult) {
            EndCallHelper(asyncResult);
        }

        /// <summary>Async End Call </summary>
        /// <param name="asyncResult">the referenced given back by the Begin method</param>
        /// <returns>the result of the call</returns>
        public TResult EndCall<TResult>(ExtAsyncResult<TResult> asyncResult) {
            var result = EndCallHelper(asyncResult);
            return (TResult) result;
        }

        private object EndCallHelper(ExtAsyncResult asyncResult) {
            // prepare call to end method
            MethodInfo endMethod = asyncType.GetMethod("End" + asyncResult.SyncMethod.Name);
            object[] arguments = new object[endMethod.GetParameters().Length];
            arguments[arguments.Length - 1] = asyncResult.WcfAsyncResult;

            object result = endMethod.Invoke(this.asyncChannel, arguments);
            return result;
        }

        #endregion


        #region private helpers

        private IAsyncResult BeginCall(MethodInfo syncMethod, object[] syncArguments, AsyncCallback callback, object state) {
            MethodInfo beginMethod = asyncType.GetMethod("Begin" + syncMethod.Name);
            int beginMethodArgsCount = beginMethod.GetParameters().Length;
            var beginMethodArgs = new object[beginMethodArgsCount];
            Array.Copy(syncArguments, 0, beginMethodArgs, 0, beginMethodArgsCount - 2);
            beginMethodArgs[beginMethodArgs.Length - 2] = callback;
            beginMethodArgs[beginMethodArgs.Length - 1] = state;

            // invoke
            var returnValue = (IAsyncResult) beginMethod.Invoke(asyncChannel, beginMethodArgs);
            return returnValue;
        }

        #endregion
    }
}