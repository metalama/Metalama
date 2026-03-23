// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// Tests that transitive deserialization of an [Inheritable] aspect with a static typeof()
// field initializer works correctly (requires UserCodeExecutionContext during deserialization).

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Inheritance.CrossAssembly_TypeOf
{
    // Inherits TypeOfAspect from BaseClass transitively across assemblies.
    // This triggers deserialization of TransitiveAspectsManifest.
    public class DerivedClass : BaseClass { }
}
