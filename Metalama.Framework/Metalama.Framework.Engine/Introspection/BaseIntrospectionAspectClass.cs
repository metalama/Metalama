// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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