// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace Test.Common.TestObjects.Utilities.Poco
{
    internal class PocoHelper
    {
        public static string SerializeToString(object instance)
        {
            DataContractSerializer serializer = new DataContractSerializer(instance.GetType());
            using (MemoryStream ms = new MemoryStream())
            {
                serializer.WriteObject(ms, instance);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public static object DeSerializeFromString(string instance, Type type)
        {
            DataContractSerializer serializer = new DataContractSerializer(type);
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(instance)))
            {
                return serializer.ReadObject(ms);
            }
        }

        public static void SerializeToFile(object instance, string fileName)
        {
            DataContractSerializer serializer = new DataContractSerializer(instance.GetType());
            //using (FileStream fs = PartialTrustFileStream.CreateFileStream(fileName, FileMode.Create, FileAccess.Write))
            //{
            //    serializer.WriteObject(fs, instance);
            //}
        }

        public static object DeSerializeFromFile(string fileName, Type type)
        {
            DataContractSerializer serializer = new DataContractSerializer(type);
            //using (FileStream fs = PartialTrustFileStream.CreateFileStream(fileName, FileMode.Open, FileAccess.Read))
            //{
            //    return serializer.ReadObject(fs);
            //}
            return null;
        }
    }
}

