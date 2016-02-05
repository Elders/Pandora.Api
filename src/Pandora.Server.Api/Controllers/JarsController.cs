﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Elders.Pandora.Box;
using Elders.Pandora.Server.Api.Common;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Authorization.Infrastructure;
using Microsoft.AspNet.Mvc;
using Newtonsoft.Json;

namespace Elders.Pandora.Server.Api.Controllers
{
    [Route("api/[controller]")]
    public class JarsController : Controller
    {
        static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(JarsController));

        IAuthorizationService authorizationService;

        public JarsController(IAuthorizationService authorizationService)
        {
            this.authorizationService = authorizationService;
        }

        [Authorize]
        [HttpGet("ListJars/{projectName}")]
        public async Task<IEnumerable<string>> ListJars(string projectName)
        {
            string projectPath = Path.Combine(Folders.Projects, projectName, "src", projectName + ".Configuration", "public");

            var configurations = Directory.GetFiles(projectPath).Where(x => x.EndsWith(".json", StringComparison.Ordinal));

            var jars = new List<string>();

            foreach (var config in configurations)
            {
                var fileInfo = new FileInfo(config);

                var configName = fileInfo.Name.Replace(fileInfo.Extension, string.Empty);

                if (await authorizationService.AuthorizeAsync(User,
                    new Resource() { ProjectName = projectName, ConfigurationName = configName, Access = ViewModels.Access.ReadAcccess },
                    new OperationAuthorizationRequirement() { Name = "Read" }))
                {
                    jars.Add(configName);
                }
            }

            return jars;
        }

        [Authorize(Roles = "superAdmin")]
        [HttpGet("{projectName}")]
        public IEnumerable<Jar> Get(string projectName)
        {
            string projectPath = Path.Combine(Folders.Projects, projectName, "src", projectName + ".Configuration", "public");

            var configurations = Directory.GetFiles(projectPath).Where(x => x.EndsWith(".json", StringComparison.Ordinal));

            foreach (var config in configurations)
            {
                Jar jarObject = null;

                try
                {
                    jarObject = JsonConvert.DeserializeObject<Jar>(System.IO.File.ReadAllText(config));
                }
                catch (Exception ex)
                {
                    log.Error(ex);

                    continue;
                }

                if (config != null)
                    yield return jarObject;
            }
        }

        [Authorize(Roles = "superAdmin")]
        [HttpGet("{projectName}/{configurationName}")]
        public Jar Get(string projectName, string configurationName)
        {
            try
            {
                var projectPath = Path.Combine(Folders.Projects, projectName);

                var configurationPath = GetConfigurationFile(projectName, configurationName);

                var jar = JsonConvert.DeserializeObject<Jar>(System.IO.File.ReadAllText(configurationPath));

                return jar;
            }
            catch (Exception ex)
            {
                log.Fatal(ex);

                throw;
            }
        }

        [Authorize(Roles = "superAdmin")]
        [HttpPost("{projectName}/{configurationName}")]
        public void Post(string projectName, string configurationName, [FromBody]string value)
        {
            try
            {
                var cfg = JsonConvert.DeserializeObject<Jar>(value);

                if (string.IsNullOrWhiteSpace(projectName) || string.IsNullOrWhiteSpace(configurationName))
                    throw new InvalidOperationException();

                var projectPath = Path.Combine(Folders.Projects, projectName);

                if (configurationName.EndsWith(".json", StringComparison.Ordinal) == false)
                    configurationName += ".json";

                var configurationPath = GetConfigurationFile(projectName, configurationName);

                if (System.IO.File.Exists(configurationPath))
                    throw new InvalidOperationException("There is already a configuration file: " + configurationName);

                var box = Box.Box.Mistranslate(cfg);

                var jar = JsonConvert.SerializeObject(Box.Box.Mistranslate(box), Formatting.Indented);

                System.IO.File.WriteAllText(configurationPath, jar);

                var nameClaim = ClaimsPrincipal.Current.Identities.First().Claims.SingleOrDefault(x => x.Type == "name");
                var username = nameClaim != null ? nameClaim.Value : "no name claim";
                var emailClaim = ClaimsPrincipal.Current.Identities.First().Claims.SingleOrDefault(x => x.Type == "email");
                var email = emailClaim != null ? emailClaim.Value : "no email claim";
                var message = "Added new configuration: " + cfg.Name + " in " + projectName;

                var git = new Git(projectPath);
                git.Stage(new List<string>() { configurationPath });
                git.Commit(message, username, email);
                git.Push();

                //MvcApplication.TcpServer.SendToAllClients(Encoding.UTF8.GetBytes(jar));
            }
            catch (Exception ex)
            {
                log.Fatal(ex);
                throw;
            }
        }

        [Authorize(Roles = "superAdmin")]
        [HttpPut("{projectName}/{configurationName}")]
        public void Put(string projectName, string configurationName, [FromBody]string value)
        {
            try
            {
                var cfg = JsonConvert.DeserializeObject<Jar>(value);

                if (string.IsNullOrWhiteSpace(projectName) || string.IsNullOrWhiteSpace(configurationName))
                    throw new InvalidOperationException();

                var projectPath = Path.Combine(Folders.Projects, projectName);

                if (configurationName.EndsWith(".json", StringComparison.Ordinal) == false)
                    configurationName += ".json";

                var configurationPath = GetConfigurationFile(projectName, configurationName);

                if (System.IO.File.Exists(configurationPath) == false)
                    throw new InvalidOperationException("There is no configuration file: " + configurationName);

                var box = Box.Box.Mistranslate(cfg);

                var jar = JsonConvert.SerializeObject(Box.Box.Mistranslate(box), Formatting.Indented);

                System.IO.File.WriteAllText(configurationPath, jar);

                var nameClaim = ClaimsPrincipal.Current.Identities.First().Claims.SingleOrDefault(x => x.Type == "name");
                var username = nameClaim != null ? nameClaim.Value : "no name claim";
                var emailClaim = ClaimsPrincipal.Current.Identities.First().Claims.SingleOrDefault(x => x.Type == "email");
                var email = emailClaim != null ? emailClaim.Value : "no email claim";
                var message = "Added new application configuration: " + cfg.Name + " in " + projectName;

                var git = new Git(projectPath);
                git.Stage(new List<string>() { configurationPath });
                git.Commit(message, username, email);
                git.Push();

                //MvcApplication.TcpServer.SendToAllClients(Encoding.UTF8.GetBytes(jar));
            }
            catch (Exception ex)
            {
                log.Fatal(ex);
                throw;
            }
        }

        [Authorize(Roles = "superAdmin")]
        [HttpDelete("{projectName}/{configurationName}")]
        public void Delete(string projectName, string configurationName)
        {
            try
            {
                var projectPath = Path.Combine(Folders.Projects, projectName);

                var configurationPath = GetConfigurationFile(projectName, configurationName);

                if (System.IO.File.Exists(configurationPath))
                {
                    var nameClaim = ClaimsPrincipal.Current.Identities.First().Claims.SingleOrDefault(x => x.Type == "name");
                    var username = nameClaim != null ? nameClaim.Value : "no name claim";
                    var emailClaim = ClaimsPrincipal.Current.Identities.First().Claims.SingleOrDefault(x => x.Type == "email");
                    var email = emailClaim != null ? emailClaim.Value : "no email claim";
                    var message = "Deleted configuration " + configurationName + " from " + projectName;

                    var git = new Git(projectPath);
                    git.Remove(new List<string>() { configurationPath });
                    git.Stage(new List<string>() { configurationPath });
                    git.Commit(message, username, email);
                    git.Push();
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
