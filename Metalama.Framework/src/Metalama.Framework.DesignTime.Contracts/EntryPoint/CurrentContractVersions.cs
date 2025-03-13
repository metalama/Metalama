// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Framework.DesignTime.Contracts.EntryPoint;

// ReSharper disable InconsistentNaming
#pragma warning disable SA1310

/// <summary>
/// Exposes the <see cref="ContractVersion_1_0"/> constant, which is used to differentiate versions of the API between pre-releases.
/// This class intentionally only exposes <i>constants</i> so they are copied in the caller code during compilation.
/// </summary>
[PublicAPI]
public static class CurrentContractVersions
{
    /// <summary>
    /// Gets the current version of the 1.0 contracts.
    /// </summary>
    public const int ContractVersion_1_0 = 3;

    public static ContractVersion[] All { get; } = new[] { new ContractVersion { Version = "1.0", Revision = ContractVersion_1_0 } };
}