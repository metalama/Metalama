// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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