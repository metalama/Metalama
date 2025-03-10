// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Metalama.Framework.Tests.UnitTests.CompileTime
{
    internal sealed class TestAssemblyLocator : IAssemblyLocator
    {
        public Dictionary<AssemblyIdentity, MetadataReference> Files { get; } = new();

#pragma warning disable 8767
        public bool TryFindAssembly( AssemblyIdentity assemblyIdentity, out MetadataReference? reference )
#pragma warning restore 8767
            => this.Files.TryGetValue( assemblyIdentity, out reference );
    }
}