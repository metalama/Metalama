// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;

namespace Metalama.Framework.DesignTime.Pipeline;

internal readonly struct DesignTimeTransformation( SerializableDeclarationId targetDeclarationId, string aspectClassFullName, string description )
{
    public SerializableDeclarationId TargetDeclarationId { get; } = targetDeclarationId;

    public string AspectClassFullName { get; } = aspectClassFullName;

    public string Description { get; } = description;
}