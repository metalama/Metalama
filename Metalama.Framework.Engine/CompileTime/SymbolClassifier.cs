// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.CompileTime;

/// <summary>
/// The main implementation of <see cref="ISymbolClassifier"/>.
/// </summary>
internal sealed class SymbolClassifier : ISymbolClassifier
{
    /// <summary>
    /// List of well-known types, for which the scope is overriden (i.e. this list takes precedence over any other rule).
    /// </summary>
    private static readonly ImmutableDictionary<string, (string Namespace, TemplatingScope? Scope)> _wellKnownTypes =
        new (Type ReflectionType, TemplatingScope? Scope)[]
            {
                // We don't want users to interact with a few classes so we mark then RunTimeOnly.
                // However, we can't make Debugger run-time-only because it's the only way to debug compile-time code at the moment.
                (typeof(Console), TemplatingScope.RunTimeOnly),
                (typeof(GC), TemplatingScope.RunTimeOnly),
                (typeof(Debug), TemplatingScope.RunTimeOnly),
                (typeof(Trace), TemplatingScope.RunTimeOnly),
                (typeof(GCCollectionMode), TemplatingScope.RunTimeOnly),
                (typeof(GCNotificationStatus), TemplatingScope.RunTimeOnly),
                (typeof(STAThreadAttribute), TemplatingScope.RunTimeOnly),
                (typeof(AppDomain), TemplatingScope.RunTimeOnly),
                (typeof(Process), TemplatingScope.RunTimeOnly),
                (typeof(Thread), TemplatingScope.RunTimeOnly),
                (typeof(ExecutionContext), TemplatingScope.RunTimeOnly),
                (typeof(SynchronizationContext), TemplatingScope.RunTimeOnly),
                (typeof(Environment), TemplatingScope.RunTimeOnly),
                (typeof(RuntimeEnvironment), TemplatingScope.RunTimeOnly),
                (typeof(RuntimeInformation), TemplatingScope.RunTimeOnly),
                (typeof(Marshal), TemplatingScope.RunTimeOnly),
                (typeof(Index), TemplatingScope.RunTimeOrCompileTime),
                (typeof(Range), TemplatingScope.RunTimeOrCompileTime)
            }.ToImmutableDictionary(
                t => t.ReflectionType.Name.AssertNotNull(),
                t => (t.ReflectionType.Namespace.AssertNotNull(), t.Scope) )

            // This system type is .NET Framework-only but does not affect the scope.
            .Add( "_Attribute", ("System.Runtime.InteropServices", null) );

    /// <summary>
    /// List of well-known members, for which the scope is overriden (i.e. this list takes precedence over any other rule, including well-known types).
    /// Matching of members is currently only done by name.
    /// </summary>
    private static readonly ImmutableDictionary<(string Type, string Member), (string Namespace, TemplatingScope? Scope)> _wellKnownMembers =
        new (Type Type, string[] MemberNames, TemplatingScope? Scope)[]
            {
                (typeof(DateTime), [nameof(DateTime.Now), nameof(DateTime.Today), nameof(DateTime.UtcNow)], TemplatingScope.RunTimeOnly),
                (typeof(DateTimeOffset), [nameof(DateTimeOffset.Now), nameof(DateTimeOffset.UtcNow)], TemplatingScope.RunTimeOnly)
            }.SelectMany( t => t.MemberNames.SelectAsReadOnlyList( memberName => (t.Type, MemberName: memberName, t.Scope) ) )
            .ToImmutableDictionary(
                t => (t.Type.Name.AssertNotNull(), t.MemberName),
                t => (t.Type.Namespace.AssertNotNull(), t.Scope) );

    public static SymbolClassifier GetSymbolClassifier( in ProjectServiceProvider serviceProvider, CompilationContext compilationContext )
        => new( serviceProvider, compilationContext );

    private readonly Compilation _compilation;
    private readonly INamedTypeSymbol? _templateAttribute;
    private readonly INamedTypeSymbol? _declarativeAdviceAttribute;

    private readonly ConcurrentDictionary<ISymbol, TemplatingScope?>[] _caches;

    private readonly ConcurrentDictionary<ISymbol, TemplateInfo> _cacheInheritedTemplateInfo;
    private readonly ConcurrentDictionary<ISymbol, TemplateInfo> _cacheNonInheritedTemplateInfo;
    private readonly ConcurrentDictionary<ISymbol, bool> _cacheIsTemplateOnly;

    private readonly ReferenceAssemblyLocator _referenceAssemblyLocator;
    private readonly IAttributeDeserializer _attributeDeserializer;
    private readonly ILogger _logger;
    private readonly bool _roslynIsCompileTimeOnly;
    private readonly IEqualityComparer<ISymbol> _symbolEqualityComparer;
    private readonly CompilationContext _compilationContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SymbolClassifier"/> class.
    /// </summary>
    /// <param name="referenceAssemblyLocator"></param>
    /// <param name="compilation">The compilation, or null if the compilation has no reference to Metalama.</param>
    private SymbolClassifier( in ProjectServiceProvider serviceProvider, CompilationContext compilationContext )
        : this(
            serviceProvider,
            compilationContext.Compilation,
            serviceProvider.GetRequiredService<SystemAttributeDeserializer.Provider>().Get( compilationContext ),
            serviceProvider.GetReferenceAssemblyLocator() ) { }

