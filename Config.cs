// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using IdentityServer4;
using IdentityServer4.Models;
using System.Collections.Generic;

/*
THIS FILE IS NOT USED
*/

namespace IdentityCore
{

    public static class Config
    {
        private static string JsClientHost = "http://localhost:3000";

        public static IEnumerable<IdentityResource> IdentityResources =>
            new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
            };

        public static IEnumerable<ApiResource> ApiResources =>
            new List<ApiResource> {
                new ApiResource("catApi", "Api providing cat facts")
                {
                    Scopes = { "catApi.fact", "catApi.user" }
                }
            };

        public static IEnumerable<ApiScope> ApiScopes =>
            new List<ApiScope>
            {
                new ApiScope("api1", "My API"),
                new ApiScope("catApi.fact", "Get fact about cat"),
                new ApiScope("catApi.user", "Get user info")
            };

        public static IEnumerable<Client> Clients =>
            new List<Client>
            {
                new Client
                {
                    ClientId = "js",
                    ClientName = "JavaScript Client",
                    AllowedGrantTypes = GrantTypes.Code,
                    RequireClientSecret = false,

                    RefreshTokenUsage = TokenUsage.OneTimeOnly,
                    RefreshTokenExpiration = TokenExpiration.Absolute,

                    AbsoluteRefreshTokenLifetime = 60 * 60,
                    AccessTokenLifetime = 60 * 10,

                    RedirectUris = {
                        $"{JsClientHost}",
                        $"{JsClientHost}/#/signin-oidc",
                        $"{JsClientHost}/#/silentrenew" },
                    PostLogoutRedirectUris = { $"{JsClientHost}" },
                    AllowedCorsOrigins =     { $"{JsClientHost}" },

                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "api1",
                        "catApi.fact",
                        "catApi.user"
                    },

                    RequireConsent = false,
                }
            };
    }

}