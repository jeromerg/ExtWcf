using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.ServiceModel;

namespace ExtWcf {
    public class AsyncInterfaceEmitter {

        #region private classes and interface
        private class ParamData {
            public Type ParameterType { get; private set; }
            public ParameterAttributes Attributes { get; private set; }
            public string Name { get; private set; }

            public ParamData(Type parameterType, ParameterAttributes attributes, string name) {
                ParameterType = parameterType;
                Attributes = attributes;
                Name = name;
            }
        }

        private interface ITransformer<out THolderInfo> {
            THolderInfo HolderInfo { get; }
            object Apply(object originalValue);
        }

        private class Transformer<THolderInfo, TValue> : ITransformer<THolderInfo> {
            private readonly Func<TValue, TValue> change_;

            public Transformer(THolderInfo holderInfo, Func<TValue, TValue> change) {
                change_ = change;
                HolderInfo = holderInfo;
            }

            public THolderInfo HolderInfo { get; private set; }

            public object Apply(object originalValue) {
                if (originalValue == null) {
                    originalValue = default(TValue);
                }
                return change_((TValue) originalValue);
            }
        }

        #endregion

        public static Type Emit<T>(bool saveDll) {
            Type syncType = typeof (T);

            var assName = new AssemblyName("AsyncWcfContractAssembly");
            AssemblyBuilder assBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assName, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder modBuilder = saveDll 
                                           ? assBuilder.DefineDynamicModule("AsyncWcfContractModule", "AsyncWcfContractAssembly.dll") 
                                           : assBuilder.DefineDynamicModule("AsyncWcfContractModule");
            
            TypeBuilder asyncTypeBuilder = modBuilder.DefineType("AsyncWcfContract.Generated." + syncType.Name + "Async", TypeAttributes.Public|TypeAttributes.Interface|TypeAttributes.Abstract);
            asyncTypeBuilder.AddInterfaceImplementation(syncType);
            
            // copy type custom attributes
            // if name already explicitly set in original contract, then keep it, elsewhere set explicitly the name of the original contract

            var serviceContractNameTransformer = new Transformer<PropertyInfo, string>(
                typeof(ServiceContractAttribute).GetProperty("Name"), 
                syncName => syncName ?? syncType.Name);             

            IList<CustomAttributeData> syncTypeCustomAttributeDatas = CustomAttributeData.GetCustomAttributes(syncType);
            foreach (var customAttributeData in syncTypeCustomAttributeDatas) {
                CustomAttributeBuilder customAttributeBuilder = CreateAttributeCloneBuilder(customAttributeData, null, null, 
                                                     new ITransformer<PropertyInfo>[]{serviceContractNameTransformer});

                asyncTypeBuilder.SetCustomAttribute(customAttributeBuilder);
            }

            // add begin and end methods of all existing sync operation if not already existing, including operation located in base interfaces
            var allMethods = new List<MethodInfo>();
            allMethods.AddRange(syncType.GetMethods(BindingFlags.Public|BindingFlags.Instance));
            allMethods.AddRange(syncType.GetInterfaces().SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance)));

            foreach (var methodInfo in allMethods) {
                // skip non-operation methods
                if (!methodInfo.GetCustomAttributes(false).OfType<OperationContractAttribute>().Any()) {
                    continue;
                }

                // skip begin operations 
                if (methodInfo.Name.StartsWith("Begin")) {
                    continue;
                }

                // skip that already have BeginXxx and EndXxx declaration (just check begin method)
                string beginMethodName = "Begin" + methodInfo.Name;
                if (allMethods.Any(m => m.Name == beginMethodName)) {
                    continue;
                }

                // method is a CANDIDATE to create BeginXxx and EndMethodXxx !
                CreateAsyncMethodBeginAndEndBuilder(methodInfo, asyncTypeBuilder);
            }

            // produce interface
            Type asyncType = asyncTypeBuilder.CreateType();

            // TODO REMOVE THESE DEBUG THINGS
            //object[] syncCu = syncType.GetMethods();
            //object[] asyncCu = asyncType.GetMethods();
            if(saveDll) {
                assBuilder.Save("AsyncWcfContractAssembly.dll");
            }