    private SymbolClassifier(
        ProjectServiceProvider serviceProvider,
        Compilation compilation,
        IAttributeDeserializer attributeDeserializer,
        ReferenceAssemblyLocator referenceAssemblyLocator )
    {
        var compilationContext = compilation.GetCompilationContext();

        this._compilationContext = compilationContext;

        this._referenceAssemblyLocator = referenceAssemblyLocator;
        this._symbolEqualityComparer = compilationContext.SymbolComparer;

        this._cacheNonInheritedTemplateInfo = new ConcurrentDictionary<ISymbol, TemplateInfo>( this._symbolEqualityComparer );
        this._cacheInheritedTemplateInfo = new ConcurrentDictionary<ISymbol, TemplateInfo>( this._symbolEqualityComparer );
        this._cacheIsTemplateOnly = new ConcurrentDictionary<ISymbol, bool>( this._symbolEqualityComparer );

        this._caches = Enumerable.Range( 0, (int) GetTemplatingScopeOptions.Count )
            .Select( _ => new ConcurrentDictionary<ISymbol, TemplatingScope?>( this._symbolEqualityComparer ) )
            .ToArray();

        this._attributeDeserializer = attributeDeserializer;
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( "SymbolClassifier" );

        this._roslynIsCompileTimeOnly = serviceProvider.GetRequiredService<IProjectOptions>().RoslynIsCompileTimeOnly;

        var hasMetalamaReference = compilation.GetTypeByMetadataName( typeof(RunTimeOrCompileTimeAttribute).FullName.AssertNotNull() ) != null;
        this._compilation = compilation;

        if ( hasMetalamaReference )
        {
            this._templateAttribute = this._compilation.GetTypeByMetadataName( typeof(TemplateAttribute).FullName.AssertNotNull() ).AssertNotNull();

            this._declarativeAdviceAttribute = this._compilation.GetTypeByMetadataName( typeof(DeclarativeAdviceAttribute).FullName.AssertNotNull() )
                .AssertNotNull();
        }
    }

    public TemplateInfo GetTemplateInfo( ISymbol symbol ) => this.GetTemplateInfo( symbol, false );

    private TemplateInfo GetTemplateInfo( ISymbol symbol, bool isInherited )
        => isInherited
            ? this._cacheInheritedTemplateInfo.GetOrAdd( symbol, static ( s, x ) => x.GetTemplateInfoCore( s, true ), this )
            : this._cacheNonInheritedTemplateInfo.GetOrAdd( symbol, static ( s, x ) => x.GetTemplateInfoCore( s, false ), this );

    private TemplateInfo GetTemplateInfoCore( ISymbol symbol, bool isInherited )
    {
        if ( this._templateAttribute == null || this._declarativeAdviceAttribute == null )
        {
            // The compilation does not have any reference to Metalama.
            return TemplateInfo.None;
        }

        // Look for a [Template] attribute on the symbol.
        var templateAttribute = symbol
            .GetAttributes()
            .FirstOrDefault( a => this.IsAttributeOfType( a, this._templateAttribute ) || this.IsAttributeOfType( a, this._declarativeAdviceAttribute ) );

        if ( templateAttribute != null )
        {
            var templateInfo = this.GetTemplateInfo( symbol, templateAttribute );

            if ( !templateInfo.IsNone )
            {
                // Ignore any abstract member.
                if ( !isInherited && (symbol.IsAbstract
                                      || templateAttribute.NamedArguments.Any( a => a.Key == nameof(TemplateAttribute.IsEmpty) && (bool) a.Value.Value! )) )
                {
                    return templateInfo.AsAbstract();
                }
                else if ( symbol.IsExtern )
                {
                    // Extern members indicate template without body.
                    return templateInfo.WithoutBody();
                }
                else
                {
                    return templateInfo;
                }
            }
        }

        if ( symbol is IMethodSymbol { AssociatedSymbol: { } associatedSymbol }
             && this.GetTemplateInfo( associatedSymbol, isInherited ) is { IsNone: false } associatedTemplateInfo )
        {
            return associatedTemplateInfo;
        }

        // ReSharper disable once PossibleUnintendedReferenceComparison
        if ( symbol.OriginalDefinition != symbol
             && this.GetTemplateInfo( symbol.OriginalDefinition, isInherited ) is { IsNone: false } originalDefinitionTemplateInfo )
        {
            return originalDefinitionTemplateInfo;
        }

        if ( symbol.GetOverriddenMember() is { } overriddenMember )
        {
            return this.GetTemplateInfo( overriddenMember, true );
        }

        return TemplateInfo.None;
    }

    private bool IsAttributeOfType( AttributeData a, ITypeSymbol type ) => this._compilation.HasImplicitConversion( a.AttributeClass, type );

    private TemplateInfo GetTemplateInfo( ISymbol declaringSymbol, AttributeData attributeData )
    {
        if ( !this._attributeDeserializer.TryCreateAttribute( attributeData, NullDiagnosticAdder.Instance, out var attributeInstance ) )
        {
            // This happens when the attribute class is defined in user code.
            // In this case, we have to instantiate the attribute later, after we have the compile-time assembly for the user code.

            // It also happens in case of mismatch between the current Metalama version and the Metalama version to which the project is
            // linked, which should not happen in theory.

            this._logger.Warning?.Log( $"Could not instantiate an attribute of type '{attributeData.AttributeClass}'." );
        }

        var memberId = SymbolId.Create( declaringSymbol );

        var templateAttributeType = attributeData.AttributeClass?.Name switch
        {
            nameof(TemplateAttribute) or nameof(TestTemplateAttribute) => TemplateAttributeType.Template,
            nameof(InterfaceMemberAttribute) => TemplateAttributeType.InterfaceMember,
            _ => TemplateAttributeType.DeclarativeAdvice
        };

        return new TemplateInfo( memberId, templateAttributeType, (IAdviceAttribute?) attributeInstance );
    }

