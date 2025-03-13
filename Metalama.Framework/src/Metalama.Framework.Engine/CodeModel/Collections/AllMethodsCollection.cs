// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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