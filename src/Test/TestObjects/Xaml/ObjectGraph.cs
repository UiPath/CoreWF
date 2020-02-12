using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using TestObjects.Xaml.GraphCore;
using TestObjects.Xaml.GraphOperations.Builders;

namespace TestObjects.Xaml
{
    [Serializable]
    public class ObjectGraph : GraphNode
    {

        #region Constructors

        public static XName DataAsStrignProperty = XName.Get("DataAsString", "");

        public static XName DataTypeProperty = XName.Get("DataType", "");

        // The data might not be serializable - so we have to lose it we store //
        [NonSerialized]
        object data;

        public ObjectGraph()
            : base("NoName", null)
        {

        }

        public ObjectGraph(object nodeData)
            : base("Root", null)
        {
            this.Data = nodeData;
            this.DataType = nodeData.GetType();
        }


        public ObjectGraph(string name, object nodeData, Type nodeType, IGraphNode parent)
            : base(name, parent)
        {
            this.Data = nodeData;
            this.DataType = nodeType;
        }

        #endregion contructors

        #region Properties
        public object Data
        {
            get { return this.data; }
            set
            {
                this.data = value;
                if (this.data != null)
                {
                    SetValue(ObjectGraph.DataAsStrignProperty, this.data.ToString());
                }
            }
        }
        public string DataAsString
        {
            get
            {
                return (string)GetValue(ObjectGraph.DataAsStrignProperty);
            }
            set
            {
                SetValue(ObjectGraph.DataAsStrignProperty, value);
            }

        }
        public Type DataType
        {
            get { return (Type)GetValue(ObjectGraph.DataTypeProperty); }
            set { SetValue(ObjectGraph.DataTypeProperty, value); }
        }

        #endregion

        public ObjectGraph Clone()
        {
            return ObjectGraphWalker.Create(this.Data);
        }

        public static ObjectGraph Deserialize(string fileName)
        {
            // * XmlSerializer does not support cycles *
            //BinaryFormatter binaryFormatter = new BinaryFormatter();
            //ObjectGraph root = (ObjectGraph)binaryFormatter.Deserialize(PartialTrustFileStream.CreateFileStream(fileName, FileMode.Open));

            //XmlSerializer xmlSerializer = new XmlSerializer(typeof(ObjectGraph));
            //ObjectGraph root = (ObjectGraph)xmlSerializer.Deserialize(PartialTrustFileStream.CreateFileStream(fileName, FileMode.Open));

            // NetDataContractSerializer //
            //FileStream fileStream = PartialTrustFileStream.CreateFileStream(fileName, FileMode.Open);
            var fileStream = new FileStream(fileName, FileMode.Open);
            XmlDictionaryReader xmlDictionaryReader = XmlDictionaryReader.CreateTextReader(fileStream, new XmlDictionaryReaderQuotas());
            //NetDataContractSerializer netDataContractSerializr = new NetDataContractSerializer();
            var dcs = new DataContractSerializer(typeof(ObjectGraph));

            // Deserialize the data and read it from the instance.
            //ObjectGraph root = (ObjectGraph)netDataContractSerializr.ReadObject(xmlDictionaryReader, true);
            ObjectGraph root = (ObjectGraph)dcs.ReadObject(xmlDictionaryReader, true);
            fileStream.Close();

            return root;
        }

        public static void Serialize(ObjectGraph root, string fileName)
        {
            // * XmlSerializer does not support cycles *
            //BinaryFormatter binaryFormatter = new BinaryFormatter();
            //Stream stream = PartialTrustFileStream.CreateFileStream(fileName, FileMode.CreateNew);
            //binaryFormatter.Serialize(stream, root);
            //stream.Close();

            // NetDataContractSerializer //
            //FileStream fileStream = PartialTrustFileStream.CreateFileStream(fileName, FileMode.Create);
            var fileStream = new FileStream(fileName, FileMode.Create);
            XmlDictionaryWriter xmlDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(fileStream);
            //NetDataContractSerializer netDataContractSerializr = new NetDataContractSerializer();
            //netDataContractSerializr.WriteObject(xmlDictionaryWriter, root);
            var dcs = new DataContractSerializer(typeof(ObjectGraph));
            dcs.WriteObject(xmlDictionaryWriter, root);
            xmlDictionaryWriter.Close();

        }
    }
}
