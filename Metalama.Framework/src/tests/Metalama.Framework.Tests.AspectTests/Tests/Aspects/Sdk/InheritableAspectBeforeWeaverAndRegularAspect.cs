// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// The inheritable aspect is applied in the dependency, whose pipeline also contains a low-level weaver stage.
// The aspect must be written to the transitive manifest of the dependency and be inherited here.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.InheritableAspectBeforeWeaverAndRegularAspect
{
    public class C : I { }
}
