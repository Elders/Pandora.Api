using System.Linq;
using System.Security.Claims;
using System.Security.Principal;

namespace Elders.Pandora.Server.Api.ViewModels
{
    public static class AccessExtensions
    {
        //public static bool HasAccess(this SecurityAccess access, string projectName)
        //{
        //    if (access.Projects.Any(x => x.Name == projectName))
        //        return true;
        //    else
        //        return false;
        //}

        //public static bool HasAccess(this SecurityAccess access, string projectName, string configurationName)
        //{
        //    if (access.HasAccess(projectName))
        //    {
        //        if (access.Projects.SingleOrDefault(x => x.Name == projectName)
        //            .Applications.SingleOrDefault(x => x.Name == configurationName).Access.HasAccess(Access.ReadAcccess))
        //            return true;
        //        else
        //            return false;
        //    }
        //    else
        //        return false;
        //}

        //public static bool HasAccess(this SecurityAccess access, string projectName, string configurationName, string clusterName)
        //{
        //    if (access.HasAccess(projectName, configurationName))
        //    {
        //        if (access.Projects.SingleOrDefault(x => x.Name == projectName)
        //            .Applications.SingleOrDefault(x => x.Name == configurationName)
        //            .Clusters.SingleOrDefault(x => x.Name == clusterName).Access.HasAccess(Access.ReadAcccess))
        //            return true;
        //        else
        //            return false;
        //    }
        //    else
        //        return false;
        //}

        //public static bool HasAccess(this SecurityAccess self, string project, string application, string cluster, Access access)
        //{
        //    if (self.Projects.Select(x => x.Name).Contains(project))
        //    {
        //        if (self.Projects.SingleOrDefault(x => x.Name == project).Applications.Select(x => x.Name).Contains(application))
        //        {
        //            if (cluster == "Defaults")
        //            {
        //                return self.Projects.SingleOrDefault(x => x.Name == project).Applications.SingleOrDefault(x => x.Name == application).Access.HasAccess(access);
        //            }
        //            else if (self.Projects.SingleOrDefault(x => x.Name == project).Applications.SingleOrDefault(x => x.Name == application).Clusters.Select(x => x.Name).Contains(cluster))
        //            {
        //                var cl = self.Projects.SingleOrDefault(x => x.Name == project).Applications.SingleOrDefault(x => x.Name == application).Clusters.SingleOrDefault(x => x.Name == cluster);

        //                return cl.Access.HasAccess(access);
        //            }
        //        }
        //    }

        //    return false;
        //}

        public static bool HasAccess(this Access self, Access check)
        {
            return (self & check) == check;
        }

        public static string Token(this IPrincipal self)
        {
            if (self is ClaimsPrincipal)
                return (self as ClaimsPrincipal).Claims.Where(x => x.Type == "at").FirstOrDefault().Value;
            else
                return string.Empty;
        }

        public static string FullName(this IPrincipal self)
        {
            if (self is ClaimsPrincipal)
            {
                var fullname = (self as ClaimsPrincipal).Claims.Where(x => x.Type == "name").FirstOrDefault();

                if (fullname != null && !string.IsNullOrWhiteSpace(fullname.Value))
                    return fullname.Value;
            }
            return self.Email();
        }

        public static string FirstName(this IPrincipal self)
        {
            if (self is ClaimsPrincipal)
            {
                var firstname = (self as ClaimsPrincipal).Claims.Where(x => x.Type == "given_name").FirstOrDefault();

                if (firstname != null && !string.IsNullOrWhiteSpace(firstname.Value))
                    return firstname.Value;
            }
            return string.Empty;
        }

        public static string LastName(this IPrincipal self)
        {
            if (self is ClaimsPrincipal)
            {
                var lastname = (self as ClaimsPrincipal).Claims.Where(x => x.Type == "family_name").FirstOrDefault();

                if (lastname != null && !string.IsNullOrWhiteSpace(lastname.Value))
                    return lastname.Value;
            }
            return string.Empty;
        }

        public static string Avatar(this IPrincipal self)
        {
            if (self is ClaimsPrincipal)
            {
                var fullname = (self as ClaimsPrincipal).Claims.Where(x => x.Type == "picture").FirstOrDefault();

                if (fullname != null && !string.IsNullOrWhiteSpace(fullname.Value))
                    return fullname.Value;
            }
            return self.Email();
        }

        public static string Email(this IPrincipal self)
        {
            if (self is ClaimsPrincipal)
            {
                var email = (self as ClaimsPrincipal).Claims.Where(x => x.Type == "email").FirstOrDefault();

                if (email != null)
                    return email.Value;
            }
            return string.Empty;
        }

        public static string Id(this IPrincipal self)
        {
            if (self is ClaimsPrincipal)
            {
                var id = (self as ClaimsPrincipal).Claims.Where(x => x.Type == "sub").FirstOrDefault();

                if (id != null)
                    return id.Value;
            }
            return string.Empty;
        }
    }
}
