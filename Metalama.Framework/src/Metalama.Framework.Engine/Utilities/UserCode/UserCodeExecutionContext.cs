// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Templating.MetaModel;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Project;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace Metalama.Framework.Engine.Utilities.UserCode;

/// <summary>
/// Represents the context of execution of compile-time user code, when this code does not have another
/// "cleaner" way to get the context. Specifically, this is used to in the transformed expression of <c>typeof</c>.
/// The current class is a service that must be registered and then disposed.
/// </summary>
[PublicAPI]
public class UserCodeExecutionContext : IExecutionContextInternal
{
    private readonly IDiagnosticAdder? _diagnosticAdder;
    private readonly bool _throwOnUnsupportedDependencies;
    private readonly IDependencyCollector? _dependencyCollector;
    private readonly INamedType? _targetType;
    private readonly ImmutableArray<SyntaxTree>? _sourceTrees;
    private readonly ISyntaxBuilderImpl? _syntaxBuilder;

    private bool _collectDependencyDisabled;

    internal static UserCodeExecutionContext Current => (UserCodeExecutionContext) MetalamaExecutionContext.Current ?? throw new InvalidOperationException();

    internal static UserCodeExecutionContext? CurrentOrNull => (UserCodeExecutionContext?) MetalamaExecutionContext.CurrentOrNull;

    internal static Type ResolveCompileTimeTypeOf( string typeId, IReadOnlyDictionary<string, IType>? substitutions = null )
    {
        if ( Current.CompilationContext == null )
        {
            throw new InvalidOperationException( "Cannot use typeof for run-time types in the current execution context." );
        }

        // When substitutions contain types without symbols (e.g. introduced types), the symbol-based resolver
        // cannot handle them. Use the IType-based resolver via the CompilationModel instead.
        if ( substitutions != null && Current.Compilation != null && substitutions.Values.Any( t => t.GetSymbol() == null ) )
        {
            return ResolveCompileTimeTypeOfUsingITypeResolver( typeId, substitutions );
        }

        return Current.CompilationContext.CompileTimeTypeFactory
            .Get( new SerializableTypeId( typeId ), substitutions );
    }

    private static Type ResolveCompileTimeTypeOfUsingITypeResolver( string typeId, IReadOnlyDictionary<string, IType> substitutions )
    {
        var compilation = Current.Compilation!;

        // Resolve using the IType-based resolver, which can handle introduced types.
        var resolvedType = compilation.SerializableTypeIdResolver.ResolveId( new SerializableTypeId( typeId ), substitutions );

        // Get the serializable type ID for the resolved type (for caching in CompileTimeTypeFactory).
        var resolvedTypeId = resolvedType.GetSerializableTypeId( bypassSymbols: true );

        // Build metadata from the resolved IType.
        var ns = GetNamespaceForType( resolvedType );
        var name = resolvedType.GetReflectionName( bypassSymbols: true );
        var fullName = resolvedType.GetReflectionFullName( bypassSymbols: true );
        var toStringName = resolvedType.GetReflectionToStringName( bypassSymbols: true );
        var metadata = new CompileTimeTypeMetadata( ns, name, fullName, toStringName );

        return Current.CompilationContext!.CompileTimeTypeFactory.Get( resolvedTypeId, metadata );
    }

    /// <summary>
    /// Emulates <see cref="Type.Namespace"/>: unwraps arrays, pointers, etc. to find the innermost named type's namespace.
    /// </summary>
    private static string? GetNamespaceForType( IType type )
    {
        while ( true )
        {
            switch ( type )
            {
                case INamedType namedType:
                    return namedType.ContainingNamespace.FullName;

                case IArrayType arrayType:
                    type = arrayType.ElementType;

                    continue;

                case IPointerType pointerType:
                    type = pointerType.PointedAtType;

                    continue;

                default:
                    // For other non-named types (e.g., dynamic), no namespace is available.
                    return null;
            }
        }
    }

    internal static Type ResolveCompileTimeTypeOf( string typeId, string? ns, string name, string fullName, string toString )
    {
        return Current.CompilationContext.AssertNotNull()
            .CompileTimeTypeFactory
            .Get( new SerializableTypeId( typeId ), new CompileTimeTypeMetadata( ns, name, fullName, toString ) );
    }

