// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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