// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.Invokers;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Collections;

internal sealed class ParameterBuilderList : List<BaseParameterBuilder>, IParameterList, IParameterBuilderList
{
    public ParameterBuilderList() { }

    public ParameterBuilderList( IEnumerable<BaseParameterBuilder> parameterBuilders ) : base( parameterBuilders ) { }

    IEnumerator<IParameterBuilder> IEnumerable<IParameterBuilder>.GetEnumerator() => this.GetEnumerator();

    IEnumerator<IParameter> IEnumerable<IParameter>.GetEnumerator() => this.GetEnumerator();

    IParameterBuilder IReadOnlyList<IParameterBuilder>.this[ int index ] => this[index];

    public IParameterBuilder this[ string name ] => this.Single<IParameterBuilder>( p => p.Name == name );

    public object ToValueArray() => new ValueArrayExpression( this );

    int IReadOnlyCollection<IParameter>.Count => this.Count;

    IParameter IReadOnlyList<IParameter>.this[ int index ] => this[index];

    IParameter IParameterList.this[ string name ] => this[name];
}