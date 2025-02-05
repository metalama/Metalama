// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.CodeFixes;
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
}