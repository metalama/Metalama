// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Metalama.Framework.Engine.CodeModel.Helpers;

/// <summary>
/// Provides the metadata name of an indexer. The accessors of an indexer are named after that name, so the getter of
/// an indexer whose metadata name is <c>Item</c> is named <c>get_Item</c>.
/// </summary>
internal static class IndexerHelper
{
    /// <summary>
    /// The metadata name of an indexer that does not carry <see cref="IndexerNameAttribute"/>.
    /// </summary>
    public const string DefaultMetadataName = "Item";

    /// <summary>
    /// Gets the metadata name of an indexer of the code model.
    /// </summary>
    public static string GetMetadataName( IIndexer indexer )
    {
        foreach ( var attribute in indexer.Attributes )
        {
            if ( !IsIndexerNameAttribute( attribute.Type ) || attribute.ConstructorArguments.Length != 1 )
            {
                continue;
            }

            if ( attribute.ConstructorArguments[0].Value is string indexerName )
            {
                return indexerName;
            }
        }

        return DefaultMetadataName;
    }

    /// <summary>
    /// Gets the metadata name of an indexer that is still represented by the attributes of its builder, i.e. before
    /// the indexer itself is a part of the code model.
    /// </summary>
    public static string GetMetadataName( ImmutableArray<AttributeBuilderData> indexerAttributes, CompilationModel compilation )
    {
        foreach ( var attribute in indexerAttributes )
        {
            if ( !IsIndexerNameAttribute( attribute.Type.GetTarget( compilation ) ) || attribute.ConstructorArguments.Length != 1 )
            {
                continue;
            }

            if ( attribute.ConstructorArguments[0].ToTypedConstant( compilation ).Value is string indexerName )
            {
                return indexerName;
            }
        }

        return DefaultMetadataName;
    }

    private static bool IsIndexerNameAttribute( INamedType attributeType ) => attributeType.FullName == typeof(IndexerNameAttribute).FullName;
}
