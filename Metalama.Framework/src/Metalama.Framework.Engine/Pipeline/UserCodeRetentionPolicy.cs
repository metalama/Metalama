// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.ObjectGraph;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Metalama.Framework.Engine.Pipeline;

/// <summary>
/// Decides, for <see cref="UserCodeRetentionAnalyzer"/>, which objects pin a compilation, which objects the walk must not
/// descend into, and which findings belong to code the user wrote.
/// </summary>
/// <remarks>
/// These three decisions are what make the difference between a report the user can act upon and a list of every object
/// that Metalama happens to hold, so they are separated from the mechanics of the walk and tested on their own.
/// </remarks>
internal sealed class UserCodeRetentionPolicy
{
    private readonly ImmutableHashSet<string> _compileTimeAssemblyNames;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserCodeRetentionPolicy"/> class.
    /// </summary>
    /// <param name="compileTimeAssemblyNames">
    /// The names of the assemblies that contain compile-time code written by the user. A type declared in one of them,
    /// including a compiler-generated closure type, identifies a finding as one the user can fix.
    /// </param>
    public UserCodeRetentionPolicy( ImmutableHashSet<string> compileTimeAssemblyNames )
    {
        this._compileTimeAssemblyNames = compileTimeAssemblyNames;
    }

    /// <summary>
    /// Creates the policy of a project.
    /// </summary>
    /// <remarks>
    /// The closure of a compile-time project includes a project for <c>Metalama.Framework</c> itself, which is excluded,
    /// otherwise every type of the code model would count as user code and every finding would be misattributed.
    /// </remarks>
    public static UserCodeRetentionPolicy Create( CompileTimeProject compileTimeProject )
        => new(
            compileTimeProject.ClosureProjects
                .Where( p => !p.IsFramework )
                .Select( p => p.CompileTimeIdentity.Name )
                .ToImmutableHashSet( StringComparer.OrdinalIgnoreCase ) );

    /// <summary>
    /// Determines whether an object pins a compilation, and must therefore be reported.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only objects that pin a compilation by their very nature are listed here. A <see cref="Diagnostic"/> and a
    /// <see cref="Location"/> are deliberately absent, although both are common causes of a retention: whether they
    /// pin anything depends on what they hold. A diagnostic reported without a location and with no compilation-bound
    /// message argument reaches nothing, whereas one reported on a declaration reaches the syntax tree through its
    /// location and possibly the declaration itself through its arguments. Classifying the container would therefore be
    /// wrong in the first case, and in the second it would hide which of its parts is responsible.
    /// </para>
    /// <para>
    /// The walk descends into such objects instead, and reports the <see cref="SyntaxTree"/>, <see cref="ISymbol"/> or
    /// <see cref="IDeclaration"/> that it actually finds inside, with the chain of fields naming the route. A container
    /// that holds none of those produces no finding, which is the correct answer.
    /// </para>
    /// </remarks>
    public static bool IsPinning( object obj )
        => obj switch
        {
            Compilation or SyntaxTree or SemanticModel or SyntaxNode => true,

            // A symbol pins a compilation only when it belongs to the source of one. The symbols of a referenced
            // assembly belong to its metadata, which the reference manager shares between compilations, and 'dynamic'
            // belongs to nothing.
            ISymbol symbol => IsFromSource( symbol ),
            CompilationModel or PartialCompilation or CompilationContext => true,
            IDeclaration or IType => true,

            // A durable reference is backed by a serializable identifier and reaches nothing. Any other reference holds
            // the symbol and the RefFactory, which reaches the compilation. The equivalent in the public API is
            // SerializableDeclarationId, which is a string and is therefore never reported.
            IRef => obj is not IDurableRef,
            _ => false
        };

    /// <summary>
    /// Determines whether a symbol belongs to the source of a compilation, and therefore reaches it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This distinction decides most of the report. The template members of the aspect classes hold the parameter types
    /// of their templates, and for an aspect that comes from a referenced package those are metadata symbols: they hang
    /// off a <c>PEAssemblySymbol</c> owned by a reference manager that Roslyn shares between compilations, they have no
    /// declaring compilation, and they keep nothing alive. Reporting every symbol would fill the report with those and
    /// bury the few that matter.
    /// </para>
    /// <para>
    /// A constructed type such as <c>List&lt;MyClass&gt;</c> is declared in metadata but reaches a source symbol through
    /// its type arguments, so the components of a type are examined as well as the type itself.
    /// </para>
    /// </remarks>
    private static bool IsFromSource( ISymbol symbol )
    {
        switch ( symbol.Kind )
        {
            case SymbolKind.ArrayType:
                return IsFromSource( ((IArrayTypeSymbol) symbol).ElementType );

            case SymbolKind.PointerType:
                return IsFromSource( ((IPointerTypeSymbol) symbol).PointedAtType );

            case SymbolKind.NamedType:
                var namedType = (INamedTypeSymbol) symbol;

                if ( namedType.IsGenericType && !namedType.TypeArguments.IsDefaultOrEmpty && namedType.TypeArguments.Any( IsFromSource ) )
                {
                    return true;
                }

                break;
        }

        if ( symbol.Locations.Any( l => l.IsInSource ) )
        {
            return true;
        }

        // A symbol with no location of its own, such as an implicitly declared member, is attributed to its assembly.
        // A source assembly is the assembly of a compilation; a metadata one is not.
        return symbol.ContainingAssembly is { } assembly && assembly.GetMetadata() == null;
    }

