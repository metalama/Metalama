// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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