    private static TemplatingScope? GetTemplatingScope( AttributeData attribute )
        => attribute.AttributeClass?.Name switch
        {
            nameof(CompileTimeAttribute) => TemplatingScope.CompileTimeOnly,
            nameof(RunTimeAttribute) => TemplatingScope.RunTimeOnly,
            nameof(CompileTimeReturningRunTimeAttribute) => TemplatingScope.CompileTimeOnlyReturningRuntimeOnly,
            nameof(TemplateAttribute) => TemplatingScope.CompileTimeOnly,
            nameof(RunTimeOrCompileTimeAttribute) => TemplatingScope.RunTimeOrCompileTime,
            nameof(IntroduceAttribute) => TemplatingScope.RunTimeOnly,
            nameof(InterfaceMemberAttribute) => TemplatingScope.RunTimeOnly,
            _ => null
        };

    private static TemplatingScope? GetAssemblyScope( IAssemblySymbol? assembly )
    {
        if ( assembly == null )
        {
            return null;
        }

        if ( assembly.Name == "Metalama.Compiler.Interface" )
        {
            return TemplatingScope.CompileTimeOnly;
        }

        var scopeFromAttributes = assembly.GetAttributes()
            .Concat( assembly.Modules.First().GetAttributes() )
            .SelectAsArray( GetTemplatingScope )
            .FirstOrDefault( s => s != null );

        if ( scopeFromAttributes != null )
        {
            return scopeFromAttributes.Value;
        }

        return null;
    }

    public TemplatingScope GetTemplatingScope( ISymbol symbol )
    {
        symbol.ThrowIfBelongsToDifferentCompilationThan( this._compilationContext );

        return this.GetTemplatingScopeCore( symbol, GetTemplatingScopeOptions.Default, ImmutableLinkedList<ISymbol>.Empty, null )
            .GetValueOrDefault( TemplatingScope.RunTimeOnly );
    }

    public bool IsTemplateOnly( ISymbol symbol ) => this._cacheIsTemplateOnly.GetOrAdd( symbol, static ( s, x ) => x.IsTemplateOnlyCore( s ), this );

    private bool IsTemplateOnlyCore( ISymbol symbol )
    {
        // Symbols that aren't compile-time-only can't be template-only.
        var isCompileTimeOnly = this.GetTemplatingScope( symbol ).GetExpressionExecutionScope() == TemplatingScope.CompileTimeOnly;

        if ( !isCompileTimeOnly )
        {
            return false;
        }

        // Check whether the symbol is marked with [CompileTime(isTemplateOnly: true)].
        var compileTimeAttribute = symbol.GetAttributes().FirstOrDefault( a => a.AttributeClass?.Name == nameof(CompileTimeAttribute) );

        if ( compileTimeAttribute is { ConstructorArguments: [{ Value: true }, ..] } )
        {
            return true;
        }

        // Check the containing symbol.
        if ( symbol.ContainingSymbol is { } containingSymbol && this.IsTemplateOnly( containingSymbol ) )
        {
            return true;
        }

        // Check whether any type in the symbol signature is dynamic.
        var types = symbol switch
        {
            IMethodSymbol method => method.Parameters.Select( p => p.Type ).Append( method.ReturnType ),
            IPropertySymbol property => property.Parameters.Select( p => p.Type ).Append( property.Type ),
            IFieldSymbol field => [field.Type],
            IEventSymbol @event => [@event.Type],
            _ => []
        };

        if ( types.Any( t => this.GetTemplatingScope( t ) == TemplatingScope.Dynamic ) )
        {
            return true;
        }

        return false;
    }

    public void ReportScopeError( SyntaxNode node, ISymbol symbol, IDiagnosticAdder diagnosticAdder )
    {
        var tracer = new SymbolClassifierTracer( symbol );

        _ = this.GetTemplatingScopeCore( symbol, GetTemplatingScopeOptions.Default, ImmutableLinkedList<ISymbol>.Empty, tracer );

        var conflictNode = tracer.SelectManyRecursive( t => t.Children, true )
            .Where( t => t.Result is TemplatingScope.Conflict )
            .MaxByOrNull( t => t.Depth );

        if ( conflictNode == null )
        {
            // Nothing to report.
        }
        else if ( conflictNode.Result == TemplatingScope.DynamicTypeConstruction )
        {
            diagnosticAdder.Report(
                TemplatingDiagnosticDescriptors.InvalidDynamicTypeConstruction.CreateRoslynDiagnostic(
                    node.GetDiagnosticLocation(),
                    symbol.ToDisplayString( SymbolDisplayFormat.CSharpErrorMessageFormat ) ) );
        }
        else
        {
            var firstRunTimeOnly = conflictNode.Children.FirstOrDefault( c => c.Result == TemplatingScope.RunTimeOnly );
            var firstCompileTimeOnly = conflictNode.Children.FirstOrDefault( c => c.Result == TemplatingScope.CompileTimeOnly );

            if ( firstCompileTimeOnly == null || firstRunTimeOnly == null )
            {
                // Cannot find the reason.
                diagnosticAdder.Report(
                    TemplatingDiagnosticDescriptors.UnexplainedTemplatingScopeConflict.CreateRoslynDiagnostic( node.GetDiagnosticLocation(), symbol ) );
            }
            else
            {
                diagnosticAdder.Report(
                    TemplatingDiagnosticDescriptors.TemplatingScopeConflict.CreateRoslynDiagnostic(
                        node.GetDiagnosticLocation(),
                        (symbol, firstRunTimeOnly.Symbol!, firstCompileTimeOnly.Symbol!) ) );
            }
        }
    }

