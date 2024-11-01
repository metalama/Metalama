// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Runtime.CompilerServices;

namespace Metalama.Framework.Validation;

/// <summary>
/// Represents an end of a code reference. Code references have two ends: a referenced one (the <see cref="ReferenceEndRole.Origin"/> end)
/// and a referencing one (the <see cref="ReferenceEndRole.Destination"/> end).
/// </summary>
[CompileTime]
[PublicAPI]
public sealed class ReferenceEnd
{
    private IDeclaration? _parameterOrAttribute;
    private INamedType? _type;
    private INamedType? _topLevelType;
    private INamespace? _namespace;
    private IMember? _member;

    /// <summary>
    /// Gets the declaration that was analyzed, whose kind corresponds to the <see cref="Granularity"/> of the analysis
    /// for this end of the reference.
    /// </summary>
    public IDeclaration Declaration { get; }

    /// <summary>
    /// Gets the granularity at which the analysis was performed for this end of the reference.
    /// </summary>
    public ReferenceGranularity Granularity { get; }

    /// <summary>
    /// Gets the <see cref="IParameter"/>, <see cref="ITypeParameter"/> or <see cref="IAttribute"/> for which the analysis was performed,
    /// or throw an exception if the analysis <see cref="Granularity"/> was coarser than <see cref="ReferenceGranularity.ParameterOrAttribute"/>.
    /// </summary>
    public IDeclaration ParameterOrAttribute => this._parameterOrAttribute ??= this.GetDeclarationOfGranularity( ReferenceGranularity.ParameterOrAttribute );

    /// <summary>
    /// Gets the closest <see cref="INamedType"/> of the declaration for the analysis was performed, 
    /// or throw an exception if the analysis <see cref="Granularity"/> was coarser than <see cref="ReferenceGranularity.Type"/>.
    /// </summary>
    public INamedType Type => this._type ??= (INamedType) this.GetDeclarationOfGranularity( ReferenceGranularity.Type );

    /// <summary>
    /// Gets the top-level <see cref="INamedType"/> of the declaration for the analysis was performed, 
    /// or throw an exception if the analysis <see cref="Granularity"/> was coarser than <see cref="ReferenceGranularity.TopLevelType"/>.
    /// </summary>
    public INamedType TopLevelType => this._topLevelType ??= (INamedType) this.GetDeclarationOfGranularity( ReferenceGranularity.TopLevelType );

    /// <summary>
    /// Gets the <see cref="INamespace"/> of the declaration for the analysis was performed, 
    /// or throw an exception if the analysis <see cref="Granularity"/> was coarser than <see cref="ReferenceGranularity.Namespace"/>.
    /// </summary>
    public INamespace Namespace => this._namespace ??= (INamespace) this.GetDeclarationOfGranularity( ReferenceGranularity.Namespace );

    /// <summary>
    /// Gets the <see cref="IMember"/> of the declaration for the analysis was performed, 
    /// or throw an exception if the analysis <see cref="Granularity"/> was coarser than <see cref="ReferenceGranularity.Member"/>.
    /// </summary>
    public IMember Member => this._member ??= (IMember) this.GetDeclarationOfGranularity( ReferenceGranularity.Member );

    /// <summary>
    /// Gets the <see cref="IAssembly"/> (or <see cref="ICompilation"/>) for which the analysis was performed.
    /// </summary>
    public IAssembly? Assembly => this.Declaration.DeclaringAssembly;

    internal ReferenceEnd( IDeclaration declaration, ReferenceGranularity granularity )
    {
        this.Declaration = declaration;
        this.Granularity = granularity;
    }

    private IDeclaration GetDeclarationOfGranularity(
        ReferenceGranularity requestedGranularity,
        [CallerMemberName] string? callingProperty = null )
    {
        if ( requestedGranularity > this.Granularity )
        {
            throw new InvalidOperationException(
                $"Cannot get the {callingProperty} because the granularity of outbound references for this validator is set to {this.Granularity}" );
        }

        return requestedGranularity switch
        {
            ReferenceGranularity.Namespace => this.Declaration as INamespace
                                              ?? this.Declaration.GetClosestNamedType()?.ContainingNamespace
                                              ?? throw new InvalidOperationException(
                                                  $"Cannot get the namespace of '{this.Declaration}' because it is a {this.Declaration.DeclarationKind}." ),
            ReferenceGranularity.Type => this.Declaration.GetClosestNamedType()
                                         ?? throw new InvalidOperationException(
                                             $"Cannot get the declaring type of '{this.Declaration}' because it is a {this.Declaration.DeclarationKind}." ),
            ReferenceGranularity.TopLevelType => this.Declaration.GetClosestNamedType()?.GetTopmostNamedType()
                                                 ?? throw new InvalidOperationException(
                                                     $"Cannot get the declaring type of '{this.Declaration}' because it is a {this.Declaration.DeclarationKind}." ),
            ReferenceGranularity.Member => this.Declaration.GetClosestMemberOrNamedType() as IMember
                                           ?? throw new InvalidOperationException(
                                               $"Cannot get the member of '{this.Declaration}' because it is a {this.Declaration.DeclarationKind}." ),
            ReferenceGranularity.ParameterOrAttribute => this.Declaration,
            _ => throw new ArgumentOutOfRangeException( nameof(requestedGranularity) )
        };
    }

    public override string ToString() => this.Declaration.ToString() ?? "null";
}