// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.UserCode;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.SerializableIds;

public static partial class SerializableDeclarationIdProvider
{
    /// <summary>
    /// Resolves a <see cref="SerializableDeclarationId"/> to a declaration of <paramref name="compilation"/>, or returns
    /// <c>null</c> when the identifier does not match any declaration.
    /// </summary>
    /// <remarks>
    /// The lookup walks the merged global namespace, so it reaches referenced assemblies and not only the current
    /// project. Enumerating namespaces is normally rejected in the <c>BuildAspect</c> context at design time because
    /// design-time cache invalidation cannot track such a query, but this method is framework machinery and not a user
    /// code model query: the identifier names a single declaration, and the dependency on the project that declares it
    /// is already tracked through the project version. Dependency collection is therefore suppressed for the duration
    /// of the lookup. Without this suppression, resolving a reference that reached the current project through a
    /// deserialized transitive manifest throws inside <c>BuildAspect</c>, and the transitive aspect silently does not
    /// apply (issue #1752).
    /// </remarks>
    internal static ICompilationElement? ResolveToDeclaration( this SerializableDeclarationId id, CompilationModel compilation )
    {
        // The nullable annotation is carried by the identifier but is not part of the documentation identifier that
        // names the declaration, so it is removed before the lookup and applied to its result.
        var idWithoutNullability = id.StripNullability( out var isNullable );

        using ( UserCodeExecutionContext.CurrentOrNull?.WithoutDependencyCollection() ?? default )
        {
            return ApplyNullability( ResolveToDeclarationCore( idWithoutNullability, compilation ), isNullable );
        }
    }

    private static ICompilationElement? ResolveToDeclarationCore( SerializableDeclarationId id, CompilationModel compilation )
    {
        var idString = id.Id;

        var indexOfAt = idString.IndexOfOrdinal( ';' );

        if ( indexOfAt > 0 )
        {
            // We have a parameter or a type parameter.

            var parts = idString.Split( _separators );

            var parentId = parts[0];
            var kind = parts[1];
            var ordinal = parts.Length == 3 ? int.Parse( parts[2], CultureInfo.InvariantCulture ) : -1;

            // The identifier of a constructor is written from its source signature and therefore matches both the
            // source constructor and the constructor that an aspect extended with pulled parameters. At design time
            // both are present, because the generated overload is added beside the original one rather than replacing
            // it, so the first candidate is not necessarily the one that declares the requested ordinal.
            //
            // The candidates are considered from the fewest parameters to the most, and the first one that declares the
            // requested ordinal is kept. Two candidates that match one identifier cannot have the same number of
            // parameters, because their signatures would then be identical, so the order is total and the selection
            // does not depend on the order in which DocumentationIdHelper enumerated the members, which is undefined.
            // Preferring the fewest parameters selects the declaration whose signature the identifier describes, which
            // is the pre-transformation one, as documented on AspectGeneratedAttribute.
            var candidates = DocumentationIdHelper.GetDeclarationsForDeclarationId( parentId, compilation );

            // Almost every identifier matches a single declaration, which is not worth ordering.
            var orderedCandidates = candidates.Count <= 1
                ? (IEnumerable<IDeclaration>) candidates
                : candidates.OrderBy( GetParameterCount );

            foreach ( var candidate in orderedCandidates )
            {
                if ( ResolveChild( candidate ) is { } child )
                {
                    return child;
                }
            }

            return null;

            static int GetParameterCount( IDeclaration declaration )
                => declaration.DeclarationKind is DeclarationKind.Method or DeclarationKind.Constructor or DeclarationKind.Indexer
                   && declaration is IHasParameters hasParameters
                    ? hasParameters.Parameters.Count
                    : 0;

            ICompilationElement? ResolveChild( IDeclaration parent )
                => (parent, kind) switch
                {
                    (IHasParameters method, "Parameter") => GetAtOrNull( method.Parameters, ordinal ),
                    (IGeneric generic, "TypeParameter") => GetAtOrNull( generic.TypeParameters, ordinal ),
                    (IMethod method, nameof(RefTargetKind.Return)) => method.ReturnParameter,
                    (INamedType { TypeKind: TypeKind.Delegate } delegateType, nameof(RefTargetKind.Return))
                        => delegateType.Methods.OfName( "Invoke" ).SingleOrDefault()?.ReturnParameter,
                    (IField field, nameof(RefTargetKind.PropertyGet)) => field.GetMethod,
                    (IField field, nameof(RefTargetKind.PropertySet)) => field.SetMethod,
                    (IField field, nameof(RefTargetKind.PropertySetParameter)) => field.SetMethod?.Parameters[0],
                    (IField field, nameof(RefTargetKind.PropertyGetReturnParameter)) => field.GetMethod?.ReturnParameter,
                    (IField field, nameof(RefTargetKind.PropertySetReturnParameter)) => field.SetMethod?.ReturnParameter,
                    (IEvent @event, nameof(RefTargetKind.EventRaise)) => @event.RaiseMethod,
                    (IEvent @event, nameof(RefTargetKind.EventRaiseParameter)) => @event.RaiseMethod?.Parameters[0],
                    (IEvent @event, nameof(RefTargetKind.EventRaiseReturnParameter)) => @event.RaiseMethod?.ReturnParameter,
                    (INamedType type, nameof(RefTargetKind.PrimaryConstructor)) => type.PrimaryConstructor,
                    _ => null
                };
        }
        else if ( idString.StartsWith( _assemblyPrefix, StringComparison.OrdinalIgnoreCase ) )
        {
            if ( !AssemblyIdentity.TryParseDisplayName( idString.Substring( _assemblyPrefix.Length ), out var assemblyIdentity ) )
            {
                throw new AssertionFailedException( $"Cannot parse the id '{id.Id}'." );
            }

            return compilation.Factory.GetAssembly( assemblyIdentity );
        }
        else if ( idString.StartsWith( SerializableTypeId.Prefix, StringComparison.Ordinal ) )
        {
            if ( !compilation.CompilationContext.SerializableTypeIdResolver.TryResolveId( new SerializableTypeId( idString ), out var typeSymbol ) )
            {
                return null;
            }
            else
            {
                return compilation.Factory.GetIType( typeSymbol, defaultNullability: null );
            }
        }
        else
        {
            return DocumentationIdHelper.GetFirstDeclarationForDeclarationId( idString, compilation );
        }
    }

    /// <summary>
    /// Returns the item of <paramref name="list"/> at <paramref name="index"/>, or <c>null</c> if the index is out of
    /// range.
    /// </summary>
    /// <remarks>
    /// An identifier that names a parameter or a type parameter of a declaration that has fewer of them in the current
    /// compilation is unresolvable, which is reported by returning <c>null</c> as for any other identifier that does
    /// not match, rather than by throwing.
    /// </remarks>
    private static T? GetAtOrNull<T>( IReadOnlyList<T> list, int index )
        where T : class
        => index >= 0 && index < list.Count ? list[index] : null;
}