    // This method exists so that it is easy to put a breakpoint on conflict.
    private static TemplatingScope OnConflict() => TemplatingScope.Conflict;

    private TemplatingScope? GetTemplatingScopeCore(
        ISymbol symbol,
        GetTemplatingScopeOptions options,
        ImmutableLinkedList<ISymbol> symbolsBeingProcessed,
        SymbolClassifierTracer? parentTracer )
    {
        this.CheckRecursion( symbolsBeingProcessed );

        if ( symbol.Kind == SymbolKind.Namespace )
        {
            return TemplatingScope.RunTimeOrCompileTime;
        }

        // Recursion happens when are are classifying a type like `class C : IEquatable<C>`. The recursion in this example is on C.
        // We need to return the method before the result is cached.
        if ( symbolsBeingProcessed.Contains( symbol, this._symbolEqualityComparer ) )
        {
            return null;
        }

        // Cache lookup.
        var cache = this._caches[(int) options];

        TemplatingScope? scope;

        if ( parentTracer == null )
        {
            if ( cache.TryGetValue( symbol, out scope ) )
            {
                return scope;
            }
        }
        else
        {
            // Don't use cache when we are tracing.
        }

        var tracer = parentTracer?.CreateChild( symbol );

        var symbolsBeingProcessedIncludingCurrent = symbolsBeingProcessed.Insert( symbol );

        scope = GetRawScope();

        // Fix compile-time-only symbols according to their expression type.
        if ( scope == TemplatingScope.CompileTimeOnly )
        {
            if ( symbol.GetExpressionType() is { } expressionType )
            {
                var expressionTypeScope = this.GetTemplatingScopeCore( expressionType, options, symbolsBeingProcessedIncludingCurrent, tracer );

                switch ( expressionTypeScope )
                {
                    case TemplatingScope.RunTimeOnly:
                    case TemplatingScope.Dynamic:
                    case TemplatingScope.CompileTimeOnlyReturningRuntimeOnly:
                        scope = TemplatingScope.CompileTimeOnlyReturningRuntimeOnly;

                        break;

                    case TemplatingScope.RunTimeOrCompileTime:
                        scope = TemplatingScope.CompileTimeOnlyReturningBoth;

                        break;
                }

                // If the return type is marked [CompileTime] (as in meta.CompileTime), enforce that.
                if ( symbol is IMethodSymbol methodSymbol
                     && methodSymbol.GetReturnTypeAttributes().Any( a => a.AttributeClass?.Name == nameof(CompileTimeAttribute) ) )
                {
                    scope = scope == TemplatingScope.CompileTimeOnlyReturningRuntimeOnly ? OnConflict() : TemplatingScope.CompileTimeOnly;
                }
            }
            else if ( symbol is ITypeParameterSymbol { DeclaringMethod: { } declaringMethod } && !this.GetTemplateInfo( declaringMethod ).IsNone )
            {
                // Compile-time template parameters always represent run-time types.
                scope = TemplatingScope.CompileTimeOnlyReturningRuntimeOnly;
            }
        }

        tracer?.SetResult( scope );

        // Add to cache.

        cache.TryAdd( symbol, scope );

        return scope;

        TemplatingScope? GetRawScope()
        {
            // From well-known types.

            if ( this.TryGetWellKnownScope( symbol, options, out var scopeFromWellKnown ) )
            {
                return scopeFromWellKnown;
            }

            switch ( symbol )
            {
                // Dynamic.
                case IDynamicTypeSymbol:
                    return TemplatingScope.Dynamic;

                // Type parameters.
                case ITypeParameterSymbol typeParameterSymbol:
                    var scopeFromAttribute = GetScopeFromAttributes( tracer, typeParameterSymbol );

                    if ( scopeFromAttribute == TemplatingScope.CompileTimeOnly && typeParameterSymbol.ContainingSymbol is IMethodSymbol m
                                                                               && !this.GetTemplateInfo( m ).IsNone )
                    {
                        return TemplatingScope.CompileTimeOnlyReturningRuntimeOnly;
                    }

                    if ( scopeFromAttribute != null )
                    {
                        return scopeFromAttribute.Value;
                    }
                    else if ( typeParameterSymbol.ContainingSymbol.Kind == SymbolKind.Method
                              && !this.GetTemplateInfo( typeParameterSymbol.ContainingSymbol ).IsNone )
                    {
                        // Template parameters are run-time by default.
                        return TemplatingScope.RunTimeOnly;
                    }
                    else if ( (options & GetTemplatingScopeOptions.TypeParametersAreNeutral) != 0 )
                    {
                        // Do not try to go to the containing symbol if we are called from the method level because this would
                        // create an infinite recursion.

                        return null;
                    }
                    else
                    {
                        var declaringScope = this.GetTemplatingScopeCore(
                            typeParameterSymbol.ContainingSymbol,
                            options,
                            symbolsBeingProcessedIncludingCurrent,
                            tracer );

                        return declaringScope;
                    }

                // Error (unresolved types).
                case IErrorTypeSymbol:
                    // We treat all error symbols as run-time to avoid including error types in the compile-time compilations,
                    // which may cause a high number of errors during the solution load at design time.
                    return TemplatingScope.RunTimeOnly;

                // Array.
                case IArrayTypeSymbol array:
                    {
                        var elementScope = this.GetTemplatingScopeCore( array.ElementType, options, symbolsBeingProcessedIncludingCurrent, tracer );

                        if ( elementScope is TemplatingScope.Dynamic )
                        {
                            return TemplatingScope.DynamicTypeConstruction;
                        }
                        else
                        {
                            return elementScope?.GetExpressionValueScope();
                        }
                    }

                // Pointers.
                case IPointerTypeSymbol pointer:
                    return this.GetTemplatingScopeCore( pointer.PointedAtType, options, symbolsBeingProcessedIncludingCurrent, tracer );

                // Generic type instances.
                case INamedTypeSymbol { IsGenericType: true } namedType when !namedType.IsGenericTypeDefinition():
                    {
                        List<TemplatingScope?> scopes = new( namedType.TypeArguments.Length + 1 );

                        var declarationScope = this.GetTemplatingScopeCore(
                            namedType.OriginalDefinition,
                            options,
                            symbolsBeingProcessedIncludingCurrent,
                            tracer );

                        scopes.Add( declarationScope );

                        scopes.AddRange(
                            namedType.TypeArguments.Select(
                                arg => this.GetTemplatingScopeCore(
                                    arg,
                                    options | GetTemplatingScopeOptions.TypeParametersAreNeutral,
                                    symbolsBeingProcessedIncludingCurrent,
                                    tracer ) ) );

                        var compileTimeOnlyCount = 0;
                        var runTimeCount = 0;
                        var runTimeOrCompileTimeCount = 0;

                        foreach ( var typeArgumentScope in scopes )
                        {
                            switch ( typeArgumentScope )
                            {
                                case null:
                                    break;

                                case TemplatingScope.Dynamic:
                                    // Only a few well-known types can have dynamic generic arguments, others are unsupported.
                                    switch ( namedType.Name )
                                    {
                                        case nameof(Task<object>):
                                        case nameof(ConfiguredTaskAwaitable<object>):
                                        case nameof(ValueTask<object>):
                                        case nameof(IEnumerable<object>):
                                        case nameof(IEnumerator<object>):
                                        case nameof(IAsyncEnumerable<object>):
                                        case nameof(ConfiguredCancelableAsyncEnumerable<object>):
                                        case nameof(IAsyncEnumerator<object>):
                                            return TemplatingScope.Dynamic;

                                        default:
                                            return TemplatingScope.DynamicTypeConstruction;
                                    }

                                case TemplatingScope.RunTimeOnly:
                                case TemplatingScope.CompileTimeOnlyReturningRuntimeOnly:
                                    runTimeCount++;

                                    break;

                                case TemplatingScope.CompileTimeOnly:
                                    compileTimeOnlyCount++;

                                    break;

                                case TemplatingScope.RunTimeOrCompileTime:
                                    runTimeOrCompileTimeCount++;

                                    break;

                                case TemplatingScope.Conflict:
                                    return TemplatingScope.Conflict;

                                case TemplatingScope.DynamicTypeConstruction:
                                    return TemplatingScope.DynamicTypeConstruction;

                                default:
                                    throw new AssertionFailedException( $"Unexpected scope: {typeArgumentScope}." );
                            }
                        }

                        switch ( runTimeCount )
                        {
                            case > 0 when compileTimeOnlyCount > 0:
                                return OnConflict();

                            case > 0:
                                return TemplatingScope.RunTimeOnly;

                            default:
                                {
                                    if ( compileTimeOnlyCount > 0 )
                                    {
                                        return TemplatingScope.CompileTimeOnly;
                                    }
                                    else if ( runTimeOrCompileTimeCount > 0
                                              || (options & GetTemplatingScopeOptions.ImplicitRuntimeOrCompileTimeAsNull) == 0 )
                                    {
                                        return TemplatingScope.RunTimeOrCompileTime;
                                    }
                                    else
                                    {
                                        return null;
                                    }
                                }
                        }
                    }

                // Anonymous types.
                case INamedTypeSymbol { IsAnonymousType: true } anonymousType:
                    {
                        TemplatingScope? combinedScope = TemplatingScope.RunTimeOrCompileTime;

                        foreach ( var member in anonymousType.GetMembers() )
                        {
                            if ( member is IPropertySymbol property )
                            {
                                this.CombineScope(
                                    property.Type,
                                    GetTemplatingScopeOptions.Default,
                                    symbolsBeingProcessedIncludingCurrent,
                                    ref combinedScope,
                                    tracer );
                            }
                        }

                        return combinedScope;
                    }

                // Type definitions
                case INamedTypeSymbol namedType:
                    {
                        // Note: Type with [CompileTime] on a base type or an interface should be considered compile-time,
                        // even if it has a generic argument from an external assembly (which makes it run-time). So generic arguments should come last.

                        var combinedScope = GetScopeFromAttributes( tracer, namedType );
                        TemplatingScope? declaringTypeScope = null;

                        // Check the scope of the containing type.
                        if ( combinedScope == null )
                        {
                            if ( namedType.ContainingType != null )
                            {
                                // We do not check conflicts here. Errors must be reported by TemplateCodeValidator.

                                declaringTypeScope = this.GetTemplatingScopeCore(
                                    namedType.ContainingType,
                                    options,
                                    symbolsBeingProcessedIncludingCurrent,
                                    tracer );

                                if ( declaringTypeScope == TemplatingScope.CompileTimeOnly )
                                {
                                    return TemplatingScope.CompileTimeOnly;
                                }
                                else
                                {
                                    // Run-time type can contain fabrics.
                                    // Aspects can contain compile-time nested types but not run-time.
                                    // These rules should be enforced in TemplatingCodeValidator.
                                }
                            }
                            else
                            {
                                combinedScope = GetAssemblyScope( symbol.ContainingAssembly );
                            }
                        }

                        // We don't look at the rest if the scope is known at this point.
                        if ( combinedScope != null )
                        {
                            return combinedScope;
                        }

                        // From base type.
                        if ( namedType.BaseType != null )
                        {
                            this.CombineBaseTypeScope( namedType.BaseType, ref combinedScope, symbolsBeingProcessedIncludingCurrent, tracer );
                        }

                        // From implemented interfaces.
                        foreach ( var @interface in namedType.AllInterfaces )
                        {
                            this.CombineBaseTypeScope( @interface, ref combinedScope, symbolsBeingProcessedIncludingCurrent, tracer );
                        }

                        if ( combinedScope != null )
                        {
                            return combinedScope;
                        }

                        // From generic arguments.
                        if ( !namedType.IsGenericTypeDefinition() )
                        {
                            foreach ( var genericArgument in namedType.TypeArguments )
                            {
                                this.CombineBaseTypeScope( genericArgument, ref combinedScope, symbolsBeingProcessedIncludingCurrent, tracer );
                            }
                        }

                        // If a type is not classified after all these inference rules were evaluated,
                        // and if it is not a nested type,  we consider it is a run-time type.
                        return combinedScope ?? declaringTypeScope ?? TemplatingScope.RunTimeOnly;
                    }

                case INamespaceSymbol:
                    // Namespace can be either run-time, build-time or both. We don't do more now but we may have to do it based on assemblies defining the namespace.
                    return TemplatingScope.RunTimeOrCompileTime;

                case IParameterSymbol parameter:
                    {
                        var parameterScope = GetScopeFromAttributes( tracer, parameter );

                        if ( parameterScope != null )
                        {
                            return parameterScope;
                        }

                        parameterScope = this.GetTemplatingScopeCore( parameter.Type, options, symbolsBeingProcessedIncludingCurrent, tracer )
                            ?.GetExpressionValueScope();

                        if ( parameterScope == null && this.GetTemplateInfo( parameter.ContainingSymbol ).IsNone )
                        {
                            parameterScope = this.GetTemplatingScopeCore(
                                parameter.ContainingSymbol,
                                options,
                                symbolsBeingProcessedIncludingCurrent,
                                tracer );
                        }

                        return parameterScope;
                    }

                case ILocalSymbol:
                    // Local variables are classified by the template annotator. The SymbolClassifier can be called by other components
                    // for a local variable, but then it cannot give any answer. We could return null, but then the RunTime fallback would be
                    // applied. So we use RunTimeOrCompileTime.
                    return TemplatingScope.RunTimeOrCompileTime;

                case IDiscardSymbol:
                    return TemplatingScope.RunTimeOrCompileTime;

                case IFunctionPointerTypeSymbol:
                    return TemplatingScope.RunTimeOnly;

                // The default case covers all members.
                default:
                    {
                        // For templates, we do not analyze the signature.
                        var templateInfo = this.GetTemplateInfo( symbol );

                        if ( !templateInfo.IsNone )
                        {
                            if ( templateInfo.CanBeReferencedAsRunTimeCode )
                            {
                                // Introductions can be referenced from run-time code.
                                return TemplatingScope.RunTimeOnly;
                            }
                            else
                            {
                                // Other templates cannot be referenced anywhere, but this should be enforced elsewhere.
                                return TemplatingScope.CompileTimeOnly;
                            }
                        }

                        var memberScope = GetScopeFromAttributes( tracer, symbol );

                        // If we have no attribute, look at the associated symbol (property/event).
                        if ( memberScope == null && symbol is IMethodSymbol { AssociatedSymbol: { } associatedSymbol } )
                        {
                            memberScope = this.GetTemplatingScopeCore( associatedSymbol, options, symbolsBeingProcessedIncludingCurrent, tracer );
                        }

                        // If we still have no attribute, look at the containing symbol.
                        if ( memberScope == null && symbol.ContainingSymbol != null )
                        {
                            memberScope = this.GetTemplatingScopeCore( symbol.ContainingSymbol, options, symbolsBeingProcessedIncludingCurrent, tracer )
                                          ?? TemplatingScope.RunTimeOnly;

                            if ( memberScope == TemplatingScope.Conflict )
                            {
                                // If the declaring type has conflict scope, we consider it has neutral scope,
                                // otherwise we would report errors on all type members and this is confusing.
                                memberScope = null;
                            }
                        }

                        // If the scope is given by attributes, we do not try to guess by signature.
                        if ( memberScope != null && memberScope != TemplatingScope.RunTimeOrCompileTime )
                        {
                            return memberScope;
                        }

                        if ( (options & GetTemplatingScopeOptions.ImplicitRuntimeOrCompileTimeAsNull) != 0 )
                        {
                            throw new AssertionFailedException( $"The {options} option is not expected for members." );
                        }

                        var signatureMemberOptions = options | GetTemplatingScopeOptions.TypeParametersAreNeutral;

                        static TemplatingScope? ApplyDefault( TemplatingScope? s )
                        {
                            if ( s != null )
                            {
                                return s;
                            }
                            else
                            {
                                return TemplatingScope.RunTimeOrCompileTime;
                            }
                        }

                        switch ( symbol )
                        {
                            case IMethodSymbol method:
                                {
                                    TemplatingScope? signatureScope = null;
                                    this.CombineScope( method.ReturnType, signatureMemberOptions, symbolsBeingProcessed, ref signatureScope, tracer );

                                    foreach ( var parameter in method.Parameters )
                                    {
                                        this.CombineScope( parameter.Type, signatureMemberOptions, symbolsBeingProcessed, ref signatureScope, tracer );
                                    }

                                    var typeArguments = method.TypeArguments;

                                    if ( !typeArguments.IsDefaultOrEmpty )
                                    {
                                        foreach ( var typeArgument in typeArguments )
                                        {
                                            if ( typeArgument.Kind == SymbolKind.TypeParameter )
                                            {
                                                continue;
                                            }

                                            var typeArgumentScope = this.GetTemplatingScopeCore(
                                                    typeArgument,
                                                    options,
                                                    symbolsBeingProcessedIncludingCurrent,
                                                    tracer )
                                                ?.GetExpressionValueScope();

                                            if ( typeArgumentScope != TemplatingScope.RunTimeOrCompileTime && typeArgumentScope != signatureScope )
                                            {
                                                return OnConflict();
                                            }
                                        }
                                    }

                                    return ApplyDefault( signatureScope );
                                }

                            case IPropertySymbol property:
                                {
                                    TemplatingScope? signatureScope = null;

                                    this.CombineScope( property.Type, signatureMemberOptions, symbolsBeingProcessed, ref signatureScope, tracer );

                                    foreach ( var parameter in property.Parameters )
                                    {
                                        this.CombineScope( parameter.Type, signatureMemberOptions, symbolsBeingProcessed, ref signatureScope, tracer );
                                    }

                                    return ApplyDefault( signatureScope );
                                }

                            case IFieldSymbol field:
                                {
                                    var typeScope = this.GetTemplatingScopeCore( field.Type, signatureMemberOptions, symbolsBeingProcessed, tracer );

                                    return ApplyDefault( typeScope );
                                }

                            case IEventSymbol @event:
                                {
                                    var eventScope = this.GetTemplatingScopeCore( @event.Type, signatureMemberOptions, symbolsBeingProcessed, tracer );

                                    return ApplyDefault( eventScope );
                                }

                            default:
                                throw new AssertionFailedException( $"Not supported: '{symbol}'." );
                        }
                    }
            }
        }

        static TemplatingScope? GetScopeFromAttributes( SymbolClassifierTracer? tracer, ISymbol symbol )
        {
            // From attributes.
            var scopeFromAttributes = symbol
                .GetAttributes()
                .Select( GetTemplatingScope )
                .FirstOrDefault( s => s != null );

            if ( scopeFromAttributes != null )
            {
                tracer?.CreateChild( symbol.OriginalDefinition ).SetResult( scopeFromAttributes );
            }

            return scopeFromAttributes;
        }
    }