    IDisposable IExecutionContext.WithoutDependencyCollection() => this.WithoutDependencyCollection();

    internal DisposeAction WithoutDependencyCollection()
    {
        if ( this._dependencyCollector == null )
        {
            return default;
        }
        else
        {
            var previousValue = this._collectDependencyDisabled;
            this._collectDependencyDisabled = true;

            return new DisposeAction( () => this._collectDependencyDisabled = previousValue );
        }
    }

    public static DisposeAction WithContext( UserCodeExecutionContext? context )
    {
        if ( context == null )
        {
            return default;
        }

        var oldContext = MetalamaExecutionContext.CurrentOrNull;
        MetalamaExecutionContext.CurrentOrNull = context;
        var oldCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = MetalamaStringFormatter.Instance;

        return new DisposeAction(
            () =>
            {
                MetalamaExecutionContext.CurrentOrNull = oldContext;
                CultureInfo.CurrentCulture = oldCulture;
            } );
    }

    /// <summary>
    /// Creates a <see cref="UserCodeExecutionContext"/> for a given <see cref="CompilationModel"/>. 
    /// </summary>
    [PublicAPI]
    public static DisposeAction WithContext( in ProjectServiceProvider serviceProvider, CompilationModel compilation, string? description = null )
        => WithContext(
            new UserCodeExecutionContext(
                serviceProvider,
                UserCodeDescription.Create( description ?? "executing a test method" ),
                compilation.CompilationContext,
                compilationModel: compilation ) );

    internal static DisposeAction WithContext( in ProjectServiceProvider serviceProvider, CompilationModel compilation, UserCodeDescription description )
        => WithContext( new UserCodeExecutionContext( serviceProvider, description, compilation.CompilationContext, compilationModel: compilation ) );

    internal UserCodeExecutionContext(
        ProjectServiceProvider serviceProvider,
        UserCodeDescription description,
        CompilationModel compilationModel,
        AspectLayerId? aspectAspectLayerId = null,
        IDeclaration? targetDeclaration = null,
        ISyntaxBuilderImpl? syntaxBuilder = null,
        MetaApi? metaApi = null,
        IDiagnosticAdder? diagnostics = null,
        ImmutableArray<SyntaxTree> sourceTrees = default ) : this(
        serviceProvider,
        description,
        compilationModel.CompilationContext,
        aspectAspectLayerId,
        compilationModel,
        targetDeclaration,
        syntaxBuilder,
        metaApi,
        diagnostics,
        sourceTrees ) { }

    public static UserCodeExecutionContext CreateInstance(
        ProjectServiceProvider serviceProvider,
        UserCodeDescription description,
        CompilationContext? compilationContext,
        IDiagnosticAdder? diagnostics = null )
        => new( serviceProvider, description, compilationContext, diagnostics: diagnostics );

    public static UserCodeExecutionContext CreateInstance(
        ProjectServiceProvider serviceProvider,
        UserCodeDescription description,
        CompilationModel compilation,
        IDiagnosticAdder? diagnostics = null )
        => new( serviceProvider, description, compilation, diagnostics: diagnostics );

    /// <summary>
    /// Initializes a new instance of the <see cref="UserCodeExecutionContext"/> class that can be used
    /// to invoke user code using <see cref="UserCodeInvoker.Invoke"/> but not <see cref="UserCodeInvoker.TryInvoke{T}"/>.
    /// </summary>
    internal UserCodeExecutionContext(
        ProjectServiceProvider serviceProvider,
        UserCodeDescription description,
        CompilationContext? compilationContext = null,
        AspectLayerId? aspectAspectLayerId = null,
        CompilationModel? compilationModel = null,
        IDeclaration? targetDeclaration = null,
        ISyntaxBuilderImpl? syntaxBuilder = null,
        MetaApi? metaApi = null,
        IDiagnosticAdder? diagnostics = null,
        ImmutableArray<SyntaxTree> sourceTrees = default,
        bool throwOnUnsupportedDependencies = false )
    {
        var current = CurrentOrNull;
        this.Description = description;
        this.ServiceProvider = serviceProvider;
        this.AspectLayerId = aspectAspectLayerId;
        this.Compilation = compilationModel ?? current?.Compilation;
        this.CompilationContext = compilationContext ?? current?.CompilationContext;
        this.TargetDeclaration = targetDeclaration ?? current?.TargetDeclaration;
        this._dependencyCollector = serviceProvider.GetService<IDependencyCollector>();
        this._targetType = targetDeclaration?.GetTopmostNamedType();
        this.MetaApi = metaApi ?? current?.MetaApi;
        this._diagnosticAdder = diagnostics ?? current?._diagnosticAdder;
        this._throwOnUnsupportedDependencies = throwOnUnsupportedDependencies;
        this._sourceTrees = sourceTrees;
        this._syntaxBuilder = GetSyntaxBuilder( compilationModel, targetDeclaration, serviceProvider, syntaxBuilder, metaApi, current?._syntaxBuilder );
    }

