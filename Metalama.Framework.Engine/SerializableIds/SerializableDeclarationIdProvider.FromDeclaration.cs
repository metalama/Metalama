// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.References;
using System;

namespace Metalama.Framework.Engine.SerializableIds;

public static partial class SerializableDeclarationIdProvider
{
    internal static SerializableDeclarationId GetSerializableId( this IDeclaration declaration, RefTargetKind targetKind = RefTargetKind.Default )
    {
        if ( !TryGetSerializableId( declaration, targetKind, out var id ) )
        {
            throw new ArgumentException( $"Cannot create a SerializableDeclarationId for '{declaration}'.", nameof(declaration) );
        }

        return id;
    }

    public static bool TryGetSerializableId( this IDeclaration? declaration, out SerializableDeclarationId id )
        => TryGetSerializableId( declaration, RefTargetKind.Default, out id );

    private static bool TryGetSerializableId( this IDeclaration? declaration, RefTargetKind targetKind, out SerializableDeclarationId id )
    {
        switch ( declaration )
        {
            case null:

                id = default;

                return false;

            case IParameter { IsReturnParameter: true } parameter:
                return TryGetSerializableId( parameter.DeclaringMember, RefTargetKind.Return, out id );

            case IParameter { ContainingDeclaration.ContainingDeclaration: IField } parameter:
                return TryGetSerializableId( parameter.ContainingDeclaration, RefTargetKind.Parameter, out id );

            case IParameter parameter:
                {
                    var parentId = DocumentationIdHelper.CreateDeclarationId( parameter.DeclaringMember ).AssertNotNull();

                    id = new SerializableDeclarationId( $"{parentId};Parameter={parameter.Index}" );

                    return true;
                }

            case ITypeParameter typeParameter:
                {
                    var parentId = DocumentationIdHelper.CreateDeclarationId( typeParameter.ContainingDeclaration! ).AssertNotNull();

                    id = new SerializableDeclarationId( $"{parentId};TypeParameter={typeParameter.Index}" );

                    return true;
                }

            case IAssembly assembly:
                {
                    id = new SerializableDeclarationId( $"{_assemblyPrefix}{assembly.Identity}" );

                    return true;
                }

            case IMethod { ContainingDeclaration: IField } fieldPseudoAccessor:
                return TryGetSerializableId(
                    fieldPseudoAccessor.DeclaringMember,
                    fieldPseudoAccessor.MethodKind.ToDeclarationRefTargetKind( targetKind ),
                    out id );

            case IMethod { ContainingDeclaration: IEvent, MethodKind: MethodKind.EventRaise } eventRaisePseudoAccessor:
                return TryGetSerializableId( eventRaisePseudoAccessor.DeclaringMember, RefTargetKind.EventRaise, out id );

            default:
                string documentationId;

                try
                {
                    documentationId = DocumentationIdHelper.CreateDeclarationId( declaration );
                }
                catch ( InvalidOperationException exception )
                {
                    throw new InvalidOperationException(
                        $"Cannot get a DeclarationDocumentationCommentId for '{declaration}' ({declaration.DeclarationKind}).",
                        exception );
                }

                id = new SerializableDeclarationId( targetKind == RefTargetKind.Default ? documentationId : $"{documentationId};{targetKind}" );

                return true;
        }
    }

    /// <summary>Gets the <see cref="SerializableDeclarationId"/> for the declaration as it appears in the unmodified source code.</summary>
    /// <remarks>This is relevant in the case of constructor parameter introduction, which alter the serializable ID of the constructor.</remarks>
    [PublicAPI]
    public static SerializableDeclarationId GetSourceSerializableId( this IDeclaration declaration )
        => declaration.GetSymbol()?.GetSerializableId() ?? declaration.ToSerializableId();
}