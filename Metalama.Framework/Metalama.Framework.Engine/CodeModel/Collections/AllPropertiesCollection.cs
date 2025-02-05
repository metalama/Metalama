// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Collections;

internal sealed class AllPropertiesCollection : AllMembersCollection<IProperty>, IPropertyCollection
{
    public AllPropertiesCollection( INamedTypeImpl declaringType ) : base( declaringType ) { }

    protected override IMemberCollection<IProperty> GetMembers( INamedType namedType ) => namedType.Properties;

    protected override IEqualityComparer<IProperty> Comparer => this.CompilationContext.PropertyComparer;

    public IProperty this[ string name ] => this.OfName( name ).Single();
}