// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.CodeFixes;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.Services;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;
using Metalama.Framework.Engine.Services;

namespace Metalama.Framework.DesignTime.Extensibility;

public sealed class DesignTimeInitializationContext
{
    private readonly List<ICodeFixProviderExtension> _codeFixProviderExtensions = new();
    private readonly List<ICodeRefactoringProviderExtension> _codeRefactoringProviderExtensions = new();
    private readonly List<IRpcServiceFactory> _rpcServices = new();

    internal DesignTimeInitializationContext( GlobalServiceProvider serviceProvider, DesignTimeProcessKind processKind )
    {
        this.ServiceProvider = serviceProvider;
        this.ProcessKind = processKind;
    }

    public GlobalServiceProvider ServiceProvider { get; }

    public DesignTimeProcessKind ProcessKind { get; }

    public void AddCodeFixProvider( ICodeFixProviderExtension codeFixProviderExtension ) => this._codeFixProviderExtensions.Add( codeFixProviderExtension );

    public void AddCodeRefactoringProvider( ICodeRefactoringProviderExtension codeFixProviderExtension )
        => this._codeRefactoringProviderExtensions.Add( codeFixProviderExtension );

    internal IReadOnlyList<ICodeFixProviderExtension> CodeFixProviderExtensions => this._codeFixProviderExtensions;

    internal IReadOnlyList<ICodeRefactoringProviderExtension> CodeRefactoringProviderExtensions => this._codeRefactoringProviderExtensions;

    // Note that the caller of ConfigureServices can only add shared services, otherwise it has no effect.
    public void ConfigureSharedServices( Action<GlobalServiceProvider> action ) => action( this.ServiceProvider );

    internal IReadOnlyList<IRpcServiceFactory> RpcServices => this._rpcServices;

    public void AddRpcService( IRpcServiceFactory serviceFactory ) => this._rpcServices.Add( serviceFactory );

    /// <summary>
    /// Registers types on the design-time RPC deserialization allow-list (security fix #1651 / GHSA-h26j-4vp7-x9w2).
    /// An extension must call this for every type it sends over the RPC channel that is not already allow-listed by the
    /// core framework (e.g. its own DTOs and any collection element/base types they reference); otherwise those types are
    /// rejected on deserialization. Always pass <c>typeof(...)</c> so the resolved assembly identity is correct under the
    /// <c>Metalama.Repacked</c> ILMerge in the VS extension.
    /// </summary>
    public void AddRpcContractTypes( params Type[] types )
    {
        var binderProvider = (IJsonSerializationBinderProvider?) this.ServiceProvider.Underlying.GetService( typeof(IJsonSerializationBinderProvider) );

        if ( binderProvider == null )
        {
            return;
        }

        foreach ( var type in types )
        {
            binderProvider.Binder.AddContractType( type );
        }
    }
}