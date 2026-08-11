// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Options;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// Runs <see cref="RefTests"/> with identifier-based durable references and without the resolution cache, so that
/// every resolution goes through the symbol table.
/// </summary>
/// <remarks>
/// The cache returns the result of most resolutions performed in a single compilation. Without this class, the
/// identifier resolution code would be covered only by the first resolution of each reference.
/// </remarks>
public sealed class UncachedSerializedRefTests : RefTests
{
    protected override DurableRefKind DurableRefKind => DurableRefKind.SerializedWithoutCache;
}
