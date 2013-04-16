using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace ExtWcf {
    public class ExtChannelFactory<TWcfContract> {
        /// <summary>
        /// static ensures that the type is dynamically created only once in the whole process. 
        /// Elsewhere we would get an exception 
        /// </summary>
        private static readonly Type asyncType;

        /// <summary>Wrapped wcf factory</summary>
        private readonly ChannelFactory channelFactory;

        static ExtChannelFactory() {
            asyncType = AsyncInterfaceEmitter.Emit<TWcfContract>(false);
        } 

        public ExtChannelFactory(Binding binding, EndpointAddress endpointAddress) {            
            Type channelFactoryType = typeof(ChannelFactory<>).MakeGenericType(new Type[] { asyncType });
            channelFactory = (ChannelFactory)Activator.CreateInstance(channelFactoryType, binding, endpointAddress);
        }

        public IExtChannel<TWcfContract> CreateChannel() {
            var wcfProxy = (TWcfContract) channelFactory.GetType().GetMethod("CreateChannel", new Type[0]).Invoke(channelFactory, new object[0]);
            return new ExtChannel<TWcfContract>(asyncType, wcfProxy);
        }
    }
}