    private protected UserCodeExecutionContext( UserCodeExecutionContext prototype )
    {
        this.ServiceProvider = prototype.ServiceProvider;
        this.AspectLayerId = prototype.AspectLayerId;
        this.Compilation = prototype.Compilation;
        this.CompilationContext = prototype.CompilationContext;
        this._diagnosticAdder = prototype._diagnosticAdder;
        this._throwOnUnsupportedDependencies = prototype._throwOnUnsupportedDependencies;
        this.Description = prototype.Description;
        this.TargetDeclaration = prototype.TargetDeclaration;
        this._sourceTrees = prototype._sourceTrees;
        this._dependencyCollector = prototype._dependencyCollector;
        this._targetType = prototype._targetType;
        this.CompilationContext = prototype.CompilationContext;
        this._syntaxBuilder = prototype._syntaxBuilder;
        this.MetaApi = prototype.MetaApi;
    }

    private UserCodeExecutionContext( UserCodeExecutionContext prototype, UserCodeDescription description ) : this( prototype )
    {
        this.Description = description;
    }

    private UserCodeExecutionContext( UserCodeExecutionContext prototype, CompilationModel compilation, IDiagnosticAdder diagnostics ) : this( prototype )
    {
        this._diagnosticAdder = diagnostics;

        if ( !ReferenceEquals( prototype.Compilation, compilation ) )
        {
            if ( this.MetaApi != null )
            {
                // TODO: Translate the MetaApi.
                throw new AssertionFailedException();
            }

            this.Compilation = compilation;
            this.CompilationContext = compilation.CompilationContext;
            this.TargetDeclaration = prototype.TargetDeclaration?.ForCompilation( compilation );
            this._syntaxBuilder = GetSyntaxBuilder( compilation, this.TargetDeclaration, this.ServiceProvider, this._syntaxBuilder );
        }
    }

    private static ISyntaxBuilderImpl? GetSyntaxBuilder(
        CompilationModel? compilation,
        IDeclaration? currentDeclaration,
        ProjectServiceProvider serviceProvider,
        ISyntaxBuilderImpl? syntaxBuilder1 = null,
        ISyntaxBuilderImpl? syntaxBuilder2 = null,
        ISyntaxBuilderImpl? syntaxBuilder3 = null )
    {
        if ( compilation == null )
        {
            return null;
        }

        bool CanReuse( ISyntaxBuilderImpl? syntaxBuilder )
            => syntaxBuilder != null && ReferenceEquals( syntaxBuilder.Compilation, compilation )
                                     && (currentDeclaration == null || ReferenceEquals( syntaxBuilder.CurrentDeclaration, currentDeclaration ));

        if ( CanReuse( syntaxBuilder1 ) )
        {
            return syntaxBuilder1;
        }

        if ( CanReuse( syntaxBuilder2 ) )
        {
            return syntaxBuilder2;
        }

        if ( CanReuse( syntaxBuilder3 ) )
        {
            return syntaxBuilder3;
        }

        var syntaxGenerationOptions = serviceProvider.GetService<SyntaxGenerationOptions>();

        if ( syntaxGenerationOptions == null )
        {
            return null;
        }

        return new SyntaxBuilderImpl(
            compilation,
            syntaxGenerationOptions,
            currentDeclaration );
    }

    internal IDiagnosticAdder Diagnostics
        => this._diagnosticAdder ?? throw new InvalidOperationException( "Cannot report diagnostics in a context without diagnostics adder." );

