// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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