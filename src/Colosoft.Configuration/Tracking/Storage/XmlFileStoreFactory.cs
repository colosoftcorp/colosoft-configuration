using System;

namespace Colosoft.Configuration.Tracking.Storage
{
    public class XmlFileStoreFactory : IStoreFactory
    {
        public string StoreFolderPath { get; set; }

        public XmlFileStoreFactory()
            : this(true)
        {
        }

        public XmlFileStoreFactory(bool perUser)
            : this(ConstructPath(perUser ? Environment.SpecialFolder.ApplicationData : Environment.SpecialFolder.CommonApplicationData))
        {
        }

        public XmlFileStoreFactory(Environment.SpecialFolder folder)
            : this(Environment.GetFolderPath(folder))
        {
        }

        public XmlFileStoreFactory(string storeFolderPath)
        {
            this.StoreFolderPath = storeFolderPath;
        }

        private static string ConstructPath(Environment.SpecialFolder baseFolder)
        {
            string companyPart = string.Empty;
            string appNamePart = string.Empty;

            var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                var companyAttribute = (System.Reflection.AssemblyCompanyAttribute)Attribute.GetCustomAttribute(entryAssembly, typeof(System.Reflection.AssemblyCompanyAttribute));
                if (!string.IsNullOrEmpty(companyAttribute.Company))
                {
                    companyPart = $"{companyAttribute.Company}\\";
                }

                var titleAttribute = (System.Reflection.AssemblyTitleAttribute)Attribute.GetCustomAttribute(entryAssembly, typeof(System.Reflection.AssemblyTitleAttribute));
                if (!string.IsNullOrEmpty(titleAttribute.Title))
                {
                    appNamePart = $"{titleAttribute.Title}\\";
                }
            }

            return System.IO.Path.Combine(Environment.GetFolderPath(baseFolder), $@"{companyPart}{appNamePart}");
        }

        public IStore CreateStoreForObject(string objectId)
        {
            return new XmlFileStore(System.IO.Path.Combine(this.StoreFolderPath, $"{objectId}.xml"));
        }
    }
}
