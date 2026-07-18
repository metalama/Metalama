// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Issue1710.Library;

namespace Issue1710.App
{
    /// <summary>
    /// Overrides the member that carries the [Positive] contract in Issue1710.Library.Base. The contract is
    /// therefore inherited across the project boundary, from a netstandard2.0 project into this net472 project.
    /// Collecting that inherited contract is what triggers the cross-copy hierarchical-options merge (issue #1710).
    /// </summary>
    public class Derived : Base
    {
        public override void SetValue( int value ) { }
    }
}
