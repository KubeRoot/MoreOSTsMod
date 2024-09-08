using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;

namespace MoreOSTs.Serialization
{
    public struct settings
    {
        [DefaultValue(false)]
        public bool loop;
        public song[] music;

        public static settings Deserialize(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(settings));
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            settings result = (settings)serializer.Deserialize(fs);

            return result;
        }

        public static void Serialize(string path, settings input)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(settings));
            FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
            
            serializer.Serialize(fs, input);
        }
    }

    public struct song
    {
        [XmlAttribute]
        public string name;
        [XmlAttribute]
        public string scenes;
        [XmlAttribute]
        [DefaultValue(false)]
        public bool boss;
        [XmlAttribute]
        [DefaultValue(0.5)]
        public float volume;
        [XmlAttribute]
        [DefaultValue(false)]
        public bool loop;
    }
}