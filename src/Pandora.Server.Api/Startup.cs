using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication;
using Microsoft.AspNet.Authentication.JwtBearer;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Authorization.Infrastructure;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Elders.Pandora.Server.Api.AuthenticationMiddleware;
using Elders.Pandora.Server.Api.ViewModels;

namespace Elders.Pandora.Server.Api
{
    public class Resource
    {
        public string ProjectName { get; set; }

        public string ConfigurationName { get; set; }

        public string ClusterName { get; set; }

        public string MachineName { get; set; }

        public Access Access { get; set; }
    }

    public class ResourceAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, Resource>
    {
        protected override void Handle(AuthorizationContext context, OperationAuthorizationRequirement requirement, Resource resource)
        {
            var securityAccessClaim = context.User.Claims.SingleOrDefault(x => x.Type == "SecurityAccess");

            SecurityAccess security;

            if (securityAccessClaim == null || string.IsNullOrWhiteSpace(securityAccessClaim.Value))
                security = new SecurityAccess();
            else
                security = JsonConvert.DeserializeObject<SecurityAccess>(securityAccessClaim.Value);

            if (security == null)
                security = new SecurityAccess();

            if (string.IsNullOrWhiteSpace(resource.MachineName) == false)
            {
                var project = security.Projects.SingleOrDefault(x => x.Name == resource.ProjectName);
                if (project == null)
                {
                    context.Fail();
                    return;
                }

                var configuration = project.Applications.SingleOrDefault(x => x.Name == resource.ConfigurationName);
                if (configuration == null)
                {
                    context.Fail();
                    return;
                }

                if (configuration.Access.HasAccess(resource.Access) == false)
                {
                    context.Fail();
                    return;
                }

                var cluster = configuration.Clusters.SingleOrDefault(x => x.Name == resource.ClusterName);
                if (cluster == null)
                {
                    context.Fail();
                    return;
                }

                if (cluster.Access.HasAccess(resource.Access) == false)
                {
                    context.Fail();
                    return;
                }

                context.Succeed(requirement);
                return;
            }

            if (string.IsNullOrWhiteSpace(resource.ClusterName) == false)
            {
                var project = security.Projects.SingleOrDefault(x => x.Name == resource.ProjectName);
                if (project == null)
                {
                    context.Fail();
                    return;
                }

                var configuration = project.Applications.SingleOrDefault(x => x.Name == resource.ConfigurationName);
                if (configuration == null)
                {
                    context.Fail();
                    return;
                }

                if (configuration.Access.HasAccess(resource.Access) == false)
                {
                    context.Fail();
                    return;
                }

                var cluster = configuration.Clusters.SingleOrDefault(x => x.Name == resource.ClusterName);
                if (cluster == null)
                {
                    context.Fail();
                    return;
                }

                if (cluster.Access.HasAccess(resource.Access) == false)
                {
                    context.Fail();
                    return;
                }

                context.Succeed(requirement);
                return;
            }

            if (string.IsNullOrWhiteSpace(resource.ConfigurationName) == false)
            {
                var project = security.Projects.SingleOrDefault(x => x.Name == resource.ProjectName);
                if (project == null)
                {
                    context.Fail();
                    return;
                }

                var configuration = project.Applications.SingleOrDefault(x => x.Name == resource.ConfigurationName);
                if (configuration == null)
                {
                    context.Fail();
                    return;
                }

                if (configuration.Access.HasAccess(resource.Access) == false)
                {
                    context.Fail();
                    return;
                }

                context.Succeed(requirement);
                return;
            }

            if (string.IsNullOrWhiteSpace(resource.ProjectName) == false)
            {
                //var project = security.Projects.SingleOrDefault(x => x.Name == resource.ProjectName);
                //if (project == null)
                //{
                //    context.Fail();
                //    return;
                //}

                context.Succeed(requirement);
                return;
            }

            context.Fail();
        }
    }

    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            // Set up configuration sources.
            var builder = new ConfigurationBuilder()
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddInstance<IAuthorizationHandler>(new ResourceAuthorizationHandler());
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseIISPlatformHandler();

            ApplicationConfiguration.SetContext("Elders.Pandora.Server.Api");

            foreach (var directory in new[] { Folders.Main, Folders.Users, Folders.Projects })
            {
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
            }

            // Configure the HTTP request pipeline.
            app.UseStaticFiles();

            app.UseJwtBearerAuthentication(new JwtBearerOptions()
            {
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                TokenValidationParameters = GoogleTokenValidationParameters.GetParameters()
            });

            app.UseClaimsTransformation(new ClaimsTransformationOptions() { Transformer = new ClaimsTransformer() });

            // Add MVC to the request pipeline.
            app.UseMvc();
        }
        // Entry point for the application.
        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }

    public class ClaimsTransformer : IClaimsTransformer
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var identity = principal.Identity as ClaimsIdentity;

            if (identity.IsAuthenticated == false)
                return Task.FromResult<ClaimsPrincipal>(principal);

            var emailClaim = principal.Claims.SingleOrDefault(x => x.Type == ClaimTypes.Email);

            if (emailClaim != null && !string.IsNullOrWhiteSpace(emailClaim.Value))
            {
                var adminUsers = ApplicationConfiguration.Get("super_admin_users").Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries).ToList();

                if (adminUsers.Contains(emailClaim.Value))
                {
                    if (identity.HasClaim(x => x.Type == ClaimTypes.Role && x.Value == "superAdmin"))
                        return Task.FromResult<ClaimsPrincipal>(principal);

                    identity.AddClaim(new Claim(ClaimTypes.Role, "superAdmin"));
                }
            }

            var user = GetUser(identity);

            var access = JsonConvert.SerializeObject(user.Access, Formatting.Indented);

            identity.AddClaim(new Claim("SecurityAccess", access));

            return Task.FromResult<ClaimsPrincipal>(principal);
        }

        private User GetUser(ClaimsIdentity args)
        {
            var userId = args.Claims.Where(x => x.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value;

            var userFilePath = Path.Combine(Folders.Users, userId, userId + ".json");

            User user = null;

            if (File.Exists(userFilePath))
            {
                user = JsonConvert.DeserializeObject<User>(File.ReadAllText(userFilePath));
            }

            if (user == null)
            {
                user = new User();
                user.Id = userId;
                user.Access = new SecurityAccess();

                CreateUser(user);
            }

            return user;
        }

        private void CreateUser(User user)
        {
            var workingDir = Path.Combine(Folders.Users, user.Id);

            var userFilePath = Path.Combine(workingDir, user.Id + ".json");

            if (!File.Exists(userFilePath))
            {
                Directory.CreateDirectory(Path.Combine(workingDir));

                var serializedUser = JsonConvert.SerializeObject(user, Formatting.Indented);

                File.WriteAllText(userFilePath, serializedUser);
            }
        }
    }
}
