// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Collections;

internal sealed class AllTypesCollection : AllMemberOrNamedTypesCollection<INamedType, INamedTypeCollection>, INamedTypeCollection
{
    public AllTypesCollection( INamedTypeImpl declaringType ) : base( declaringType ) { }

    protected override IEqualityComparer<INamedType> Comparer => this.CompilationContext.Comparers.GetTypeComparer( TypeComparison.Default );

    public IEnumerable<INamedType> OfTypeDefinition( INamedType typeDefinition )
    {
        return this.Where( p => p.IsConvertibleTo( typeDefinition, ConversionKind.TypeDefinition ) );
    }

    protected override INamedTypeCollection GetMembers( INamedType namedType ) => namedType.Types;
}