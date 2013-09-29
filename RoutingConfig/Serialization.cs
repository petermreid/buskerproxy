using System.Xml;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace RoutingConfig
{
    internal static class GenericDataContractSerializer<T>
    {
        public static string WriteObject(T outputObject)
        {
            StringBuilder sb=new StringBuilder();
            using (StringWriter writer = new StringWriter(sb))
            {
                XmlTextWriter xmlWriter = new XmlTextWriter(writer);
                DataContractSerializer ser = new DataContractSerializer(typeof(T));
                ser.WriteObject(xmlWriter, outputObject);
                xmlWriter.Close();
            }
            return sb.ToString();
        }

        public static T ReadObject(string objectData)
        {
            T deserializedObject = default(T);
            using (StringReader reader = new StringReader(objectData))
            {
                XmlTextReader xmlReader = new XmlTextReader(reader);
                DataContractSerializer ser = new DataContractSerializer(typeof(T));
                deserializedObject = (T)ser.ReadObject(xmlReader, true);
                xmlReader.Close();
            }
            return deserializedObject;
        }
    }
}
