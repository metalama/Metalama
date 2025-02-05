// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System;
using System.Globalization;

namespace Metalama.Framework.Engine.SerializableIds;

public static partial class SerializableDeclarationIdProvider
{
    internal static ICompilationElement? ResolveToDeclaration( this SerializableDeclarationId id, CompilationModel compilation )
    {
        var indexOfAt = id.Id.IndexOfOrdinal( ';' );

        if ( indexOfAt > 0 )
        {
            // We have a parameter or a type parameter.

            var parts = id.Id.Split( _separators );

            var parentId = parts[0];
            var kind = parts[1];
            var ordinal = parts.Length == 3 ? int.Parse( parts[2], CultureInfo.InvariantCulture ) : -1;

            var parent = DocumentationIdHelper.GetFirstDeclarationForDeclarationId( parentId, compilation );

            return (parent, kind) switch
            {
                (null, _) => null,
                (IHasParameters method, "Parameter") => method.Parameters[ordinal],
                (IGeneric generic, "TypeParameter") => generic.TypeParameters[ordinal],
                (IMethod method, nameof(RefTargetKind.Return)) => method.ReturnParameter,
                (IField field, nameof(RefTargetKind.PropertyGet)) => field.GetMethod,
                (IField field, nameof(RefTargetKind.PropertySet)) => field.SetMethod,
                (IField field, nameof(RefTargetKind.PropertySetParameter)) => field.SetMethod?.Parameters[0],
                (IField field, nameof(RefTargetKind.PropertyGetReturnParameter)) => field.GetMethod?.ReturnParameter,
                (IField field, nameof(RefTargetKind.PropertySetReturnParameter)) => field.SetMethod?.ReturnParameter,
                (IEvent @event, nameof(RefTargetKind.EventRaise)) => @event.RaiseMethod,
                (IEvent @event, nameof(RefTargetKind.EventRaiseParameter)) => @event.RaiseMethod?.Parameters[0],
                (IEvent @event, nameof(RefTargetKind.EventRaiseReturnParameter)) => @event.RaiseMethod?.ReturnParameter,
                _ => null
            };
        }
        else if ( id.Id.StartsWith( _assemblyPrefix, StringComparison.OrdinalIgnoreCase ) )
        {
            if ( !AssemblyIdentity.TryParseDisplayName( id.Id.Substring( _assemblyPrefix.Length ), out var assemblyIdentity ) )
            {
                throw new AssertionFailedException( $"Cannot parse the id '{id.Id}'." );
            }

            return compilation.Factory.GetAssembly( assemblyIdentity );
        }
        else if ( id.Id.StartsWith( SerializableTypeId.Prefix, StringComparison.Ordinal ) )
        {
            if ( !compilation.CompilationContext.SerializableTypeIdResolver.TryResolveId( new SerializableTypeId( id.Id ), out var typeSymbol ) )
            {
                return null;
            }
            else
            {
                return compilation.Factory.GetIType( typeSymbol );
            }
        }
        else
        {
            return DocumentationIdHelper.GetFirstDeclarationForDeclarationId( id.ToString(), compilation );
        }
    }
}