using System;

namespace Colosoft.Configuration.Tracking.Storage
{
    internal sealed class XmlStoreItem : System.Xml.Serialization.IXmlSerializable
    {
        public string Name { get; set; }

        public object Value { get; set; }

        System.Xml.Schema.XmlSchema System.Xml.Serialization.IXmlSerializable.GetSchema()
        {
            throw new NotImplementedException();
        }

        void System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader reader)
        {
            if (reader.MoveToAttribute("Name"))
            {
                this.Name = reader.ReadContentAsString();
            }

            Type type = null;

            if (reader.MoveToAttribute("Type"))
            {
                var typeString = reader.ReadContentAsString();

                if (!string.IsNullOrEmpty(typeString))
                {
                    type = Type.GetType(typeString, true);
                }
            }

            reader.MoveToElement();

            if (!reader.IsEmptyElement)
            {
                if (type == typeof(byte[]))
                {
                    var content = (string)reader.ReadElementContentAs(typeof(string), null);
                    this.Value = Convert.FromBase64String(content);
                }
                else if (type != null)
                {
                    this.Value = reader.ReadElementContentAs(type, null);
                }
                else
                {
                    this.Value = null;
                }
            }
            else
            {
                reader.Skip();
            }
        }

        void System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter writer)
        {
            writer.WriteAttributeString("Name", this.Name);

            if (this.Value != null)
            {
                object value;

                var valueType = this.Value.GetType();

                if (valueType.IsEnum)
                {
                    valueType = Enum.GetUnderlyingType(valueType);
                    value = Convert.ChangeType(this.Value, valueType, System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    value = this.Value;
                }

                writer.WriteAttributeString("Type", valueType.FullName);

                if (valueType == typeof(byte[]))
                {
                    writer.WriteValue(Convert.ToBase64String((byte[])value));
                }
                else
                {
                    writer.WriteValue(value);
                }
            }
            else
            {
                writer.WriteAttributeString("Type", string.Empty);
            }
        }
    }
}