    /// <summary>
    /// Determines whether the walk must stop at an object without reporting it.
    /// </summary>
    /// <remarks>
    /// A service provider and a compile-time project are project-scoped objects shared by every component, so a chain
    /// that passes through one of them explains nothing about the fabric and would make the walk explore the whole
    /// engine. The reflection and threading types are excluded because descending into them costs a great deal and
    /// produces no chain that this codebase could act upon.
    /// </remarks>
    public static bool IsBoundary( object obj )
    {
        switch ( obj )
        {
            // The graph of a Roslyn symbol or syntax object is enormous and belongs to the compiler. It is stopped at
            // whether or not it was reported: a metadata symbol pins nothing, but descending into one reaches the
            // module, its references and the symbols of every other assembly, and turns a report into nonsense.
            case ISymbol:
            case Compilation:
            case SyntaxTree:
            case SemanticModel:
            case SyntaxNode:
            case ServiceProvider:
            case CompileTimeProject:
            case CompileTimeDomain:

            // A cacheable template reflection context owns the compilation against which the templates of a referenced
            // assembly are reflected. That compilation has no syntax tree and only portable executable references, so
            // the engine keeps it deliberately, for the whole session. A fabric reaches it through the template class of
            // its own aspect, therefore descending into it would report a compilation that the user did not create and
            // cannot release, and would name the fabric as the cause. The compilation context of the source compilation
            // also implements this interface, but it is classified as pinning and is reported before this method is
            // consulted.
            case ITemplateReflectionContext:

            // A logger is process-wide infrastructure. Nothing reached through one is a retention that the owner of the
            // chain could act upon, and the graph behind it is unbounded: in a test host it reaches the runner, and
            // through the runner every other test in the process.
            case ILogger:
            case ILoggerFactory:
            case string:
            case Type:
            case Assembly:
            case Module:
            case MemberInfo:
            case ParameterInfo:
            case Thread:
            case AppDomain:
                return true;

            default:
                return IsRuntimeInternal( obj.GetType() );
        }
    }

    /// <summary>
    /// Determines whether a type belongs to the internals of the runtime, through which no chain says anything about
    /// the code that this analysis is about.
    /// </summary>
    /// <remarks>
    /// The case that matters is <c>LoaderAllocator</c>, which the runtime stores in the <c>_methodBase</c> field of a
    /// delegate to a dynamic method, and which references everything allocated in its load context. Following it turns
    /// any delegate into a route to arbitrary unrelated objects, and produces a chain that names a dozen types the user
    /// has never heard of and no field anybody could change. The type is internal, so it is matched by name.
    /// </remarks>
    private static bool IsRuntimeInternal( Type type )
        => type.FullName is "System.Reflection.LoaderAllocator" or "System.Reflection.Emit.DynamicResolver"
            or "System.RuntimeType+RuntimeTypeCache";

    /// <summary>
    /// Returns the name to display for a type when it belongs to compile-time user code, and <c>null</c> otherwise.
    /// </summary>
    /// <remarks>
    /// A closure type is nested in the type that declares the lambda and has a compiler-generated name, therefore the
    /// outermost declaring type is what identifies the code the user wrote.
    /// </remarks>
    public string? GetUserCodeTypeName( Type type )
    {
        var assemblyName = type.Assembly.GetName().Name;

        if ( assemblyName == null || !this._compileTimeAssemblyNames.Contains( assemblyName ) )
        {
            return null;
        }

        var outermost = type;

        while ( outermost.DeclaringType != null && outermost.Name.IndexOfOrdinal( '<' ) >= 0 )
        {
            outermost = outermost.DeclaringType;
        }

        return outermost.FullName ?? outermost.Name;
    }

    /// <summary>
    /// Returns the type of compile-time user code that is closest to the pinning object on its chain of references, or
    /// <c>null</c> when the whole chain belongs to Metalama.
    /// </summary>
    /// <remarks>
    /// The chain is searched backwards, from the pinning object towards the root, so that the type reported is the one
    /// nearest to the reference rather than the first that happens to appear.
    /// </remarks>
    public string? FindUserCodeTypeName( IReadOnlyList<ObjectGraphNode> path )
    {
        for ( var i = path.Count - 1; i >= 0; i-- )
        {
            var candidate = this.GetUserCodeTypeName( path[i].Object.GetType() );

            if ( candidate != null )
            {
                return candidate;
            }
        }

        return null;
    }
}
