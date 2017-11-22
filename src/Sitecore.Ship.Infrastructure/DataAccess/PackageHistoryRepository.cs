using System;
using System.Collections.Generic;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;
using Sitecore.Ship.Core.Contracts;
using Sitecore.Ship.Core.Domain;
using Sitecore.Update.Metadata;
using System.Linq;
using Sitecore.Data;

namespace Sitecore.Ship.Infrastructure.DataAccess
{
    public class PackageHistoryRepository : IPackageHistoryRepository
    {
        private const string HISTORY_FOLDER_PATH = "/sitecore/content/PackageHistory";
        private const string PACKAGE_HISTORY_TEMPLATE_PATH = "SitecoreShip/InstalledPackage";
        private const string GLOBAL_PACKAGE_INSTALLATION_HISTORY_PATH = "/sitecore/system/Packages/Installation history";
        private const string GLOBAL_PACKAGE_NAME_FIELD = "Package name";
        private readonly ID GLOBAL_PACKAGE_REGISTRATION_TEMPLATE_ID = new ID("{22A11D20-5F1D-4216-BF3F-18C016F1F98E}");
        private const string DATABASE_NAME = "master";
        //private const string ONLY_ONCE
        private const string CORE_DATABASE_NAME = "core";
        private const string PACKAGE_ID_FIELD_NAME = "PackageId";
        private const string DATE_INSTALLED_FIELD_NAME = "DateInstalled";
        private const string DESCRIPTION_FIELD_NAME = "Description";

        public void Add(InstalledPackage package)
        {
            using (new SecurityDisabler())
            {
                // TODO how does this behave if the package has not been installed?

                var database = Factory.GetDatabase(DATABASE_NAME);
                var rootItem = database.GetItem(HISTORY_FOLDER_PATH);

                TemplateItem template = database.GetTemplate(PACKAGE_HISTORY_TEMPLATE_PATH);
                var item = rootItem.Add(package.PackageId, template);

                try
                {
                    item.Editing.BeginEdit();
                    item.Fields[PACKAGE_ID_FIELD_NAME].Value = package.PackageId;
                    item.Fields[DATE_INSTALLED_FIELD_NAME].Value = package.DateInstalled.ToString();
                    item.Fields[DESCRIPTION_FIELD_NAME].Value = package.Description;
                }
                finally
                {
                    item.Editing.EndEdit();
                }
            }
        }

        public List<InstalledPackage> GetAll()
        {
            var entries = new List<InstalledPackage>();

            var rootItem = GetRootItem();

            if (rootItem != null)
            {
                foreach (Item child in rootItem.Children)
                {
                    entries.Add(new InstalledPackage()
                        {
                            DateInstalled = DateTime.Parse(child.Fields[DATE_INSTALLED_FIELD_NAME].Value),
                            PackageId = child.Fields[PACKAGE_ID_FIELD_NAME].Value,
                            Description = child.Fields[DESCRIPTION_FIELD_NAME].Value
                        });
                }
            }

            return entries;
        }

        public bool IsPackageInstalled(string packageName)
        {
            using (new SecurityDisabler())
            {
                //Using the history log is not reliable - does not provide for the case when the package is installed manually
                //Check in the global log (should always work, even if installed manually):
                var rootItem = GetGlobalHistoryItem();
                if (rootItem == null)
                    return false;
                var installationItems = rootItem.Axes.GetDescendants().Where(item => item.TemplateID == GLOBAL_PACKAGE_REGISTRATION_TEMPLATE_ID).ToList();
                return installationItems.Any(item => item.Fields[GLOBAL_PACKAGE_NAME_FIELD]?.Value == packageName);
            }
        }

        private static Item GetGlobalHistoryItem()
        {
            var database = Factory.GetDatabase(CORE_DATABASE_NAME);
            var rootItem = database.GetItem(GLOBAL_PACKAGE_INSTALLATION_HISTORY_PATH);
            return rootItem;
        }

        private static Item GetRootItem()
        {
            var database = Factory.GetDatabase(DATABASE_NAME);
            var rootItem = database.GetItem(HISTORY_FOLDER_PATH);
            return rootItem;
        }
    }
}
