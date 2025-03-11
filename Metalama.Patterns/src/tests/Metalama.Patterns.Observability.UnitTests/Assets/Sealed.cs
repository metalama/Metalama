// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Observability.UnitTests.Assets.Core;

namespace Metalama.Patterns.Observability.UnitTests.Assets.Sealed
{
    /// <summary>
    /// No base, sealed, has [Observable].
    /// </summary>
    [Observable]
    public sealed partial class C1
    {
        /// <summary>
        /// Auto
        /// </summary>
        public int C1P1 { get; set; }

        /// <summary>
        /// Auto
        /// </summary>
        public Simple C1P2 { get; set; }

        /// <summary>
        /// Ref to C1P2.S1.
        /// </summary>
        public int C1P3 => this.C1P2.S1;
    }

    /// <summary>
    /// C2 : Simple, sealed.
    /// </summary>
    public sealed partial class C2 : Simple
    {
        /// <summary>
        /// Auto
        /// </summary>
        public int C2P1 { get; set; }

        /// <summary>
        /// Ref to Simple.S1.
        /// </summary>
        public int C2P3 => this.S1;
    }
}