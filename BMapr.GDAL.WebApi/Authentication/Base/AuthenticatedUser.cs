﻿using System.Security.Principal;

namespace BMapr.GDAL.WebApi.Authentication.Base
{
    public class AuthenticatedUser : IIdentity
    {
        public AuthenticatedUser(string authenticationType, bool isAuthenticated, string name)
        {
            AuthenticationType = authenticationType;
            IsAuthenticated = isAuthenticated;
            Name = name;
        }

        public string AuthenticationType { get; }

        public bool IsAuthenticated { get; }

        public string Name { get; }
    }
}
