// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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