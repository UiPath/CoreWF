// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Test.Common.Configurers
{
    /// <summary>
    /// Interface used internally in common code to refer to configurers without knowing what types those configurers extend from. 
    ///  Testers should most likely never need to use this interface.
    ///
    /// Cant be made internal, since it fails to build.
    /// 
    /// The exception is in the case where you want to create a base class for configurers all of which should be treated exactly the same. 
    /// </summary>
    public interface IConfigurer
    {
    }


    /// <summary>
    /// Interface which defines a configurer that can be applied to some type of object (T). This wrapper is used so that we can represent a single
    ///  configuration task in a way that can be applied anywhere. Specifically, the issue is that we need a serializable way to represent these 
    ///  configuration steps, so that they can be shipped to a remote app domain and run there.
    ///
    ///  Note that these configurers should ideally always be public, so that they can be reused as best as possible by repros, etc.
    ///  Also, best practice is to log out the task which the configurer is doing, with the [Configuration] prefix so that its easy 
    ///  to see from test logs what was done to the target object.
    /// 
    ///  TestSettings should never be modified from inside of configure calls.
    ///
    /// All Configurers should be marked as // [Serializable], so that they can be serialized and sent to additional app domains, etc.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConfigurer<T> : IConfigurer
    {
        void Configure(T target, TestSettings settings);
    }

    /// <summary>
    /// The ConfigurationOption enum lets some Configurers specify multiple ways of configuring something, 
    ///   and then let the tester choose which to use, while exposing the same API.
    ///   Programmatic means that it will be added programatically, while ConfigurationFile means that it will
    ///   be configured through a *.config file
    ///
    /// Note that not all Configurers support these options, some only support one method of configuration and some dont use this at all.
    /// </summary>
    public enum ConfigurationOption
    {
        Programmatic,
        ConfigurationFile,
    }
}
