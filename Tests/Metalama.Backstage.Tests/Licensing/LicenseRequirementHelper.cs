// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing;
using System;

namespace Metalama.Backstage.Tests.Licensing
{
    internal static class LicenseRequirementHelper
    {
        public static LicenseRequirement GetRequirement( LicenseRequirementTestEnum requirement )
            => requirement switch
            {
                LicenseRequirementTestEnum.Core => LicenseRequirement.Core,
                LicenseRequirementTestEnum.Community => LicenseRequirement.Community,
                LicenseRequirementTestEnum.Professional => LicenseRequirement.Professional,
                LicenseRequirementTestEnum.Ultimate => LicenseRequirement.Professional,
                _ => throw new ArgumentException( nameof(requirement) )
            };
    }
}