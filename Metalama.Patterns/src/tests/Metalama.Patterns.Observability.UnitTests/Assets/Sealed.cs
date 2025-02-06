// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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