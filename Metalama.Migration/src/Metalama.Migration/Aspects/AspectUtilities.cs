// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace PostSharp.Aspects
{
    /// <summary>
    /// There is no equivalent to this class in Metalama.
    /// </summary>
    public sealed class AspectUtilities
    {
        public static void InitializeCurrentAspects()
        {
            throw new NotSupportedException( "The caller of this method should be transformed by PostSharp." );
        }
    }
}