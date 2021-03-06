﻿using System;
using System.Security.Cryptography.X509Certificates;

namespace Addresses
{
    public static class Address
    {
        public static string EXTERNAL_DNS => "baldur.geuer-pollmann.de";
        public static string LOCAL_LAPTOP => "localhost";

        public static string STS => $"https://{EXTERNAL_DNS}";
        public static string Service => $"http://{LOCAL_LAPTOP}:6001/showmemyidentity";

        public static X509Certificate2 GetTokenSigningCertificate() {
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, $"CN={EXTERNAL_DNS}", validOnly: true);
            if (certs.Count != 1) { throw new NotSupportedException("Could not find the one and only cert"); }
            var tokenSigningCert = certs[0];
            return tokenSigningCert;
        }

    }
}