// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Collections
{
    internal sealed class NamespaceCollection : DeclarationCollection<INamespace>, INamespaceCollection
    {
        public NamespaceCollection( INamespace declaringType, IReadOnlyList<IFullRef<INamespace>> sourceItems ) : base(
            declaringType,
            sourceItems ) { }

        public INamespace? OfName( string name ) => this.SingleOrDefault( ns => ns.Name == name );

        IEnumerable<INamespace> INamedDeclarationCollection<INamespace>.OfName( string name )
        {
            if ( name.ContainsOrdinal( '.' ) )
            {
                throw new ArgumentOutOfRangeException( nameof(name), "The name cannot contain a period." );
            }
            
            var ns = this.OfName( name );

            if ( ns == null )
            {
                return [];
            }
            else
            {
                return [ns];
            }
        }
    }
}