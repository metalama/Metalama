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
                LicenseRequirementTestEnum.Free => LicenseRequirement.Free,
                LicenseRequirementTestEnum.Starter => LicenseRequirement.Starter,
                LicenseRequirementTestEnum.Professional => LicenseRequirement.Professional,
                LicenseRequirementTestEnum.Ultimate => LicenseRequirement.Ultimate,
                _ => throw new ArgumentException( nameof(requirement) )
            };
    }
}