    private void CheckRecursion( ImmutableLinkedList<ISymbol> symbolsBeingProcessed )
    {
        if ( symbolsBeingProcessed.Count > 32 )
        {
            var symbols = string.Join( ", ", symbolsBeingProcessed.Distinct( this._symbolEqualityComparer ).Select( x => $"'{x}'" ) );

            throw new AssertionFailedException( $"Infinite recursion detected involving the following symbols: {symbols}" );
        }
    }

    private void CombineScope(
        ITypeSymbol type,
        GetTemplatingScopeOptions options,
        ImmutableLinkedList<ISymbol> symbolsBeingProcessed,
        ref TemplatingScope? combinedScope,
        SymbolClassifierTracer? tracer )
    {
        var typeScope = this.GetTemplatingScopeCore( type, options, symbolsBeingProcessed, tracer );

        if ( typeScope == null )
        {
            return;
        }
        else if ( typeScope is TemplatingScope.Dynamic or TemplatingScope.DynamicTypeConstruction )
        {
            // Dynamic members are allowed only in templates, where CombineScope is not called.
            // In other situations (i.e. in this method, always), it means the member is run-time-only.
            typeScope = TemplatingScope.RunTimeOnly;
        }

        if ( typeScope != combinedScope )
        {
            combinedScope = (typeScope, combinedScope) switch
            {
                (_, null) => typeScope,
                (TemplatingScope.Conflict, _) => TemplatingScope.Conflict,
                (_, TemplatingScope.Conflict) => TemplatingScope.Conflict,
                (TemplatingScope.CompileTimeOnlyReturningRuntimeOnly, TemplatingScope.RunTimeOnly) => TemplatingScope.RunTimeOnly,
                (TemplatingScope.CompileTimeOnlyReturningRuntimeOnly, TemplatingScope.RunTimeOrCompileTime) => TemplatingScope.RunTimeOnly,
                (_, TemplatingScope.RunTimeOrCompileTime) => typeScope,
                (TemplatingScope.RunTimeOrCompileTime, _) => combinedScope,
                (TemplatingScope.RunTimeOnly, TemplatingScope.CompileTimeOnly) => OnConflict(),
                (TemplatingScope.CompileTimeOnly, TemplatingScope.RunTimeOnly) => OnConflict(),
                _ => throw new AssertionFailedException( $"Invalid combination: ({typeScope}, {combinedScope})" )
            };
        }
    }

