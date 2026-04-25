// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable SA1649, SA1402

// ReSharper disable  CheckNamespace

using Metalama.Framework.Fabrics;
using System;

namespace Metalama.Patterns.Contracts.AspectTests.Fabric_PrimaryConstructor
{
    internal class Fabric : ProjectFabric
    {
        public override void AmendProject( IProjectAmender amender )
        {
            amender.VerifyNotNullableDeclarations( true );
        }
    }

    public sealed class Memento( Action action ) : IDisposable
    {
        private Action Action { get; } = action;

        void IDisposable.Dispose()
        {
            this.Action();
        }
    }
}