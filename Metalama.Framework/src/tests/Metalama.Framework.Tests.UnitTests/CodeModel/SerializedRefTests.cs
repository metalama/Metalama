// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Options;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// Runs <see cref="RefTests"/> with identifier-based durable references and with the resolution cache enabled. This is
/// the configuration used at design time, and in every execution scenario that is not a batch compilation.
/// </summary>
public sealed class SerializedRefTests : RefTests
{
    protected override DurableRefKind DurableRefKind => DurableRefKind.Serialized;
}
