// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Collections;

internal sealed class AllMethodsCollection : AllMembersCollection<IMethod>, IMethodCollection
{
    public AllMethodsCollection( INamedTypeImpl declaringType ) : base( declaringType ) { }

    protected override IMemberCollection<IMethod> GetMembers( INamedType namedType ) => namedType.Methods;

    protected override IEqualityComparer<IMethod> Comparer => this.CompilationContext.MethodComparer;

    public IEnumerable<IMethod> OfKind( MethodKind kind ) => this.Where( m => m.MethodKind == kind );

    public IEnumerable<IMethod> OfKind( OperatorKind kind ) => this.Where( m => m.OperatorKind == kind );
}