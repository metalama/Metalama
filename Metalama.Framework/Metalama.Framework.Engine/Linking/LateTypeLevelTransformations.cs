// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Engine.Linking;

internal sealed class LateTypeLevelTransformations
{
    /// <summary>
    /// Indicates that the primary constructor should be removed.
    /// </summary>
    private volatile bool _shouldRemovePrimaryConstructor;

    public bool ShouldRemovePrimaryConstructor => this._shouldRemovePrimaryConstructor;

    public void RemovePrimaryConstructor() => this._shouldRemovePrimaryConstructor = true;
}