// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Options;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// Runs <see cref="RefTests"/> with the durable references of a batch compilation. Those references store the
/// reference they were created from instead of an identifier.
/// </summary>
public sealed class LiveRefTests : RefTests
{
    protected override DurableRefKind DurableRefKind => DurableRefKind.Live;
}
