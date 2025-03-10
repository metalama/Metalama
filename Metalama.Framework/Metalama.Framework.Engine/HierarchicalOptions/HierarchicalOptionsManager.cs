// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.HierarchicalOptions;

public sealed partial class HierarchicalOptionsManager : IHierarchicalOptionsManager
{
    private readonly ConcurrentDictionary<string, OptionTypeNode> _optionTypes = new();
    private readonly ProjectServiceProvider _serviceProvider;
    private readonly UserCodeInvoker _userCodeInvoker;
    private IExternalHierarchicalOptionsProvider? _externalOptionsProvider;
    private CompileTimeTypeResolver? _typeResolver;

    private bool IsInitialized { get; set; }

    internal HierarchicalOptionsManager( in ProjectServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
        this._userCodeInvoker = serviceProvider.GetRequiredService<UserCodeInvoker>();
    }

    private Type? GetOptionType( string typeName, CompilationModel compilationModel )
    {
        // We get the type resolver lazily because several tests do not supply it.

        this._typeResolver ??= this._serviceProvider.GetRequiredService<CompilationServiceProvider<ProjectSpecificCompileTimeTypeResolver>>()
            .Get( compilationModel.CompilationContext );

        var type = compilationModel.Factory.GetTypeByReflectionName( typeName );

        if ( type == null! )
        {
            return null;
        }

        return
            this._typeResolver.GetCompileTimeType( type.GetSymbol().AssertSymbolNotNull(), false );
    }

    internal Task InitializeAsync(
        CompileTimeProject project,
        IEnumerable<IHierarchicalOptionsSource> sources,
        IExternalHierarchicalOptionsProvider? externalOptionsProvider,
        CompilationModel compilationModel,
        IUserDiagnosticSink diagnosticSink,
        CancellationToken cancellationToken )
    {
        if ( this.IsInitialized )
        {
            throw new InvalidOperationException();
        }
        else
        {
            this.IsInitialized = true;
        }

        // Initialize all default options. We need to do this during initialization because we need a diagnostic sink and won't have it later.

        foreach ( var optionTypeName in project.ClosureOptionTypes )
        {
            var userCodeExecutionContext = UserCodeExecutionContext.CreateInstance(
                this._serviceProvider,
                UserCodeDescription.Create( "Initializing options '{0}'", optionTypeName ),
                compilationModel,
                diagnostics: diagnosticSink );

            var optionType = this.GetOptionType( optionTypeName, compilationModel );

            if ( optionType == null )
            {
                // It seems to happen at design time during external rebuilds that the options type may not be found.
                continue;
            }

            if ( !this._userCodeInvoker.TryInvoke(
                    () => (IHierarchicalOptions) Activator.CreateInstance( optionType ).AssertNotNull(),
                    userCodeExecutionContext,
                    out var emptyOptions ) )
            {
                continue;
            }

            var getDefaultOptionsContext = new OptionsInitializationContext(
                compilationModel.Project,
                new ScopedDiagnosticSink(
                    diagnosticSink,
                    new AdhocDiagnosticSource( $"executing the '{optionType.Name}.{nameof(IHierarchicalOptions.GetDefaultOptions)}' method" ),
                    null,
                    null ) );

            if ( !this._userCodeInvoker.TryInvoke(
                    () => emptyOptions.GetDefaultOptions( getDefaultOptionsContext ),
                    userCodeExecutionContext,
                    out var defaultOptions ) )
            {
                // If we fail to get the default options, we will continue with the non-initialized options.
            }

            defaultOptions ??= emptyOptions;

            this._optionTypes.TryAdd(
                optionTypeName,
                new OptionTypeNode( this, optionType, diagnosticSink, defaultOptions, emptyOptions, compilationModel.CompilationContext ) );
        }

        if ( externalOptionsProvider != null )
        {
            this._externalOptionsProvider = externalOptionsProvider;

            // We have to create OptionType nodes now while we have an IDiagnosticAdder. 
            foreach ( var optionType in externalOptionsProvider.GetOptionTypes() )
            {
                _ = this.GetOptionTypeNode( optionType );
            }
        }

        return Task.WhenAll( sources.Select( s => this.AddSourceAsync( s, compilationModel, diagnosticSink, cancellationToken ) ) );
    }

    internal async Task AddSourceAsync(
        IHierarchicalOptionsSource source,
        CompilationModel compilationModel,
        IUserDiagnosticSink diagnosticSink,
        CancellationToken cancellationToken )
    {
        await source.CollectOptionsAsync( compilationModel, AddOption, diagnosticSink, cancellationToken );

        void AddOption( HierarchicalOptionsInstance option )
        {
            var optionTypeName = option.Options.GetType().FullName.AssertNotNull();

            var optionTypeNode = this.GetOptionTypeNode( optionTypeName );

            optionTypeNode.AddOptionsInstance( option, diagnosticSink );
        }
    }

    private OptionTypeNode GetOptionTypeNode( string optionTypeName )
    {
        if ( !this.IsInitialized )
        {
            throw new InvalidOperationException( $"The {nameof(HierarchicalOptionsManager)} has not been initialized." );
        }

        if ( !this._optionTypes.TryGetValue( optionTypeName, out var optionTypeNode ) )
        {
            throw new AssertionFailedException( $"The option type '{optionTypeName}' is not a part of the current project." );
        }

        return optionTypeNode;
    }

    public IHierarchicalOptions GetOptions( IDeclaration declaration, Type optionsType )
    {
        var optionTypeNode = this.GetOptionTypeNode( optionsType.FullName.AssertNotNull() );

        return optionTypeNode.GetOptions( declaration ).AssertNotNull();
    }

    public IEnumerable<KeyValuePair<HierarchicalOptionsKey, IHierarchicalOptions>>
        GetInheritableOptions( ICompilation compilation, bool withSyntaxTree )
        => this._optionTypes.Where( s => s.Value.Metadata is { InheritedByDerivedTypes: true } or { InheritedByOverridingMembers: true } )
            .SelectMany( s => s.Value.GetInheritableOptions( compilation, withSyntaxTree ) );

    internal void SetAspectOptions( IDeclaration declaration, IHierarchicalOptions options )
        => this.GetOptionTypeNode( options.GetType().FullName.AssertNotNull() ).SetAspectOptions( declaration, options );
}