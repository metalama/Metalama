// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Extensibility;

namespace Metalama.Framework.Tests.ExtensionPoints.Engine;

/// <summary>
/// The contributor kinds of the proof of concept.
/// </summary>
internal static class TestContributorKinds
{
    public static ContributorKind<TestRegistration> Registration { get; } = new( "TestExtensionPointsRegistration" );
}
