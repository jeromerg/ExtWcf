using System;
using System.ServiceModel;

namespace ExtWcf.Test {
    class Program {
        static void Main() {
            Console.WriteLine("MAIN: starting service...");
            var host = new ServiceHost(typeof(TestService));
            host.Open();
            Console.WriteLine("MAIN: service started.");

            Console.WriteLine("MAIN: starting TestAsync()...");
            TestAsync();
            Console.WriteLine("MAIN: TestAsync() ended.");

            Console.WriteLine("MAIN: closing service...");
            host.Close();
            Console.WriteLine("MAIN: service closed...");
        }

        private static void TestAsync() {
            Console.WriteLine("TestAsync: instantiating client classes...");
            var myBinding = new WSHttpBinding();
            var myEndpoint = new EndpointAddress("http://localhost:8732/Design_Time_Addresses/ExtWcf.Test/TestService");
            var asyncWcfClient = new ExtChannelFactory<ITestService>(myBinding, myEndpoint);
            IExtChannel<ITestService> extChannel = asyncWcfClient.CreateChannel();

            Console.WriteLine("TestAsync: calling asynchronously GetSomething() operation ...");
            ExtAsyncResult<string> asyncResult = extChannel.BeginCall(p => p.GetSomething(), null, null);
            string endCallResult = extChannel.EndCall(asyncResult);
            Console.WriteLine("TestAsync: GetSomething() operation called and returned " + endCallResult);

            Console.WriteLine("TestAsync: calling asynchronously SetSomething(\"yes\") operation ...");
            ExtAsyncResult asyncResult2 = extChannel.BeginCall(p => p.SetSomething("yes"), null, null);
            extChannel.EndCall(asyncResult2);
            Console.WriteLine("TestAsync: SetSomething(\"yes\") operation called.");
        }

    }
}
