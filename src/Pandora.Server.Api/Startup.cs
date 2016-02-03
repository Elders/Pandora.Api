using System.IO;
using Microsoft.AspNet.Authentication.JwtBearer;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNet.Authentication;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Security.Claims;
using System.Linq;
using Elders.Pandora.Server.Api.AuthenticationMiddleware;
using Elders.Pandora.Server.Api.ViewModels;

namespace Elders.Pandora.Server.Api
{
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
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseIISPlatformHandler();

            ApplicationConfiguration.SetContext("Elders.Pandora.Api");

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

            if (System.IO.File.Exists(userFilePath))
            {
                user = JsonConvert.DeserializeObject<User>(System.IO.File.ReadAllText(userFilePath));
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

            if (!System.IO.File.Exists(userFilePath))
            {
                Directory.CreateDirectory(Path.Combine(workingDir));

                var serializedUser = JsonConvert.SerializeObject(user, Formatting.Indented);

                System.IO.File.WriteAllText(userFilePath, serializedUser);
            }
        }
    }
}