            return asyncType;
        }

        private static void CreateAsyncMethodBeginAndEndBuilder(MethodInfo syncMethod, TypeBuilder targetTypeBuilder) {
            // TODO: transfer parameter attributes??

            var beginMethodParameterData = new List<ParamData>();
            foreach (var syncMethodParam in syncMethod.GetParameters()) {
                beginMethodParameterData.Add(new ParamData(syncMethodParam.ParameterType, syncMethodParam.Attributes, syncMethodParam.Name));
            }
            beginMethodParameterData.Add(new ParamData(typeof (AsyncCallback), ParameterAttributes.None, "asyncCallback"));
            beginMethodParameterData.Add(new ParamData(typeof (object), ParameterAttributes.None, "asyncState"));

            IList<CustomAttributeData> syncMethodAttributeDatas = CustomAttributeData.GetCustomAttributes(syncMethod);
            //-----------------------------------------
            // create builder for the begin method
            //-----------------------------------------
            MethodBuilder beginMethodBuilder = targetTypeBuilder.DefineMethod("Begin" + syncMethod.Name,
                                                                              syncMethod.Attributes, 
                                                                              syncMethod.CallingConvention, 
                                                                              typeof (IAsyncResult),
                                                                              beginMethodParameterData.Select(p => p.ParameterType).ToArray());

            // define more precisely parameters
            for (int index = 0; index < beginMethodParameterData.Count; index++) {                
                ParamData paramData = beginMethodParameterData[index]; 
                // remark: DefineParameter first parameter (index = 0) is the return value => that's why index+1
                beginMethodBuilder.DefineParameter(index + 1, paramData.Attributes, paramData.Name);
            }

            // copy operation contract metadata 
            var asyncOperationTransformer = new Transformer<PropertyInfo, bool>(typeof (OperationContractAttribute).GetProperty("AsyncPattern"),
                                                                                b => true);
            foreach (var customAttribute in syncMethodAttributeDatas) {
                CustomAttributeBuilder customAttributeBuilder = CreateAttributeCloneBuilder(customAttribute, null, null, 
                                                         new ITransformer<PropertyInfo>[] {asyncOperationTransformer});
                beginMethodBuilder.SetCustomAttribute(customAttributeBuilder);
            }
            //-----------------------------------------
            // create builder for the end method
            //-----------------------------------------
            MethodBuilder endMethodBuilder = targetTypeBuilder.DefineMethod("End" + syncMethod.Name,
                                                                            syncMethod.Attributes,
                                                                            syncMethod.CallingConvention,
                                                                            syncMethod.ReturnType,
                                                                            new[] {typeof (IAsyncResult)});

            foreach (var customAttributeData in syncMethodAttributeDatas) {
                if(customAttributeData.Constructor.DeclaringType == typeof(OperationContractAttribute)) {
                    continue; // OperationContractAttribute is not copied to end method
                }
                CustomAttributeBuilder customAttributeBuilder = CreateAttributeCloneBuilder(customAttributeData, null, null, null);
                endMethodBuilder.SetCustomAttribute(customAttributeBuilder);
            }
        }

        private static CustomAttributeBuilder CreateAttributeCloneBuilder(CustomAttributeData attributeData,
                                                    ITransformer<ParameterInfo>[] constructorParameterTransformers,
                                                    ITransformer<FieldInfo>[] fieldTransformers,
                                                    ITransformer<PropertyInfo>[] propertyTransformers) {

            // in order to allow nullable values
            constructorParameterTransformers = constructorParameterTransformers ?? new ITransformer<ParameterInfo>[0];
            fieldTransformers = fieldTransformers ?? new ITransformer<FieldInfo>[0];
            propertyTransformers = propertyTransformers ?? new ITransformer<PropertyInfo>[0];

            ParameterInfo[] constructorParameterInfos = attributeData.Constructor.GetParameters();
            
            var constructorArguments = new List<ParameterInfo>();
            var constructorArgumentValues = new List<object>();
            for (int index = 0; index < attributeData.ConstructorArguments.Count; index++) {
                var ctorArg = attributeData.ConstructorArguments[index];
                constructorArguments.Add(constructorParameterInfos[index]);
                constructorArgumentValues.Add(ctorArg.Value);
            }

            var propertyArguments = new List<PropertyInfo>();
            var propertyArgumentValues = new List<object>();
            var fieldArguments = new List<FieldInfo>();
            var fieldArgumentValues = new List<object>();
            foreach (var namedArg in attributeData.NamedArguments) {
                var fi = namedArg.MemberInfo as FieldInfo;
                var pi = namedArg.MemberInfo as PropertyInfo;

                if (fi != null) {
                    fieldArguments.Add(fi);
                    fieldArgumentValues.Add(namedArg.TypedValue.Value);
                } else if (pi != null) {
                    propertyArguments.Add(pi);
                    propertyArgumentValues.Add(namedArg.TypedValue.Value);
                }
            }

            // apply transformations
            foreach (var transformer in constructorParameterTransformers) {
                int index = constructorArguments.IndexOf(transformer.HolderInfo);
                if (index != -1) {
                    constructorArgumentValues[index] = transformer.Apply(constructorArgumentValues[index]);
                }
            }

            foreach (var transformer in fieldTransformers) {
                int index = fieldArguments.IndexOf(transformer.HolderInfo);
                if (index != -1) {
                    fieldArgumentValues[index] = transformer.Apply(fieldArgumentValues[index]);
                } else {
                    fieldArguments.Add(transformer.HolderInfo);
                    fieldArgumentValues.Add(transformer.Apply(null));
                }
            }

            foreach (var transformer in propertyTransformers) {
                int index = propertyArguments.IndexOf(transformer.HolderInfo);
                if (index != -1) {
                    propertyArgumentValues[index] = transformer.Apply(propertyArgumentValues[index]);
                } else {
                    propertyArguments.Add(transformer.HolderInfo);
                    propertyArgumentValues.Add(transformer.Apply(null));
                }
            }

            return new CustomAttributeBuilder(
              attributeData.Constructor,
              constructorArgumentValues.ToArray(),
              propertyArguments.ToArray(),
              propertyArgumentValues.ToArray(),
              fieldArguments.ToArray(),
              fieldArgumentValues.ToArray());           
        }
    }
}