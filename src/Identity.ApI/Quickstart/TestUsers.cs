// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using IdentityServer4.Test;
using System.Collections.Generic;
using System.Security.Claims;

namespace Identity.ApI
{
    public class TestUsers
    {
        public static List<TestUser> Users = new List<TestUser>
        {
            new TestUser{SubjectId = "09edbcf6-3b0f-408b-9ab5-2feeec14a12e", Username = "alice", Password = "alice",
                Claims =
                {

                    new Claim(JwtClaimTypes.GivenName, "Alice"),
                    new Claim(JwtClaimTypes.FamilyName, "Smith"),
                }
            },
            new TestUser{SubjectId = "9e990c23-45b3-4f88-950c-bfd9b0421cfa", Username = "bob", Password = "bob",
                Claims =
                {

                    new Claim(JwtClaimTypes.GivenName, "Bob"),
                    new Claim(JwtClaimTypes.FamilyName, "Smith"),

                }
            }
        };
    }
}