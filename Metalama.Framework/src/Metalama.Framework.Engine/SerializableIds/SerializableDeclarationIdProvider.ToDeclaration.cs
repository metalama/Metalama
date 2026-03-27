// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System;
using System.Globalization;
using System.Linq;

namespace Metalama.Framework.Engine.SerializableIds;

public static partial class SerializableDeclarationIdProvider
{
    internal static ICompilationElement? ResolveToDeclaration( this SerializableDeclarationId id, CompilationModel compilation )
    {
        // Handle file-local type discriminator: strip the |hash suffix before further processing.
        var (effectiveIdString, fileLocalHash) = ParseFileLocalSuffix( id.Id );

        var indexOfAt = effectiveIdString.IndexOfOrdinal( ';' );

        if ( indexOfAt > 0 )
        {
            // We have a parameter or a type parameter.

            var parts = effectiveIdString.Split( _separators );

            var parentId = parts[0];
            var kind = parts[1];
            var ordinal = parts.Length == 3 ? int.Parse( parts[2], CultureInfo.InvariantCulture ) : -1;

            var parent = ResolveDeclarationForDocId( parentId, compilation, fileLocalHash );

            return (parent, kind) switch
            {
                (null, _) => null,
                (IHasParameters method, "Parameter") => method.Parameters[ordinal],
                (IGeneric generic, "TypeParameter") => generic.TypeParameters[ordinal],
                (IMethod method, nameof(RefTargetKind.Return)) => method.ReturnParameter,
                (INamedType { TypeKind: Code.TypeKind.Delegate } delegateType, nameof(RefTargetKind.Return))
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
        else if ( effectiveIdString.StartsWith( _assemblyPrefix, StringComparison.OrdinalIgnoreCase ) )
        {
            if ( !AssemblyIdentity.TryParseDisplayName( effectiveIdString.Substring( _assemblyPrefix.Length ), out var assemblyIdentity ) )
            {
                throw new AssertionFailedException( $"Cannot parse the id '{id.Id}'." );
            }

            return compilation.Factory.GetAssembly( assemblyIdentity );
        }
        else if ( effectiveIdString.StartsWith( SerializableTypeId.Prefix, StringComparison.Ordinal ) )
        {
            if ( !compilation.CompilationContext.SerializableTypeIdResolver.TryResolveId( new SerializableTypeId( effectiveIdString ), out var typeSymbol ) )
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
            return ResolveDeclarationForDocId( effectiveIdString, compilation, fileLocalHash );
        }
    }

    /// <summary>
    /// Resolves a documentation comment ID to a declaration, optionally filtering by file-local hash for file-local types.
    /// </summary>
    private static IDeclaration? ResolveDeclarationForDocId( string docId, CompilationModel compilation, string? fileLocalHash )
    {
        if ( fileLocalHash == null )
        {
            return DocumentationIdHelper.GetFirstDeclarationForDeclarationId( docId, compilation );
        }

        // For file-local types, we may get multiple declarations with the same documentation comment ID.
        // We need to find the one with the matching hash.
        var allDeclarations = DocumentationIdHelper.GetDeclarationsForDeclarationId( docId, compilation );

        foreach ( var candidate in allDeclarations )
        {
            if ( GetFileLocalHash( candidate ) == fileLocalHash )
            {
                return candidate;
            }
        }

        // Fallback: return the first match if no file-local match found.
        return allDeclarations.Count == 0 ? null : allDeclarations[0];
    }
}