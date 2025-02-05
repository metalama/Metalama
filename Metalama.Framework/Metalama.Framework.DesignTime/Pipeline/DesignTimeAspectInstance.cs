// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;

namespace Metalama.Framework.DesignTime.Pipeline;

internal readonly struct DesignTimeAspectInstance(
    SerializableDeclarationId targetDeclarationId,
    SerializableDeclarationId? predecessorDeclarationId,
    string aspectClassFullName,
    bool isSkipped )
{
    public SerializableDeclarationId TargetDeclarationId { get; } = targetDeclarationId;

    public SerializableDeclarationId? PredecessorDeclarationId { get; } = predecessorDeclarationId;

    public string AspectClassFullName { get; } = aspectClassFullName;

    public bool IsSkipped { get; } = isSkipped;
}