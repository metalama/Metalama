// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
        switch ( declaration?.DeclarationKind )
        {
            case null:

                id = default;

                return false;

            case DeclarationKind.Parameter when declaration is IParameter { IsReturnParameter: true } parameter:
                return TryGetSerializableId( parameter.DeclaringMember, RefTargetKind.Return, out id );

            case DeclarationKind.Parameter when declaration is IParameter { ContainingDeclaration.ContainingDeclaration: IField } parameter:
                return TryGetSerializableId( parameter.ContainingDeclaration, RefTargetKind.Parameter, out id );

            case DeclarationKind.Parameter when declaration is IParameter parameter:
                {
                    var parentId = DocumentationIdHelper.CreateDeclarationId( parameter.ContainingDeclaration.AssertNotNull() ).AssertNotNull();

                    id = new SerializableDeclarationId( $"{parentId};Parameter={parameter.Index}" );

                    return true;
                }

            case DeclarationKind.TypeParameter when declaration is ITypeParameter typeParameter:
                {
                    var parentId = DocumentationIdHelper.CreateDeclarationId( typeParameter.ContainingDeclaration! ).AssertNotNull();

                    id = new SerializableDeclarationId( $"{parentId};TypeParameter={typeParameter.Index}" );

                    return true;
                }

            case DeclarationKind.Compilation or DeclarationKind.AssemblyReference when declaration is IAssembly assembly:
                {
                    id = new SerializableDeclarationId( $"{_assemblyPrefix}{assembly.Identity}" );

                    return true;
                }

            case DeclarationKind.Method when declaration is IMethod { ContainingDeclaration: IField } fieldPseudoAccessor:
                return TryGetSerializableId(
                    fieldPseudoAccessor.DeclaringMember,
                    fieldPseudoAccessor.MethodKind.ToDeclarationRefTargetKind( targetKind ),
                    out id );

            case DeclarationKind.Method when declaration is IMethod
            {
                ContainingDeclaration: IEvent, MethodKind: MethodKind.EventRaise
            } eventRaisePseudoAccessor:
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

                // For file-local types (or members of file-local types), append the source file path
                // to disambiguate types with the same name in different files.
                var fileLocalPath = GetFileLocalFilePath( declaration );
                var idString = AppendFileLocalSuffix( documentationId, fileLocalPath );

                id = new SerializableDeclarationId( targetKind == RefTargetKind.Default ? idString : $"{idString};{targetKind}" );

                return true;
        }
    }

    /// <summary>Gets the <see cref="SerializableDeclarationId"/> for the declaration as it appears in the unmodified source code.</summary>
    /// <remarks>This is relevant in the case of constructor parameter introduction, which alter the serializable ID of the constructor.</remarks>
    [PublicAPI]
    public static SerializableDeclarationId GetSourceSerializableId( this IDeclaration declaration )
        => declaration.GetSymbol()?.GetSerializableId() ?? declaration.ToSerializableId();
}