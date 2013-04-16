using System.ServiceModel;

namespace ExtWcf.Test {
    [ServiceContract()]
    public interface ITestService {
        [OperationContract]
        void SetSomething(string something);

        [OperationContract]
        string GetSomething();

        //[OperationContract]
        //string GetSomethingComplex(string normalStr1, ref string refStr, string normalStr2, out string outStr);
    }
}