    private void CombineBaseTypeScope(
        ITypeSymbol baseType,
        ref TemplatingScope? combinedScope,
        ImmutableLinkedList<ISymbol> symbolsBeingProcessed,
        SymbolClassifierTracer? tracer )
    {
        var baseTypeScope = this.GetTemplatingScopeCore(
            baseType,
            GetTemplatingScopeOptions.ImplicitRuntimeOrCompileTimeAsNull,
            symbolsBeingProcessed,
            tracer );

        if ( baseTypeScope == TemplatingScope.DynamicTypeConstruction )
        {
            baseTypeScope = TemplatingScope.RunTimeOnly;
        }

        combinedScope = (baseTypeScope, combinedScope) switch
        {
            (null, _) => combinedScope,

            (_, null) => baseTypeScope,

            // Same values.
            (TemplatingScope.RunTimeOnly, TemplatingScope.RunTimeOnly) => TemplatingScope.RunTimeOnly,
            (TemplatingScope.RunTimeOrCompileTime, TemplatingScope.RunTimeOrCompileTime) => TemplatingScope.RunTimeOrCompileTime,
            (TemplatingScope.CompileTimeOnly, TemplatingScope.CompileTimeOnly) => TemplatingScope.CompileTimeOnly,

            // A RunTimeOrCompileTime type can have interfaces that are CompileTimeOnly and/or RunTimeOnly. 
            (_, TemplatingScope.RunTimeOrCompileTime) when baseType.TypeKind == TypeKind.Interface => TemplatingScope.RunTimeOrCompileTime,
            (_, TemplatingScope.RunTimeOrCompileTime) => baseTypeScope.Value,
            (TemplatingScope.RunTimeOrCompileTime, _) => combinedScope,

            // Conflicts
            (TemplatingScope.Conflict, _) => TemplatingScope.Conflict,
            (_, TemplatingScope.Conflict) => TemplatingScope.Conflict,

            (TemplatingScope.RunTimeOnly, TemplatingScope.CompileTimeOnly) => OnConflict(),
            (TemplatingScope.CompileTimeOnly, TemplatingScope.RunTimeOnly) => OnConflict(),
            _ => throw new AssertionFailedException( $"Invalid combination: ({baseTypeScope}, {combinedScope})" )
        };
    }

