// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Inheritance.IntroducedMembers;

// Tests inheritable aspects on introduced members,
// which relies on serialization of TransitiveAspectsManifest, including its usage of SerializableDeclarationId.

public class D : C { }