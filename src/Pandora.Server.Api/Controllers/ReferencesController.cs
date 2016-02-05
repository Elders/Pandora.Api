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
    public class ReferencesController : Controller
    {
        static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(ReferencesController));

        IAuthorizationService authorizationService;

        public ReferencesController(IAuthorizationService authorizationService)
        {
            this.authorizationService = authorizationService;
        }

        [HttpGet("{projectName}/{configurationName}")]
        public async Task<IEnumerable<string>> Get(string projectName, string configurationName)
        {
            try
            {
                var references = new List<string>();

                if (await authorizationService.AuthorizeAsync(User,
                     new Resource() { ProjectName = projectName, ConfigurationName = configurationName, Access = ViewModels.Access.ReadAcccess },
                     new OperationAuthorizationRequirement() { Name = "Read" }))
                {
                    var projectPath = Path.Combine(Folders.Projects, projectName);

                    var configurationPath = GetConfigurationFile(projectName, configurationName);

                    var jar = JsonConvert.DeserializeObject<Jar>(System.IO.File.ReadAllText(configurationPath));

                    foreach (var reference in jar.References)
                    {
                        references.Add(reference.Values.First());
                    }
                }

                return references;
            }
            catch (Exception ex)
            {
                log.Fatal(ex);

                throw;
            }
        }

        [HttpPost("{projectName}/{configurationName}")]
        public async void Post(string projectName, string configurationName, string referenceName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(projectName) || string.IsNullOrWhiteSpace(configurationName))
                    throw new InvalidOperationException();

                if (await authorizationService.AuthorizeAsync(User,
                    new Resource() { ProjectName = projectName, ConfigurationName = configurationName, Access = ViewModels.Access.WriteAccess },
                    new OperationAuthorizationRequirement() { Name = "Write" }))
                {
                    var projectPath = Path.Combine(Folders.Projects, projectName);

                    if (configurationName.EndsWith(".json", StringComparison.Ordinal) == false)
                        configurationName += ".json";

                    var configurationPath = GetConfigurationFile(projectName, configurationName);

                    if (System.IO.File.Exists(configurationPath) == false)
                        throw new InvalidOperationException("There is no configuration file: " + configurationName);

                    var referencePath = GetConfigurationFile(projectName, referenceName);
                    if (System.IO.File.Exists(referencePath) == false)
                        throw new InvalidOperationException("There is no configuration file: " + referenceName);

                    if (await authorizationService.AuthorizeAsync(User,
                        new Resource() { ProjectName = projectName, ConfigurationName = referenceName, Access = ViewModels.Access.ReadAcccess },
                        new OperationAuthorizationRequirement() { Name = "Read" }))
                    {
                        var cfg = JsonConvert.DeserializeObject<Jar>(System.IO.File.ReadAllText(configurationPath));

                        var box = Box.Box.Mistranslate(cfg);

                        var existingReference = box.References.FirstOrDefault(x => x.ContainsValue(referenceName));

                        if (existingReference == null)
                        {
                            box.References.Add(new Dictionary<string, string>() { { "jar", referenceName } });

                            var jar = JsonConvert.SerializeObject(Box.Box.Mistranslate(box), Formatting.Indented);

                            System.IO.File.WriteAllText(configurationPath, jar);

                            var nameClaim = ClaimsPrincipal.Current.Identities.First().Claims.SingleOrDefault(x => x.Type == "name");
                            var username = nameClaim != null ? nameClaim.Value : "no name claim";
                            var emailClaim = ClaimsPrincipal.Current.Identities.First().Claims.SingleOrDefault(x => x.Type == "email");
                            var email = emailClaim != null ? emailClaim.Value : "no email claim";
                            var message = "Added new reference " + referenceName + " to configuration: " + cfg.Name + " in " + projectName;

                            var git = new Git(projectPath);
                            git.Stage(new List<string>() { configurationPath });
                            git.Commit(message, username, email);
                            git.Push();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Fatal(ex);
                throw;
            }
        }

        [HttpDelete("{projectName}/{configurationName}/{referenceName}")]
        public async void Delete(string projectName, string configurationName, string referenceName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(projectName) || string.IsNullOrWhiteSpace(configurationName))
                    throw new InvalidOperationException();

                if (await authorizationService.AuthorizeAsync(User,
                    new Resource() { ProjectName = projectName, ConfigurationName = configurationName, Access = ViewModels.Access.WriteAccess },
                    new OperationAuthorizationRequirement() { Name = "Write" }))
                {
                    var projectPath = Path.Combine(Folders.Projects, projectName);

                    if (configurationName.EndsWith(".json", StringComparison.Ordinal) == false)
                        configurationName += ".json";

                    var configurationPath = GetConfigurationFile(projectName, configurationName);
                    if (System.IO.File.Exists(configurationPath) == false)
                        throw new InvalidOperationException("There is no configuration file: " + configurationName);

                    var cfg = JsonConvert.DeserializeObject<Jar>(System.IO.File.ReadAllText(configurationPath));

                    var box = Box.Box.Mistranslate(cfg);

                    var referenceForDelete = box.References.FirstOrDefault(x => x.ContainsValue(referenceName));

                    if (referenceForDelete != null)
                    {
                        box.References.Remove(referenceForDelete);

                        var jar = JsonConvert.SerializeObject(Box.Box.Mistranslate(box), Formatting.Indented);

                        System.IO.File.WriteAllText(configurationPath, jar);

                        var nameClaim = ClaimsPrincipal.Current.Identities.First().Claims.SingleOrDefault(x => x.Type == "name");
                        var username = nameClaim != null ? nameClaim.Value : "no name claim";
                        var emailClaim = ClaimsPrincipal.Current.Identities.First().Claims.SingleOrDefault(x => x.Type == "email");
                        var email = emailClaim != null ? emailClaim.Value : "no email claim";
                        var message = "Removed reference " + referenceName + " from configuration: " + cfg.Name + " in " + projectName;

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
