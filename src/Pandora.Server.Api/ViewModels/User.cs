using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using Newtonsoft.Json;

namespace Elders.Pandora.Server.Api.ViewModels
{
    public class User
    {
        public string Id { get; set; }

        public string Email { get; set; }

        public string FullName { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string AvatarUrl { get; set; }

        public string Organization { get; set; }

        public SecurityAccess Access { get; set; }
    }

    public class AccessRules
    {
        public string Application { get; set; }

        public string Project { get; set; }

        public Access Access { get; set; }

        public string Cluster { get; set; }
    }

    public class SecurityAccess
    {
        public SecurityAccess()
        {
            this.Projects = new List<Project>();
        }

        public List<Project> Projects { get; set; }

        public override string ToString()
        {
            return "SecurityAccess";
        }

        public void AddRule(AccessRules rule)
        {
            var project = this.Projects.SingleOrDefault(x => x.Name == rule.Project);

            if (project == null)
            {
                project = new Project() { Name = rule.Project };

                this.Projects.Add(project);
            }

            var application = project.Applications.SingleOrDefault(x => x.Name == rule.Application);

            if (application == null)
            {
                application = new Application() { Name = rule.Application };

                project.Applications.Add(application);
            }

            if (rule.Cluster == "Defaults")
            {
                if (rule.Access == Access.WriteAccess)
                    application.Access = Access.ReadAcccess | Access.WriteAccess;
                else
                    application.Access = Access.ReadAcccess;
            }
            else
            {
                var cluster = application.Clusters.SingleOrDefault(x => x.Name == rule.Cluster);

                if (cluster == null)
                {
                    cluster = new Cluster() { Name = rule.Cluster };

                    application.Clusters.Add(cluster);
                }

                if (rule.Access == Access.WriteAccess)
                    cluster.Access = Access.ReadAcccess | Access.WriteAccess;
                else
                    cluster.Access = Access.ReadAcccess;
            }
        }
    }

    public class Project
    {
        public Project()
        {
            this.Applications = new List<Application>();
        }

        public string Name { get; set; }

        public List<Application> Applications { get; set; }
    }

    public class Application
    {
        public Application()
        {
            this.Clusters = new List<Cluster>();
        }

        public string Name { get; set; }

        public List<Cluster> Clusters { get; set; }

        public Access Access { get; set; }
    }

    public class Cluster
    {
        public string Name { get; set; }

        public Access Access { get; set; }
    }

    [Flags]
    public enum Access
    {
        WriteAccess = 2,
        ReadAcccess = 4
    }
}