    private bool TryGetWellKnownScope( ISymbol symbol, GetTemplatingScopeOptions options, out TemplatingScope? scope )
    {
        scope = null;

        switch ( symbol )
        {
            case IErrorTypeSymbol:
                // Coverage: ignore
                return false;

            case INamedTypeSymbol namedType when !namedType.IsGenericType || namedType.IsGenericTypeDefinition():
                // Check well-known types and ancestors.
                for ( var t = namedType; t != null && t.SpecialType != SpecialType.System_Object; t = t.BaseType )
                {
                    if ( t.MetadataName is { } name &&
                         _wellKnownTypes.TryGetValue( name, out var config ) &&
                         config.Namespace == t.ContainingNamespace.GetFullName() )
                    {
                        scope = config.Scope;

                        return true;
                    }
                }

                // Check Roslyn types.
                if ( namedType.ContainingNamespace.GetFullName()?.StartsWith( "Microsoft.CodeAnalysis", StringComparison.Ordinal ) == true )
                {
                    scope = this._roslynIsCompileTimeOnly ? TemplatingScope.CompileTimeOnly : TemplatingScope.RunTimeOrCompileTime;

                    return true;
                }

                // Check system types.                   
                if ( this._referenceAssemblyLocator.IsSystemType( namedType ) )
                {
                    if ( (options & GetTemplatingScopeOptions.ImplicitRuntimeOrCompileTimeAsNull) != 0 )
                    {
                        // When we are inferring the scope from base types, system types cannot play a role
                        // in the inference.
                        scope = null;
                    }
                    else
                    {
                        scope = TemplatingScope.RunTimeOrCompileTime;
                    }

                    return true;
                }

                return false;

            case { ContainingType: { } namedType }:
                {
                    // Check well-known members.
                    if ( _wellKnownMembers.TryGetValue( (namedType.MetadataName, symbol.MetadataName), out var config ) &&
                         config.Namespace == namedType.ContainingNamespace.GetFullName() )
                    {
                        scope = config.Scope;

                        return true;
                    }

                    return this.TryGetWellKnownScope( namedType, options, out scope );
                }

            default:
                return false;
        }
    }

    [Flags]
    private enum GetTemplatingScopeOptions
    {
        Default,
        TypeParametersAreNeutral = 1,

        /// <summary>
        /// Determines that a null value should be used instead of <see cref="TemplatingScope.RunTimeOrCompileTime"/>
        /// except when there is an explicit use of <see cref="RunTimeOrCompileTimeAttribute"/>.
        /// </summary>
        ImplicitRuntimeOrCompileTimeAsNull = 2,
        Count = 1 + (TypeParametersAreNeutral | ImplicitRuntimeOrCompileTimeAsNull)
    }

    public bool IsTemplate( ISymbol symbol ) => !this.GetTemplateInfo( symbol ).IsNone;
}