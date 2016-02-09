using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Elders.Pandora.Box;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Authorization.Infrastructure;
using Microsoft.AspNet.Mvc;
using Newtonsoft.Json;

namespace Elders.Pandora.Server.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class MachinesController : Controller
    {
        static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(MachinesController));

        IAuthorizationService authorizationService;

        public MachinesController(IAuthorizationService authorizationService)
        {
            this.authorizationService = authorizationService;
        }

        [HttpGet("ListMachines/{projectName}/{configurationName}/{clusterName}")]
        public async Task<IEnumerable<string>> Get(string projectName, string configurationName, string clusterName)
        {
            if (string.IsNullOrWhiteSpace(configurationName) || string.IsNullOrWhiteSpace(projectName) || string.IsNullOrWhiteSpace(clusterName))
                return null;

            var projectPath = Path.Combine(Folders.Projects, projectName);

            var configurationPath = GetConfigurationFile(projectName, configurationName);

            var cfg = JsonConvert.DeserializeObject<Jar>(System.IO.File.ReadAllText(configurationPath));

            var box = Box.Box.Mistranslate(cfg);

            var machines = new List<string>();

            foreach (var machine in box.Machines)
            {
                if (await authorizationService.AuthorizeAsync(User,
                  new Resource() { ProjectName = projectName, ConfigurationName = configurationName, ClusterName = clusterName, MachineName = machine.Name, Access = ViewModels.Access.ReadAcccess },
                  new OperationAuthorizationRequirement() { Name = "Read" }))
                {
                    machines.Add(machine.Name);
                }
            }

            return machines;
        }

        [HttpGet("{projectName}/{configurationName}/{clusterName}/{machineName}")]
        public async Task<Dictionary<string, string>> Get(string projectName, string configurationName, string clusterName, string machineName)
        {
            if (string.IsNullOrWhiteSpace(configurationName) || string.IsNullOrWhiteSpace(projectName) || string.IsNullOrWhiteSpace(clusterName) || string.IsNullOrWhiteSpace(machineName))
                return null;

            if (await authorizationService.AuthorizeAsync(User,
                  new Resource() { ProjectName = projectName, ConfigurationName = configurationName, ClusterName = clusterName, MachineName = machineName, Access = ViewModels.Access.ReadAcccess },
                  new OperationAuthorizationRequirement() { Name = "Read" }))
            {
                var projectPath = Path.Combine(Folders.Projects, projectName);

                var configurationPath = GetConfigurationFile(projectName, configurationName);

                var cfg = JsonConvert.DeserializeObject<Jar>(System.IO.File.ReadAllText(configurationPath));

                var box = Box.Box.Mistranslate(cfg);

                var pandora = new Pandora(box);

                var cluster = box.Clusters.SingleOrDefault(x => x.Name == clusterName);

                var machine = box.Machines.SingleOrDefault(x => x.Name == machineName);

                var clusterSettings = cluster.AsDictionary();

                var defaults = box.Defaults.AsDictionary();

                var settings = machine.AsDictionary();

                foreach (var setting in defaults)
                {
                    if (clusterSettings.ContainsKey(setting.Key) == false)
                    {
                        clusterSettings.Add(setting.Key, setting.Value);
                    }
                }

                foreach (var setting in settings)
                {
                    clusterSettings[setting.Key] = setting.Value;
                }

                return clusterSettings;
            }

            return null;
        }

        [HttpPost("{projectName}/{configurationName}/{machineName}")]
        public async void Post(string projectName, string configurationName, string machineName, [FromBody]string value)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(configurationName) || string.IsNullOrWhiteSpace(projectName) || string.IsNullOrWhiteSpace(machineName))
                    return;

                if (await authorizationService.AuthorizeAsync(User,
                    new Resource() { ProjectName = projectName, ConfigurationName = configurationName, Access = ViewModels.Access.WriteAccess },
                    new OperationAuthorizationRequirement() { Name = "Write" }))
                {
                    var projectPath = Path.Combine(Folders.Projects, projectName);

                    var configurationPath = GetConfigurationFile(projectName, configurationName);

                    var cfg = JsonConvert.DeserializeObject<Jar>(System.IO.File.ReadAllText(configurationPath));

                    var box = Box.Box.Mistranslate(cfg);

                    var settings = JsonConvert.DeserializeObject<Dictionary<string, string>>(value);

                    var newMachine = new Machine(machineName, settings);

                    var machines = box.Machines.ToList();

                    if (!machines.Any(x => x.Name == newMachine.Name))
                    {
                        machines.Add(newMachine);

                        box.Machines = machines;

                        var jar = JsonConvert.SerializeObject(Box.Box.Mistranslate(box), Formatting.Indented);

                        System.IO.File.WriteAllText(configurationPath, jar);

                        var nameClaim = ClaimsPrincipal.Current.Identities.First().Claims.SingleOrDefault(x => x.Type == "name");
                        var username = nameClaim != null ? nameClaim.Value : "no name claim";
                        var emailClaim = ClaimsPrincipal.Current.Identities.First().Claims.SingleOrDefault(x => x.Type == "email");
                        var email = emailClaim != null ? emailClaim.Value : "no email claim";
                        var message = "Added new machine " + newMachine.Name + " in " + configurationName + " in " + projectName;

                        var git = new Git(projectPath);
                        git.Stage(new List<string>() { configurationPath });
                        git.Commit(message, username, email);
                        git.Push();
                    }
                }
            }
            catch (Exception ex)
            {
                log.Fatal(ex);
                throw;
            }
        }

        [HttpPut("{projectName}/{configurationName}/{machineName}")]
        public async void Put(string projectName, string configurationName, string machineName, [FromBody]string value)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(configurationName) || string.IsNullOrWhiteSpace(projectName) || string.IsNullOrWhiteSpace(machineName))
                    return;

                if (await authorizationService.AuthorizeAsync(User,
                    new Resource() { ProjectName = projectName, ConfigurationName = configurationName, Access = ViewModels.Access.WriteAccess },
                    new OperationAuthorizationRequirement() { Name = "Write" }))
                {
                    var projectPath = Path.Combine(Folders.Projects, projectName);

                    var configurationPath = GetConfigurationFile(projectName, configurationName);

                    var cfg = JsonConvert.DeserializeObject<Jar>(System.IO.File.ReadAllText(configurationPath));

                    var box = Box.Box.Mistranslate(cfg);

                    var settings = JsonConvert.DeserializeObject<Dictionary<string, string>>(value);

                    var newMachine = new Machine(machineName, settings);

                    var machines = box.Machines.ToList();

                    var existing = machines.FirstOrDefault(x => x.Name == newMachine.Name);

                    if (existing != null)
                    {
                        machines.Remove(existing);

                        machines.Add(newMachine);

                        box.Machines = machines;

                        var jar = JsonConvert.SerializeObject(Box.Box.Mistranslate(box), Formatting.Indented);

                        System.IO.File.WriteAllText(configurationPath, jar);

                        var nameClaim = ClaimsPrincipal.Current.Identities.First().Claims.SingleOrDefault(x => x.Type == "name");
                        var username = nameClaim != null ? nameClaim.Value : "no name claim";
                        var emailClaim = ClaimsPrincipal.Current.Identities.First().Claims.SingleOrDefault(x => x.Type == "email");
                        var email = emailClaim != null ? emailClaim.Value : "no email claim";
                        var message = "Updated machine " + newMachine.Name + " in " + configurationName + " in " + projectName;

                        var git = new Git(projectPath);
                        git.Stage(new List<string>() { configurationPath });
                        git.Commit(message, username, email);
                        git.Push();
                    }
                }
            }
            catch (Exception ex)
            {
                log.Fatal(ex);
                throw;
            }
        }

        [HttpDelete("{projectName}/{configurationName}/{machineName}")]
        public async void Delete(string projectName, string configurationName, string machineName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(configurationName) || string.IsNullOrWhiteSpace(projectName) || string.IsNullOrWhiteSpace(machineName))
                    return;

                if (await authorizationService.AuthorizeAsync(User,
                    new Resource() { ProjectName = projectName, ConfigurationName = configurationName, Access = ViewModels.Access.WriteAccess },
                    new OperationAuthorizationRequirement() { Name = "Write" }))
                {
                    var projectPath = Path.Combine(Folders.Projects, projectName);

                    var configurationPath = GetConfigurationFile(projectName, configurationName);

                    var cfg = JsonConvert.DeserializeObject<Jar>(System.IO.File.ReadAllText(configurationPath));

                    var box = Box.Box.Mistranslate(cfg);

                    var machines = box.Machines.ToList();

                    var existing = machines.FirstOrDefault(x => x.Name == machineName);

                    if (existing != null)
                    {
                        machines.Remove(existing);

                        box.Machines = machines;

                        var jar = JsonConvert.SerializeObject(Box.Box.Mistranslate(box));

                        System.IO.File.WriteAllText(configurationPath, jar);

                        var nameClaim = ClaimsPrincipal.Current.Identities.First().Claims.SingleOrDefault(x => x.Type == "name");
                        var username = nameClaim != null ? nameClaim.Value : "no name claim";
                        var emailClaim = ClaimsPrincipal.Current.Identities.First().Claims.SingleOrDefault(x => x.Type == "email");
                        var email = emailClaim != null ? emailClaim.Value : "no email claim";
                        var message = "Removed machine " + existing.Name + " from " + configurationName + " in " + projectName;

                        var git = new Git(projectPath);
                        git.Stage(new List<string>() { configurationPath });
                        git.Commit(message, username, email);
                        git.Push();
                    }
                }
            }
            catch (Exception ex)
            {
                log.Fatal(ex);
                throw;
            }
        }

        private string GetConfigurationFile(string projectName, string configurationName)
        {
            if (string.IsNullOrWhiteSpace(configurationName) || string.IsNullOrWhiteSpace(projectName))
                return null;

            var configurationPath = Path.Combine(Folders.Projects, projectName, "src", projectName + ".Configuration", "public", configurationName);

            if (configurationPath.EndsWith(".json", StringComparison.Ordinal) == false)
                configurationPath += ".json";

            if (System.IO.File.Exists(configurationPath) == false)
                throw new InvalidOperationException("There is no configuration file: " + configurationName);

            return configurationPath;
        }
    }
}
