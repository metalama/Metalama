// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Options;

namespace Metalama.Patterns.Caching.Aspects.Configuration;

internal sealed class ParameterFilterRegistration : IIncrementalKeyedCollectionItem<string>
{
    private readonly string _name;

    public object ApplyChanges( object changes, in ApplyChangesContext context ) => changes;

    public ICacheParameterClassifier Classifier { get; }

    public ParameterFilterRegistration( string name, ICacheParameterClassifier classifier )
    {
        this._name = name;
        this.Classifier = classifier;
    }

    string IIncrementalKeyedCollectionItem<string>.Key => this._name;
}