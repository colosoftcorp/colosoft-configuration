using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Colosoft.Configuration.Tracking.Storage
{
    public class XmlFileStore : PersistentStoreBase
    {
        public string FilePath { get; }

        public XmlFileStore(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            this.FilePath = filePath;
        }

        protected override Dictionary<string, object> LoadValues()
        {
            List<XmlStoreItem> storeItems = null;

            if (System.IO.File.Exists(this.FilePath))
            {
                try
                {
                    var serializer = new DataContractSerializer(typeof(List<XmlStoreItem>));
                    using (var inputStream = System.IO.File.OpenRead(this.FilePath))
                    {
                        storeItems = (List<XmlStoreItem>)serializer.ReadObject(inputStream);
                    }
                }
                catch
                {
                    // Ignore
                    storeItems = new List<XmlStoreItem>();
                }
            }
            else
            {
                storeItems = new List<XmlStoreItem>();
            }

            return storeItems.ToDictionary(f => f.Name, f => f.Value);
        }

        protected override void SaveValues(Dictionary<string, object> values)
        {
            var items = values.Select(f => new XmlStoreItem
            {
                Name = f.Key,
                Value = f.Value,
            }).ToList();

            string directory = System.IO.Path.GetDirectoryName(this.FilePath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            using (var outputStream = System.IO.File.Create(this.FilePath))
            {
                var serializer = new DataContractSerializer(typeof(List<XmlStoreItem>));
                serializer.WriteObject(outputStream, items);
                outputStream.Flush();
            }
        }
    }
}
