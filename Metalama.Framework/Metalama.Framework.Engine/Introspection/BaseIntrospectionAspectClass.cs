// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Introspection;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Introspection;

internal abstract class BaseIntrospectionAspectClass : IIntrospectionAspectClass
{
    private readonly IAspectClass _aspectClass;

    public string FullName => this._aspectClass.FullName;

    public string ShortName => this._aspectClass.ShortName;

    public string DisplayName => this._aspectClass.DisplayName;

    public string? Description => this._aspectClass.Description;

    public bool IsAbstract => this._aspectClass.IsAbstract;

    public bool? IsInheritable => this._aspectClass.IsInheritable;

    public bool IsAttribute => this._aspectClass.IsAttribute;

    public Type Type => this._aspectClass.Type;

    public EditorExperienceOptions EditorExperienceOptions => this._aspectClass.EditorExperienceOptions;

    protected BaseIntrospectionAspectClass( IAspectClass aspectClass )
    {
        this._aspectClass = aspectClass;
    }

    public abstract ImmutableArray<IIntrospectionAspectInstance> Instances { get; }

    public override string ToString() => this.ShortName;
}