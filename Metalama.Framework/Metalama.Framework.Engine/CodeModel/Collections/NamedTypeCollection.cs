// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Collections
{
    internal sealed partial class NamedTypeCollection : MemberOrNamedTypeCollection<INamedType>, INamedTypeCollection
    {
        public NamedTypeCollection( INamespaceOrNamedType declaringType, IReadOnlyList<IFullRef<INamedType>> sourceItems, bool includeNestedTypes = false ) :
            base( declaringType, IncludeNestedTypes( declaringType.Compilation, sourceItems, includeNestedTypes ) ) { }

        public NamedTypeCollection( ICompilation declaringCompilation, IReadOnlyList<IFullRef<INamedType>> sourceItems, bool includeNestedTypes = false ) :
            base( declaringCompilation, IncludeNestedTypes( declaringCompilation, sourceItems, includeNestedTypes ) ) { }

        public IEnumerable<INamedType> OfTypeDefinition( INamedType typeDefinition )
        {
            foreach ( var reference in this.Source )
            {
                if ( reference.IsConvertibleTo( typeDefinition, ConversionKind.TypeDefinition ) )
                {
                    // Resolve the reference and store the declaration.
                    var member = this.GetItem( reference );

                    // Return the result.
                    yield return member;
                }
            }
        }

        private static IReadOnlyList<IFullRef<INamedType>> IncludeNestedTypes(
            ICompilation compilation,
            IReadOnlyList<IFullRef<INamedType>> sourceItems,
            bool includeNestedTypes )
        {
            if ( !includeNestedTypes )
            {
                return sourceItems;
            }
            else
            {
                return new FlattenedList( compilation.GetCompilationModel(), sourceItems );
            }
        }
    }
}