using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Elders.Pandora.Box;
using Newtonsoft.Json;

namespace Elders.Pandora.Server.Api.ViewModels
{
    public class ConfigurationDTO
    {
        private readonly Box.Box box;

        private readonly Pandora pandora;

        private readonly ClaimsPrincipal user;

        public ConfigurationDTO(ClaimsPrincipal user, Jar jar, string projectName)
        {
            this.user = user;
            box = Box.Box.Mistranslate(jar);
            pandora = new Pandora(box);
            SecurityAccess = GetSecurityAccess();
            ApplicationName = jar.Name;
            ProjectName = projectName;
            Defaults = GetDefaults();
            Clusters = GetAllClusters();

            Machines = new List<MachineDTO>();
            foreach (var cluster in Clusters)
            {
                var machines = GetAllMachines(cluster.Cluster.Name);

                Machines.AddRange(machines);
            }
        }

        public string ProjectName { get; set; }

        public string ApplicationName { get; set; }

        public SecurityAccess SecurityAccess { get; set; }

        public List<ClusterDTO> Clusters { get; set; }

        public List<MachineDTO> Machines { get; set; }

        public DefaultsDTO Defaults { get; set; }

        private List<ClusterDTO> GetAllClusters()
        {
            if (!this.HasAccess())
                return new List<ClusterDTO>();

            var clusters = new List<ClusterDTO>();

            foreach (var env in SecurityAccess.Projects.SingleOrDefault(x => x.Name == this.ProjectName).Applications.SingleOrDefault(x => x.Name == ApplicationName).Clusters)
            {
                if (box.Clusters.Select(x => x.Name).Any(x => x == env.Name))
                {
                    clusters.Add(new ClusterDTO(env, pandora.Open(new PandoraOptions(env.Name, string.Empty, true)).AsDictionary()));
                }
            }

            return clusters;
        }

        private List<MachineDTO> GetAllMachines(string clusterName)
        {
            var cluster = SecurityAccess.Projects
                .SingleOrDefault(x => x.Name == ProjectName).Applications
                .SingleOrDefault(x => x.Name == ApplicationName).Clusters
                .SingleOrDefault(x => x.Name == clusterName);

            if (clusterName == null)
                return new List<MachineDTO>();

            if (!this.HasAccess(clusterName))
                return new List<MachineDTO>();

            var pandora = new Pandora(box);

            var machines = new List<MachineDTO>();

            foreach (var machine in box.Machines)
            {
                machines.Add(new MachineDTO(machine.Name, cluster, pandora.Open(new PandoraOptions(clusterName, machine.Name, true)).AsDictionary()));
            }

            return machines;
        }

        private DefaultsDTO GetDefaults()
        {
            var app = SecurityAccess.Projects.SingleOrDefault(x => x.Name == this.ProjectName).Applications.SingleOrDefault(x => x.Name == this.ApplicationName);

            if (!app.Access.HasAccess(Access.ReadAcccess))
                return new DefaultsDTO(app, new Dictionary<string, string>());

            return new DefaultsDTO(app, box.Defaults.AsDictionary());
        }

        private SecurityAccess GetSecurityAccess()
        {
            var securityAccessClaim = user.Claims.SingleOrDefault(x => x.Type == "SecurityAccess");

            SecurityAccess security;

            if (securityAccessClaim == null || string.IsNullOrWhiteSpace(securityAccessClaim.Value))
                security = new SecurityAccess();
            else
                security = JsonConvert.DeserializeObject<SecurityAccess>(securityAccessClaim.Value);

            if (security == null)
                security = new SecurityAccess();

            return security;
        }

        private bool HasAccess()
        {
            if (SecurityAccess.Projects.Any(x => x.Name == this.ProjectName) && SecurityAccess.Projects.SingleOrDefault(x => x.Name == this.ProjectName).Applications.Any(x => x.Name == this.ApplicationName))
                return true;

            else
                return false;
        }

        private bool HasAccess(string cluster)
        {
            if (this.HasAccess())
            {
                if (SecurityAccess
                    .Projects.SingleOrDefault(x => x.Name == this.ProjectName)
                    .Applications.SingleOrDefault(x => x.Name == this.ApplicationName)
                    .Clusters.Any(x => x.Name == cluster))
                    return true;

                else
                    return false;
            }
            else
                return false;
        }

        public class MachineDTO
        {
            public MachineDTO(string name, Cluster cluster, Dictionary<string, string> settings)
            {
                Name = name;
                Cluster = cluster;
                Settings = settings;
            }

            public string Name { get; set; }

            public Cluster Cluster { get; set; }

            public Dictionary<string, string> Settings { get; set; }

            public string this[string settingName]
            {
                get
                {
                    string value = String.Empty;
                    if (Settings.TryGetValue(settingName.ToLowerInvariant(), out value))
                    {
                        return value;
                    }
                    else
                    {
                        throw new System.Collections.Generic.KeyNotFoundException("SettingName does not exist in the collection");
                    }
                }
            }
        }

        public class ClusterDTO
        {
            public ClusterDTO(Cluster cluster, Dictionary<string, string> settings)
            {
                Cluster = cluster;
                Settings = settings;
            }

            public Cluster Cluster { get; set; }

            public Dictionary<string, string> Settings { get; set; }

            public string this[string settingName]
            {
                get
                {
                    string value = String.Empty;
                    if (Settings.TryGetValue(settingName.ToLowerInvariant(), out value))
                    {
                        return value;
                    }
                    else
                    {
                        throw new System.Collections.Generic.KeyNotFoundException("SettingName does not exist in the collection");
                    }
                }
            }
        }

        public class DefaultsDTO
        {
            public DefaultsDTO(Application application, Dictionary<string, string> settings)
            {
                Application = application;
                Settings = settings;
            }

            public Application Application { get; set; }

            public Dictionary<string, string> Settings { get; set; }

            public string this[string settingName]
            {
                get
                {
                    string value = String.Empty;
                    if (Settings.TryGetValue(settingName.ToLowerInvariant(), out value))
                    {
                        return value;
                    }
                    else
                    {
                        throw new System.Collections.Generic.KeyNotFoundException("SettingName does not exist in the collection");
                    }
                }
            }
        }
    }
}
