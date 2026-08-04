// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Services;
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
            Compilation or SyntaxTree or SemanticModel or ISymbol or SyntaxNode => true,
            CompilationModel or PartialCompilation or CompilationContext => true,
            IDeclaration or IType => true,

            // A durable reference is backed by a serializable identifier and reaches nothing. Any other reference holds
            // the symbol and the RefFactory, which reaches the compilation. The equivalent in the public API is
            // SerializableDeclarationId, which is a string and is therefore never reported.
            IRef => obj is not IDurableRef,
            _ => false
        };

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
            case ServiceProvider:
            case CompileTimeProject:
            case CompileTimeDomain:
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
                return false;
        }
    }

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

        while ( outermost.DeclaringType != null && outermost.Name.IndexOf( '<' ) >= 0 )
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
