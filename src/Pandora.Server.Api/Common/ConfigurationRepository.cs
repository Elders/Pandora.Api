using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using Elders.Pandora.Box;
using Elders.Pandora.Server.Api.ViewModels;
using Newtonsoft.Json;

namespace Elders.Pandora.Server.Api.Common
{
    public class ConfigurationRepository
    {
        private readonly SecurityAccess access;

        public ConfigurationRepository(ClaimsPrincipal user)
        {
            this.access = GetSecurityAccess(user);
        }

        public string GetConfigurationFile(string projectName, string configurationName)
        {
            if (string.IsNullOrWhiteSpace(configurationName) || string.IsNullOrWhiteSpace(projectName))
                return null;

            var configurationPath = Path.Combine(Folders.Projects, projectName, "src", projectName + ".Configuration", "public", configurationName);

            if (configurationPath.EndsWith(".json", StringComparison.Ordinal) == false)
                configurationPath += ".json";

            if (File.Exists(configurationPath) == false)
                throw new InvalidOperationException("There is no configuration file: " + configurationName);

            return configurationPath;
        }

        public Jar GetConfiguration(string projectName, string configurationName)
        {
            var configFile = GetConfigurationFile(projectName, configurationName);

            var jar = JsonConvert.DeserializeObject<Jar>(File.ReadAllText(configFile));

            var app = access.Projects.SingleOrDefault(x => x.Name == projectName).Applications.SingleOrDefault(x => x.Name == configurationName);

            if (app.Access.HasAccess(Access.ReadAcccess) == false)
                return null;

            foreach (var cluster in jar.Clusters)
            {

            }

            return null;
        }

        public Configuration GetConfigurationForCluster(string projectName, string configurationName, string clusterName)
        {
            var configFile = GetConfigurationFile(projectName, configurationName);

            var jar = JsonConvert.DeserializeObject<Jar>(File.ReadAllText(configFile));

            var project = access.Projects.SingleOrDefault(x => x.Name == projectName);
            if (project == null)
                return null;

            var application = project.Applications.SingleOrDefault(x => x.Name == configurationName);
            if (application == null)
                return null;

            var cluster = application.Clusters.SingleOrDefault(x => x.Name == clusterName);
            if (cluster == null)
                return null;

            if (cluster.Access.HasAccess(Access.ReadAcccess) == false)
                return null;

            var box = Box.Box.Mistranslate(jar);

            var pandora = new Pandora(box);

            var configuration = pandora.Open(new PandoraOptions(clusterName, string.Empty, true));

            return configuration;
        }

        public Configuration GetConfigurationForMachine(string projectName, string configurationName, string clusterName, string machineName)
        {
            var configFile = GetConfigurationFile(projectName, configurationName);

            var jar = JsonConvert.DeserializeObject<Jar>(File.ReadAllText(configFile));

            var project = access.Projects.SingleOrDefault(x => x.Name == projectName);
            if (project == null)
                return null;

            var application = project.Applications.SingleOrDefault(x => x.Name == configurationName);
            if (application == null)
                return null;

            var cluster = application.Clusters.SingleOrDefault(x => x.Name == clusterName);
            if (cluster == null)
                return null;

            if (cluster.Access.HasAccess(Access.ReadAcccess) == false)
                return null;

            var box = Box.Box.Mistranslate(jar);

            var pandora = new Pandora(box);

            var configuration = pandora.Open(new PandoraOptions(clusterName, machineName, true));

            return configuration;
        }

        private SecurityAccess GetSecurityAccess(ClaimsPrincipal user)
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
    }
}
