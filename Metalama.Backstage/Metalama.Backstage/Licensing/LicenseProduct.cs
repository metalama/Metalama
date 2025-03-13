// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Backstage.Licensing
{
    // The names are used in telemetry and changing them can make the telemetry data ambiguous.

    /// <summary>
    /// Enumeration of products that can be licensed.
    /// The <see cref="LicenseProduct"/> (and not the <see cref="LicenseType"/>) determines the set of allowed features.
    /// </summary>
    public enum LicenseProduct : byte
    {
        /// <summary>
        /// None.
        /// </summary>
        None,

        /// <summary>
        /// PostSharp 2.0.
        /// </summary>
        [Obsolete( "This product is no longer supported." )]
        PostSharp20 = 1,

        /// <summary>
        /// Should be normalized to <see cref="PostSharpUltimate"/>.
        /// </summary>
        [Obsolete( "Use PostSharpUltimate instead." )]
        PostSharpUltimate1 = 2,

        /// <summary>
        /// PostSharp Ultimate (before normalization, PostSharp Essentials, if <see cref="LicenseType"/> is <c>Essentials</c>).
        /// </summary>
        PostSharpUltimate = 3,

        /// <summary>
        /// PostSharp Framework.
        /// </summary>
        PostSharpFramework = 4,

        /// <summary>
        /// Metalama Ultimate.
        /// </summary>
        [Obsolete( "This product is no longer offered." )]
        MetalamaUltimate = 5,

        /// <summary>
        /// Metalama Professional.
        /// </summary>
        MetalamaProfessional = 6,

        /// <summary>
        /// Metalama Starter.
        /// </summary>
        [Obsolete( "This product is no longer offered." )]
        MetalamaStarter = 7,

        /// <summary>
        /// Metalama Free.
        /// </summary>
        [Obsolete( "This product is no longer offered." )]
        MetalamaFree = 8,

        /// <summary>
        /// Metalama Community.
        /// </summary>
        MetalamaCommunity = 9,

        /// <summary>
        /// PostSharp Logging Library.
        /// </summary>
        PostSharpDiagnosticsLibrary = 12,

        /// <summary>
        /// PostSharp MVVM Library (former XAML/Model Library).
        /// </summary>
        PostSharpModelLibrary = 13,

        /// <summary>
        /// PostSharp Threading Library.
        /// </summary>
        PostSharpThreadingLibrary = 14,

        /// <summary>
        /// PostSharp Caching Library.
        /// </summary>
        PostSharpCachingLibrary = 15,

        /// <summary>
        /// PostSharp Essentials.
        /// </summary>
        PostSharpEssentials = 16,

        /// <summary>
        /// Metalama Enterprise.
        /// </summary>
        MetalamaEnterprise = 17

        // 255 is reserved as unknown for testing purposes
    }
}