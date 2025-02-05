// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Collections;

internal sealed class AllFieldsAndPropertiesCollection : AllMembersCollection<IFieldOrProperty>, IFieldOrPropertyCollection
{
    public AllFieldsAndPropertiesCollection( INamedTypeImpl declaringType ) : base( declaringType ) { }

    protected override IMemberCollection<IFieldOrProperty> GetMembers( INamedType namedType ) => namedType.FieldsAndProperties;

    protected override IEqualityComparer<IFieldOrProperty> Comparer => this.CompilationContext.FieldOrPropertyComparer;
}