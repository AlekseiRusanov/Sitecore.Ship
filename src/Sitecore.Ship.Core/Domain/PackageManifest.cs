using System.Collections.Generic;

namespace Sitecore.Ship.Core.Domain
{
    public class PackageManifest
    {
        public PackageManifest()
        {
            Entries = new List<PackageManifestEntry>();
            PackageAttributes = new Dictionary<string, string>();
            DeployOnlyOnce = true;
        }
        
        public List<PackageManifestEntry> Entries { get; private set; }
        public string PackageName { get; set; }
        public bool DeployOnlyOnce { get; set; }
        public Dictionary<string, string> PackageAttributes { get; set; }
        public bool IsDeployed { get; set; }
    }
}