// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Fabrics;
using Metalama.Patterns.Contracts;

namespace Metalama.Patterns.Caching;

[UsedImplicitly]
internal class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender ) => amender.VerifyNotNullableDeclarations();
}