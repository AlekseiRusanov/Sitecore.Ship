using System;
using System.IO;
using System.Text;
using Sitecore.Install;
using Sitecore.Install.Zip;
using Sitecore.Ship.Core.Contracts;
using Sitecore.Ship.Core.Domain;
using Sitecore.Ship.Core.Services;
using Sitecore.Zip;
using Sitecore.Update.Wizard;
using System.Linq;

namespace Sitecore.Ship.Infrastructure.Install
{
    public class PackageManifestReader : IPackageManifestRepository
    {
        private const string DEPLOY_ONLY_ONCE = "DeployOnlyOnce";
        public PackageManifest GetManifest(string filename)
        {
            var manifest = new PackageManifest();

            ZipReader reader;
            try
            {
                reader = new ZipReader(filename, Encoding.UTF8);
            }
            catch (Exception exception)
            {          
                throw new InvalidOperationException("Failed to open package", exception);
            }

            string tempFileName = Path.GetTempFileName();
            ZipEntry entry = reader.GetEntry("package.zip");
            if (entry != null)
            {
                using (FileStream stream = File.Create(tempFileName))
                {
                    StreamUtil.Copy(entry.GetStream(), stream, 0x4000);
                }
                reader.Dispose();
                reader = new ZipReader(tempFileName, Encoding.UTF8);
            }
            try
            {
                foreach (ZipEntry entry2 in reader.Entries)
                {
                    var data = new ZipEntryData(entry2);

                    var packageManifestEntry = ZipEntryDataParser.GetManifestEntry(data.Key);

                    if (! (packageManifestEntry is PackageManifestEntryNotFound))
                    {
                        manifest.Entries.Add(packageManifestEntry);
                    }
                }
            }
            finally
            {
                reader.Dispose();
                File.Delete(tempFileName);
            }
            SupplyPackageMetadata(manifest, filename);
            return manifest;
        }

        private void SupplyPackageMetadata(PackageManifest packageManifest, string packagePath)
        {
            var error = string.Empty;
            var metadata = PreviewMetadataWizardPage.GetMetadata(packagePath, out error);
            if (!string.IsNullOrEmpty(error))
                return;
            packageManifest.PackageName = metadata.PackageName;
            packageManifest.PackageAttributes = metadata.Attributes
                ?.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries)
                ?.Select(s => s.Split('='))
                ?.Where(s => s.Count() == 2)
                ?.ToDictionary(s => s[0], s => s[1], StringComparer.InvariantCultureIgnoreCase);
            if (packageManifest.PackageAttributes != null && packageManifest.PackageAttributes.ContainsKey(DEPLOY_ONLY_ONCE))
                packageManifest.DeployOnlyOnce = string.Equals(packageManifest.PackageAttributes[DEPLOY_ONLY_ONCE], "true", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}