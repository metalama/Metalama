// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using Metalama.Framework.DesignTime.Contracts.Pipeline;
using Metalama.Framework.Engine.Services;
using System.Collections.Concurrent;

namespace Metalama.Framework.DesignTime.VersionNeutral;

internal class CompilerServiceProvider : ICompilerServiceProvider
{
    private readonly ConcurrentDictionary<string, ICompilerService?> _services = new( StringComparer.Ordinal );
    private GlobalServiceProvider? _serviceProvider;

    protected internal CompilerServiceProvider( ContractVersion[]? contractVersions = null, Version? version = null )
    {
        this.ContractVersions = contractVersions ?? CurrentContractVersions.All;
        this.Version = version ?? this.GetType().Assembly.GetName().Version!;
    }

    public Version Version { get; }

    public ContractVersion[] ContractVersions { get; }

    protected GlobalServiceProvider ServiceProvider => this._serviceProvider ?? throw new InvalidOperationException();

    internal void Initialize( GlobalServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
    }

    public ICompilerService? GetService( Type serviceType ) => this.GetService( serviceType.Name );

    private ICompilerService? GetService( string name ) => this._services.GetOrAdd( name, this.GetServiceCore );

    protected virtual ICompilerService? GetServiceCore( string name )
        => name switch
        {
            nameof(ITransitiveCompilationService) => new TransitiveCompilationService( this.ServiceProvider ),
            _ => null
        };
}