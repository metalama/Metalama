// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Options;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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

        this._typeResolver ??= this._serviceProvider.GetRequiredService<ProjectSpecificCompileTimeTypeResolver>();

        var type = compilationModel.Factory.GetTypeByReflectionName( typeName );

        if ( type == null! )
        {
            return null;
        }

        return
            this._typeResolver.GetCompileTimeType( type.GetSymbol().AssertSymbolNotNull(), false );
    }

    /// <summary>
    /// Returns the names of the hierarchical options types that the additional compile-time assemblies of the project declare.
    /// </summary>
    /// <remarks>
    /// An additional compile-time assembly, such as the API of an extension that is not built by the Metalama compiler, is a standard reference
    /// assembly and not a compile-time project, so <see cref="CompileTimeProject.ClosureOptionTypes"/> does not contain its options types. The
    /// types are found from the symbols of the assemblies that the compilation references, including the types that are not public, because
    /// the options of an extension can be internal and set only by the public API of the extension.
    /// </remarks>
    private IEnumerable<string> GetAdditionalCompileTimeAssemblyOptionTypes( CompilationModel compilationModel )
    {
        var additionalAssemblyNames = this._serviceProvider.GetReferenceAssemblyLocator()
            .AdditionalCompileTimeAssemblyPaths
            .Select( p => Path.GetFileNameWithoutExtension( p ) );

        var additionalAssemblyNameSet = new HashSet<string>( additionalAssemblyNames, StringComparer.OrdinalIgnoreCase );

        var roslynCompilation = compilationModel.RoslynCompilation;
        var optionsInterface = roslynCompilation.GetTypeByMetadataName( typeof(IHierarchicalOptions).FullName.AssertNotNull() );

        if ( additionalAssemblyNameSet.Count == 0 || optionsInterface == null )
        {
            return [];
        }

        return roslynCompilation.SourceModule.ReferencedAssemblySymbols
            .Where( a => additionalAssemblyNameSet.Contains( a.Name ) )
            .SelectMany( a => GetTypes( a.GlobalNamespace ) )
            .Where( t => t is { TypeKind: Microsoft.CodeAnalysis.TypeKind.Class, IsAbstract: false } && t.AllInterfaces.Contains( optionsInterface, SymbolEqualityComparer.Default ) )
            .Select( t => t.GetReflectionFullName() )
            .ToList();

        static IEnumerable<INamedTypeSymbol> GetTypes( INamespaceOrTypeSymbol container )
            => container.GetMembers()
                .OfType<INamespaceOrTypeSymbol>()
                .SelectMany( m => m.Kind == SymbolKind.NamedType ? GetTypes( m ).Prepend( (INamedTypeSymbol) m ) : GetTypes( m ) );
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

        foreach ( var optionTypeName in project.ClosureOptionTypes.Concat( this.GetAdditionalCompileTimeAssemblyOptionTypes( compilationModel ) ).Distinct() )
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

            // The option type may not be registered if the type could not be resolved during initialization
            // (e.g., during design-time when the compilation is in an inconsistent state).
            if ( this.TryGetOptionTypeNode( optionTypeName, out var optionTypeNode ) )
            {
                optionTypeNode.AddOptionsInstance( option, diagnosticSink );
            }
        }
    }

    private bool TryGetOptionTypeNode( string optionTypeName, [NotNullWhen( true )] out OptionTypeNode? node )
    {
        if ( !this.IsInitialized )
        {
            throw new InvalidOperationException( $"The {nameof(HierarchicalOptionsManager)} has not been initialized." );
        }

        return this._optionTypes.TryGetValue( optionTypeName, out node );
    }

    public IHierarchicalOptions? GetOptions( IDeclaration declaration, Type optionsType )
    {
        if ( !this.TryGetOptionTypeNode( optionsType.FullName.AssertNotNull(), out var optionTypeNode ) )
        {
            // The option type may not be registered if the type could not be resolved during initialization
            // (e.g., during design-time when the compilation is in an inconsistent state).
            // Callers handle null via the null-coalescing pattern (e.g., ?? new TOptions()).
            return null;
        }

        return optionTypeNode.GetOptions( declaration ).AssertNotNull();
    }

    public IEnumerable<KeyValuePair<HierarchicalOptionsKey, IHierarchicalOptions>>
        GetInheritableOptions( ICompilation compilation, bool withSyntaxTree )
        => this._optionTypes.Where( s => s.Value.Metadata is { InheritedByDerivedTypes: true } or { InheritedByOverridingMembers: true } )
            .SelectMany( s => s.Value.GetInheritableOptions( compilation, withSyntaxTree ) );

    internal void SetAspectOptions( IDeclaration declaration, IHierarchicalOptions options )
    {
        if ( this.TryGetOptionTypeNode( options.GetType().FullName.AssertNotNull(), out var optionTypeNode ) )
        {
            optionTypeNode.SetAspectOptions( declaration, options );
        }
    }
}