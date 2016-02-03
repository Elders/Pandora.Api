﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Elders.Pandora.Server.Api.AuthenticationMiddleware
{
    public class GoogleTokenValidationParameters
    {
        public static TokenValidationParameters GetParameters()
        {
            var tvp = new TokenValidationParameters();
            tvp.ValidAudience = ApplicationConfiguration.Get("audience");
            tvp.ValidIssuer = ApplicationConfiguration.Get("issuer");
            tvp.ValidateIssuer = true;
            tvp.ValidateAudience = true;
            tvp.ValidateIssuerSigningKey = true;
            tvp.ValidateLifetime = true;

            var keys = new List<X509SecurityKey>();
            var certs = GetCertificates();

            foreach (var certificate in certs)
            {
                var key = new X509SecurityKey(certificate);
                keys.Add(key);
            }

            tvp.IssuerSigningKeyResolver = (a, b, c, d) => keys;

            return tvp;
        }

        // Used for string parsing the Certificates from Google
        private const string beginCert = "-----BEGIN CERTIFICATE-----\\n";
        private const string endCert = "\\n-----END CERTIFICATE-----\\n";
        private static List<X509Certificate2> GetCertificates()
        {
            // The request will be made to the authentication server.
            WebRequest request = WebRequest.Create(
                "https://www.googleapis.com/oauth2/v1/certs"
            );

            StreamReader reader = new StreamReader(request.GetResponse().GetResponseStream());

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
            return certBytes.Select(x => new X509Certificate2(x)).ToList();
        }
    }
}
