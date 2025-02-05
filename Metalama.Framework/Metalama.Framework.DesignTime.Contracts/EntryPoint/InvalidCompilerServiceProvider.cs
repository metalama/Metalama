// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.DesignTime.Contracts.EntryPoint;

/// <summary>
/// An implementation of <see cref="ICompilerServiceProvider"/> that is returned when there is a mismatch
/// of contract pre-release version.
/// </summary>
internal sealed class InvalidCompilerServiceProvider : ICompilerServiceProvider
{
    public Version Version { get; }

    public ContractVersion[] ContractVersions { get; }

    public ICompilerService? GetService( Type serviceType ) => null;

    public InvalidCompilerServiceProvider( Version version, ContractVersion[] contractVersions )
    {
        this.Version = version;
        this.ContractVersions = contractVersions;
    }
}