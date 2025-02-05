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
        Ultimate = 3,

        /// <summary>
        /// PostSharp Framework.
        /// </summary>
        Framework = 4,

        /// <summary>
        /// Metalama Ultimate.
        /// </summary>
        MetalamaUltimate = 5,

        /// <summary>
        /// Metalama Professional.
        /// </summary>
        MetalamaProfessional = 6,

        /// <summary>
        /// Metalama Starter.
        /// </summary>
        MetalamaStarter = 7,

        /// <summary>
        /// Metalama Free.
        /// </summary>
        MetalamaFree = 8,

        /// <summary>
        /// Logging Library.
        /// </summary>
        DiagnosticsLibrary = 12,

        /// <summary>
        /// MVVM Library (former XAML/Model Library).
        /// </summary>
        ModelLibrary = 13,

        /// <summary>
        /// Threading Library.
        /// </summary>
        ThreadingLibrary = 14,

        /// <summary>
        /// Caching Library.
        /// </summary>
        CachingLibrary = 15

        // 255 is reserved as unknown for testing purposes
    }
}