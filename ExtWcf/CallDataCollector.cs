using System;
using System.Reflection;
using AopAlliance.Intercept;
using Spring.Aop.Framework;

namespace ExtWcf {
    internal class CallDataCollector<TWcfContract> : IMethodInterceptor {
        private readonly ProxyFactory proxyFactory;
        private readonly TWcfContract callDataCollectorDynProxy;

        [ThreadStatic] private MethodInfo calledMethodOnThisThread;
        [ThreadStatic] private object[] passedArgumentsOnThisThread;

        public CallDataCollector() {

            proxyFactory = new ProxyFactory();
            proxyFactory.AddInterface(typeof (TWcfContract));
            foreach (var interf in typeof (TWcfContract).GetInterfaces()) {
                proxyFactory.AddInterface(interf);
            }

            this.proxyFactory.AddAdvice(this);
            this.callDataCollectorDynProxy = (TWcfContract) proxyFactory.GetProxy();

        }

        public void Call(Action<TWcfContract> action, out MethodInfo calledMethod, out object[] passedArguments) {            
            action(callDataCollectorDynProxy);
            calledMethod = calledMethodOnThisThread;
            passedArguments = passedArgumentsOnThisThread ?? new object[0];
        } 

        public void Call<TResult>(Func<TWcfContract, TResult> func, out MethodInfo calledMethod, out object[] passedArguments) {
            func(callDataCollectorDynProxy);
            calledMethod = calledMethodOnThisThread;
            passedArguments = passedArgumentsOnThisThread ?? new object[0];
        }

        /// <summary> spring.net AOP interceptor used to store invocation data</summary>
        object IMethodInterceptor.Invoke(IMethodInvocation invocation) {
            this.calledMethodOnThisThread = invocation.Method;
            this.passedArgumentsOnThisThread = invocation.Arguments;
            // remark Proceed() is never called, elsewhere we get an exception: we have no target!
            return GetDefault(invocation.Method.ReturnType);
        }

        private static object GetDefault(Type type) {
            if (type == typeof (void)) {
                return null;
            }

            if (type.IsValueType) {
                return Activator.CreateInstance(type);
            }

            return null;
        }
    }
}
