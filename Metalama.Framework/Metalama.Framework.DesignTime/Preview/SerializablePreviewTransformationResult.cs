// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.DesignTime;
using Newtonsoft.Json;

namespace Metalama.Framework.DesignTime.Preview;

[JsonObject]
public class SerializablePreviewTransformationResult
{
    public bool IsSuccessful { get; }

    public SerializableSyntaxTree? TransformedSyntaxTree { get; }

    public string[]? ErrorMessages { get; }

    public SerializablePreviewTransformationResult( bool isSuccessful, SerializableSyntaxTree? transformedSyntaxTree, string[]? errorMessages )
    {
        this.IsSuccessful = isSuccessful;
        this.TransformedSyntaxTree = transformedSyntaxTree;
        this.ErrorMessages = errorMessages;
    }

    public static SerializablePreviewTransformationResult Failure( params string[] errorMessage ) => new( false, null, errorMessage );

    public static SerializablePreviewTransformationResult Success( SerializableSyntaxTree transformedSyntaxTree, string[]? errorMessages )
        => new( true, transformedSyntaxTree, errorMessages );
}