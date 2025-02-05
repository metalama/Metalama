// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.DesignTime.AspectExplorer;

internal sealed class AspectDatabaseAspectInstance(
    string targetDeclarationId,
    IEnumerable<AspectDatabaseAspectTransformation> transformations )
{
    public string TargetDeclarationId { get; } = targetDeclarationId;

    public IEnumerable<AspectDatabaseAspectTransformation> Transformations { get; } = transformations;
}