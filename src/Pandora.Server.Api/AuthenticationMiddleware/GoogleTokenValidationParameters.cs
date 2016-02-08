using System;
using System.Collections.Generic;
using System.IO;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Collections.Concurrent;

namespace Elders.Pandora.Server.Api.AuthenticationMiddleware
{
    public class GoogleTokenValidationParameters
    {
        private static ConcurrentDictionary<DateTime, List<X509SecurityKey>> securityKeys = new ConcurrentDictionary<DateTime, List<X509SecurityKey>>();

        public static TokenValidationParameters GetParameters()
        {
            var tvp = new TokenValidationParameters();
            tvp.ValidAudience = ApplicationConfiguration.Get("audience");
            tvp.ValidIssuer = ApplicationConfiguration.Get("issuer");
            tvp.ValidateIssuer = true;
            tvp.ValidateAudience = true;
            tvp.ValidateIssuerSigningKey = true;
            tvp.ValidateLifetime = true;
            tvp.IssuerSigningKeyResolver = (a, b, c, d) => GetCertificates();
            return tvp;
        }

        // Used for string parsing the Certificates from Google
        private const string beginCert = "-----BEGIN CERTIFICATE-----\\n";
        private const string endCert = "\\n-----END CERTIFICATE-----\\n";
        private static List<X509SecurityKey> GetCertificates()
        {
            var expiration = DateTime.UtcNow.AddMinutes(10);

            var expired = securityKeys.ToList().Where(x => x.Key < expiration);
            foreach (var item in expired)
            {
                List<X509SecurityKey> expiredKeys;
                securityKeys.TryRemove(item.Key, out expiredKeys);
            }

            var active = securityKeys.ToList().Where(x => x.Key > DateTime.UtcNow).OrderByDescending(x => x.Key);
            if (active.Count() > 0)
                return active.FirstOrDefault().Value;

            // The request will be made to the authentication server.
            WebRequest request = WebRequest.Create(
                "https://www.googleapis.com/oauth2/v1/certs"
            );

            var response = request.GetResponse();

            var expirationDateHeader = response.Headers["expires"];

            var certificateExpirationDate = DateTime.Parse(expirationDateHeader).ToUniversalTime();

            StreamReader reader = new StreamReader(response.GetResponseStream());

            string responseFromServer = reader.ReadToEnd();

            String[] split = responseFromServer.Split(':');

            // There are two certificates returned from Google
            byte[][] certBytes = new byte[2][];
            int index = 0;
            UTF8Encoding utf8 = new UTF8Encoding();
            for (int i = 0; i < split.Length; i++)
            {
                if (split[i].IndexOf(beginCert) > 0)
                {
                    int startSub = split[i].IndexOf(beginCert);
                    int endSub = split[i].IndexOf(endCert) + endCert.Length;
                    certBytes[index] = utf8.GetBytes(split[i].Substring(startSub, endSub).Replace("\\n", "\n"));
                    index++;
                }
            }

            var certs = certBytes.Select(x => new X509Certificate2(x)).ToList();

            var keys = new List<X509SecurityKey>();

            foreach (var cert in certs)
            {
                var key = new X509SecurityKey(cert);
                keys.Add(key);
            }

            securityKeys.TryAdd(certificateExpirationDate, keys);

            return keys;
        }
    }
}