    // This property is intentionally writable because it allows us to reuse the same context for several calls, when performance
    // is critical. This feature is used by validators.
    public UserCodeDescription Description { get; set; }

    internal IDeclaration? TargetDeclaration { get; }

    internal ProjectServiceProvider ServiceProvider { get; }

    IServiceProvider<IProjectService> IExecutionContext.ServiceProvider => this.ServiceProvider.Underlying;

    public IFormatProvider FormatProvider => MetalamaStringFormatter.Instance;

    internal AspectLayerId? AspectLayerId { get; }

    public CompilationContext? CompilationContext { get; }

    internal CompilationModel? Compilation { get; }

    ISyntaxBuilderImpl? IExecutionContextInternal.SyntaxBuilder => this._syntaxBuilder;

    IMetaApi? IExecutionContextInternal.MetaApi => this.MetaApi;

    [field: AllowNull]
    IExpressionHelper IExecutionContextInternal.ExpressionHelper => field ??= new ExpressionHelper( SyntaxGenerationContext.Contextless );

    private protected MetaApi? MetaApi { get; }

    [Memo]
    public IExecutionScenario ExecutionScenario => this.ServiceProvider.GetRequiredService<ExecutionScenario>();

    ICompilation IExecutionContext.Compilation
        => this.Compilation ?? throw new InvalidOperationException( "There is no compilation in the current execution context" );

    public IDeclaration? DiagnosticDeclaration => this.MetaApi?.DiagnosticDeclaration ?? this.TargetDeclaration;

    internal UserCodeExecutionContext WithDescription( UserCodeDescription description ) => new( this, description );

    internal UserCodeExecutionContext WithCompilationAndDiagnosticAdder( CompilationModel compilation, IDiagnosticAdder diagnostics )
    {
        if ( ReferenceEquals( this.Compilation, compilation ) && diagnostics == this.Diagnostics )
        {
            return this;
        }

        return new UserCodeExecutionContext( this, compilation, diagnostics );
    }

    internal void AddDependencyFrom( IDeclaration declaration )
    {
        // Prevent infinite recursion while getting the declaring type.
        // We assume that there is one instance of this class per execution context and that it is single-threaded.

        if ( this._collectDependencyDisabled )
        {
            return;
        }

        this._collectDependencyDisabled = true;

        try
        {
            if ( this._dependencyCollector != null && this._targetType != null )
            {
                var declaringType = declaration.GetTopmostNamedType();

                if ( declaringType != null && !declaringType.Equals( this._targetType ) )
                {
                    this._dependencyCollector.AddDependency(
                        declaringType.GetSymbol().AssertSymbolNullNotImplemented( UnsupportedFeatures.Uncategorized ),
                        this._targetType.GetSymbol().AssertSymbolNullNotImplemented( UnsupportedFeatures.Uncategorized ) );
                }
            }
        }
        finally
        {
            this._collectDependencyDisabled = false;
        }
    }

    internal void AddDependencyTo( SyntaxTree syntaxTree )
    {
        if ( this._dependencyCollector != null )
        {
            if ( this._targetType != null )
            {
                this._dependencyCollector.AddDependency(
                    this._targetType.GetSymbol().AssertSymbolNullNotImplemented( UnsupportedFeatures.Uncategorized ),
                    syntaxTree );
            }
            else if ( this._sourceTrees is { IsDefaultOrEmpty: false } sourceTrees )
            {
                foreach ( var sourceTree in sourceTrees )
                {
                    this._dependencyCollector.AddDependency( sourceTree, syntaxTree );
                }
            }
        }
    }

    internal void OnUnsupportedDependency( string api )
    {
        if ( this._throwOnUnsupportedDependencies && this._dependencyCollector != null && !this._collectDependencyDisabled )
        {
            throw new InvalidOperationException(
                $"'The '{api}' API is not supported in the BuildAspect context at design time. " +
                $"It is only supported in the context of a adding new aspects ({nameof(IQuery<IDeclaration>)}.{nameof(IQuery<IDeclaration>.Select)})' and sibling methods."
                +
                $"You can use {nameof(MetalamaExecutionContext)}.{nameof(MetalamaExecutionContext.Current)}.{nameof(IExecutionContext.ExecutionScenario)}.{nameof(IExecutionScenario.IsDesignTime)} to run your code at design time only." );
        }
    }
}