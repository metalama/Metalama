// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Backstage.Licensing
{
    /// <summary>
    /// Types of licenses.
    /// </summary>
    /// <remarks>
    /// This should be in sync with PostSharp\Public\Core\PostSharp.Compiler.Settings\Extensibility\Licensing\ReportedLicenseType.cs (TODO)
    /// and BusinessSystems\common\SharpCrafters.Internal.Services.LicenseGenerator\ProductConfiguration.cs.
    /// 
    /// Obsolete license types do not need to be included here
    /// and reported license types representing products and special cases should not be included here.
    /// </remarks>
    [PublicAPI]
    public enum LicenseType : byte
    {
        /// <summary>
        /// No license.
        /// </summary>
        None = 0,

        [Obsolete( "Renamed to Community" )]
        Essentials = Community,

        /// <summary>
        /// Metalama Community.
        /// </summary>
        Community = 1,

        /// <summary>
        /// Commercial PerUser license.
        /// </summary>
        [Obsolete( "Renamed to Business" )]
        PerUser = Business,

        Business = 2,

        /// <summary>
        /// Site license.
        /// </summary>
        Site = 3,

        /// <summary>
        /// Global license.
        /// </summary>
        Global = 4,

        /// <summary>
        /// Evaluation license.
        /// </summary>
        Evaluation = 5,

        /// <summary>
        /// Open source redistribution license.
        /// </summary>
        [Obsolete( "No longer issued or supported." )]
        OpenSourceRedistribution = 6,

        /// <summary>
        /// Academic license.
        /// </summary>
        Academic = 8,

        /// <summary>
        /// Redistribution (with contract).
        /// </summary>
        [Obsolete( "No longer issued or supported." )]
        CommercialRedistribution = 12,

        /// <summary>
        /// Anonymous license.
        /// </summary>
        [Obsolete( "No longer issued." )]
        Anonymous = 13,

        /// <summary>
        /// Commercial license (A pattern library, or PostSharp Framework (former Professional edition)).
        /// </summary>
        [Obsolete( "Use LicenseType.PerUser and LicensedProduct.Framework" )]
        Professional = 14,

        /// <summary>
        /// Commercial license (Enterprise edition).
        /// Deprecated in 6.6.
        /// </summary>
        Enterprise = 15,

        /// <summary>
        /// Internal license for build servers.
        /// </summary>
        Unattended = 16,

        /// <summary>
        /// Internal license for developers building unmodified source code.
        /// </summary>
        Unmodified = 17,

        [Obsolete( "No longer issued or supported." )]
        PerUsage = 18,

        /// <summary>
        /// Personal license.
        /// </summary>
        /// <remarks>
        /// Bound to single person.
        /// </remarks>
        Personal = 19,

        /// <summary>
        /// Usable with a preview build.
        /// </summary>
        [Obsolete( "No longer issued or supported." )]
        Preview = 20,

        /// <summary>
        /// A test license key, when something else than licensing itself if tested.
        /// </summary>
        Test = 21

        // 255 is reserved as unknown for testing purposes
    }
}