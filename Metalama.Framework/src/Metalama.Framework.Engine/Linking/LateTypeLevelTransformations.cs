// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Linking;

internal sealed class LateTypeLevelTransformations
{
    /// <summary>
    /// Indicates that the primary constructor should be removed.
    /// </summary>
    private volatile bool _shouldRemovePrimaryConstructor;

    /// <summary>
    /// Names of parameters that were introduced onto a record primary constructor with
    /// <c>MaterializeOnRecord = false</c>. The linker's primary-constructor materialization path
    /// uses this set to filter out the corresponding positional property, Deconstruct entry, and
    /// field/property initialization statement, so the parameter lives as a constructor parameter only
    /// and is NOT part of the record's value shape.
    /// </summary>
    private readonly HashSet<string> _nonMaterializedIntroducedParameterNames = new( StringComparer.Ordinal );

    /// <summary>
    /// Indicates whether any parameter was introduced onto a record primary constructor with
    /// <c>MaterializeOnRecord = true</c>. When <c>false</c>, the materialized-primary path's extended
    /// <c>Deconstruct</c> emission (<see cref="LinkerRewritingDriver"/>) is suppressed because the
    /// compensator in <see cref="LinkerInjectionStep"/>'s rewriter already emits the pre-mutation
    /// shape, which coincides with the post-filter shape.
    /// </summary>
    private volatile bool _hasMaterializedIntroducedParameterOnPrimary;

    public bool ShouldRemovePrimaryConstructor => this._shouldRemovePrimaryConstructor;

    public IReadOnlyCollection<string> NonMaterializedIntroducedParameterNames => this._nonMaterializedIntroducedParameterNames;

    public bool HasMaterializedIntroducedParameterOnPrimary => this._hasMaterializedIntroducedParameterOnPrimary;

    public void RemovePrimaryConstructor() => this._shouldRemovePrimaryConstructor = true;

    public void AddNonMaterializedIntroducedParameter( string name )
    {
        lock ( this._nonMaterializedIntroducedParameterNames )
        {
            this._nonMaterializedIntroducedParameterNames.Add( name );
        }
    }

    public void MarkMaterializedIntroducedParameterOnPrimary() => this._hasMaterializedIntroducedParameterOnPrimary = true;
}