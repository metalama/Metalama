// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Assets
{
    internal sealed class CustomComparer<T> : IEqualityComparer<T>
    {
        public bool Equals( T? x, T? y ) => true;

        public int GetHashCode( T obj ) => 0;
    }
}