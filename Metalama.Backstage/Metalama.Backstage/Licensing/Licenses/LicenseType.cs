// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;

namespace Metalama.Backstage.Licensing.Licenses
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

        /// <summary>
        /// Essentials license (former Community, Essentials, Express or Starter).
        /// </summary>
        Essentials = 1,

        /// <summary>
        /// Commercial PerUser license.
        /// </summary>
        PerUser = 2,

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
        OpenSourceRedistribution = 6,

        /// <summary>
        /// Academic license.
        /// </summary>
        Academic = 8,

        /// <summary>
        /// Redistribution (with contract).
        /// </summary>
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
        /// </summary>
        [Obsolete( "No longer issued." )]
        Enterprise = 15,

        /// <summary>
        /// Internal license for build servers.
        /// </summary>
        Unattended = 16,

        /// <summary>
        /// Internal license for developers building unmodified source code.
        /// </summary>
        Unmodified = 17,

        /// <summary>
        /// License limiting the number of enhanced types. This is used with Ultimate or with the aspect libraries.
        /// </summary>
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
        Preview

        // 255 is reserved as unknown for testing purposes
    }
}