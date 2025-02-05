// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Collections;

internal sealed class AllFieldsCollection : AllMembersCollection<IField>, IFieldCollection
{
    public AllFieldsCollection( INamedTypeImpl declaringType ) : base( declaringType ) { }

    protected override IMemberCollection<IField> GetMembers( INamedType namedType ) => namedType.Fields;

    protected override IEqualityComparer<IField> Comparer => this.CompilationContext.FieldComparer;

    public IField this[ string name ] => this.OfName( name ).Single();
}