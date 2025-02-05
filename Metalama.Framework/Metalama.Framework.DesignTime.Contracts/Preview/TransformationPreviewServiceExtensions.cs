// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.DesignTime.Contracts.Preview;

[PublicAPI]
public static class TransformationPreviewServiceExtensions
{
    public static async Task<IPreviewTransformationResult> PreviewTransformationAsync(
        this ITransformationPreviewService service,
        Document document,
        CancellationToken cancellationToken = default )
    {
        var result = new IPreviewTransformationResult[1];

        await service.PreviewTransformationAsync( document, result, cancellationToken );

        return result[0];
    }

    public static async Task<IPreviewTransformationResult> PreviewGeneratedFileAsync(
        this ITransformationPreviewService2 service,
        Project project,
        string filePath,
        CancellationToken cancellationToken = default )
    {
        var result = new IPreviewTransformationResult[1];

        await service.PreviewGeneratedFileAsync( project, filePath, result, cancellationToken );

        return result[0];
    }
}