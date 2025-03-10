// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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