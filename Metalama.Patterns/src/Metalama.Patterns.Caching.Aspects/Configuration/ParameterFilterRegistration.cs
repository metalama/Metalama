// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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