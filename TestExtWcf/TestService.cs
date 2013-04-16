using System;

namespace ExtWcf.Test {
    public class TestService : ITestService {
        public void SetSomething(string v) {
            Console.WriteLine("TestService: SetSomething called with parameter v=" + v);
        }

        public string GetSomething() {
            Console.WriteLine("TestService: GetSomething called");
            return "something";
        }
        public string GetSomethingComplex(string normalStr1, ref string refStr, string normalStr2, out string outStr) {
            Console.WriteLine("TestService: GetSomething called");
            refStr = "";
            outStr = "";
            return "something";
        }
    }
}
