// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.Preview;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.DesignTime.Preview;

/// <summary>
/// Result of the <see cref="ITransformationPreviewService.PreviewTransformationAsync"/> method.
/// </summary>
internal sealed class PreviewTransformationResult : IPreviewTransformationResult
{
    public bool IsSuccessful { get; set; }

    public SyntaxTree? TransformedSyntaxTree { get; set; }

    public string[]? ErrorMessages { get; set; }

    private PreviewTransformationResult( bool isSuccessful, SyntaxTree? transformedSyntaxTree, string[]? errorMessages )
    {
        this.IsSuccessful = isSuccessful;

        if ( isSuccessful )
        {
            this.TransformedSyntaxTree = transformedSyntaxTree ?? throw new ArgumentNullException( nameof(transformedSyntaxTree) );
            this.ErrorMessages = errorMessages;
        }
        else
        {
            this.ErrorMessages = errorMessages ?? throw new ArgumentNullException( nameof(errorMessages) );
        }
    }

    internal static PreviewTransformationResult Failure( params string[] errorMessage ) => new( false, null, errorMessage );

    internal static PreviewTransformationResult Success( SyntaxTree transformedSyntaxTree, string[]? errorMessages )
        => new( true, transformedSyntaxTree, errorMessages );
}