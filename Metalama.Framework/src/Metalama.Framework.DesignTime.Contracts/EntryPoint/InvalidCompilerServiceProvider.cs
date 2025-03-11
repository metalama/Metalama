// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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