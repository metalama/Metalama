// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Backstage.Licensing
{
    // The names are used in telemetry and changing them can make the telemetry data ambiguous.

    /// <summary>
    /// Enumeration of licensed products.
    /// </summary>
    public enum LicensedProduct : byte
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
        /// PostSharp 3.0 and future versions with active subscription.
        /// </summary>
        [Obsolete( "Use Ultimate or Framework" )]
        PostSharp30 = 2,

        /// <summary>
        /// PostSharp Ultimate.
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
        /// Logging Library.
        /// </summary>
        PostSharpDiagnosticsLibrary = 12,

        /// <summary>
        /// MVVM Library (former XAML/Model Library).
        /// </summary>
        PostSharpModelLibrary = 13,

        /// <summary>
        /// Threading Library.
        /// </summary>
        PostSharpThreadingLibrary = 14,

        /// <summary>
        /// Caching Library.
        /// </summary>
        PostSharpCachingLibrary = 15,

        // 255 is reserved as unknown for testing purposes
    }
}