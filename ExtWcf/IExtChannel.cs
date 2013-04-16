using System;

namespace ExtWcf {
    public interface IExtChannel<TWcfContract> {
        
        /// <summary>Sync Call </summary>
        /// <param name="action">action containing single call wcfContract.Operation(...) as lambda expression</param>
        void Call(Action<TWcfContract> action);

        /// <summary>Sync Call </summary>
        /// <param name="func">function containing single call wcfContract.Operation(...) as lambda expression</param>
        TResult Call<TResult>(Func<TWcfContract, TResult> func);

        /// <summary>Async Begin Call </summary>
        /// <param name="action">action containing single call wcfContract.Operation(...) as lambda expression</param>
        /// <param name="callback">async callback</param>
        /// <param name="state">async state </param>
        ExtAsyncResult BeginCall(Action<TWcfContract> action, AsyncCallback callback, object state);

        /// <summary>Async Begin Call </summary>
        /// <param name="func">func containing single call wcfContract.Operation(...) as lambda expression</param>
        /// <param name="callback">async callback</param>
        /// <param name="state">async state </param>
        ExtAsyncResult<TResult> BeginCall<TResult>(Func<TWcfContract, TResult> func, AsyncCallback callback, object state);

        void EndCall(ExtAsyncResult asyncResult);

        /// <summary>Async End Call </summary>
        /// <param name="asyncResult">the referenced given back by the Begin method</param>
        /// <returns>the result of the call</returns>
        TResult EndCall<TResult>(ExtAsyncResult<TResult> asyncResult);
    }
}