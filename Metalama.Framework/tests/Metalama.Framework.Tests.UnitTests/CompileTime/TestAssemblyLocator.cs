// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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