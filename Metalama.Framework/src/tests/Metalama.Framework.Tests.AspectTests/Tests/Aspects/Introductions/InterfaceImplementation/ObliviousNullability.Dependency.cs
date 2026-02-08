// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// This file simulates an assembly compiled without nullable annotations (e.g. .NET Framework),
// where types have oblivious nullability.

#nullable disable

using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ObliviousNullability
{
    public delegate void ObliviousHandler( object sender, EventArgs e );

    public interface IObliviousInterface
    {
        event ObliviousHandler PropertyChanged;

        string ObliviousProperty { get; set; }

        string ObliviousMethod( string x );

        int ValueTypeProperty { get; set; }

        int ValueTypeMethod( int x );

        int? NullableValueTypeProperty { get; set; }

        int? NullableValueTypeMethod( int? x );
    }
}
