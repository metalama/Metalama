// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.DesignTime.AspectExplorer;

internal sealed class AspectDatabaseAspectTransformation(
    string targetDeclarationId,
    string description,
    string? transformedDeclarationId = null,
    string? filePath = null )
{
    public string TargetDeclarationId { get; } = targetDeclarationId;

    public string Description { get; } = description;

    public string? TransformedDeclarationId { get; } = transformedDeclarationId;

    public string? FilePath { get; } = filePath;
}