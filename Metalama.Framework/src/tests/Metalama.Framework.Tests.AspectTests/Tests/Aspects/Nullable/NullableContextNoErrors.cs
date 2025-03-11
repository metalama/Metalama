// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#nullable disable

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Nullable.NullableContextNoErrors
{
    internal class Aspect : TypeAspect
    {
        [Introduce]
        private string Introduced1( string a ) => a.ToString();

#nullable enable
        [Introduce]
        private string Introduced2( string? a ) => a!.ToString();

        [Introduce]
        private string Introduced3( int x )
        {
#nullable disable
            return "";
        }
    }
}