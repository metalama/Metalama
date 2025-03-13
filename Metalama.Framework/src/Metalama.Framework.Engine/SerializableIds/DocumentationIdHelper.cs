// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Utilities.Caching;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.SerializableIds
{
    /// <summary>
    /// APIs for constructing documentation comment ids, and finding declarations that match ids.
    /// </summary>
    internal static partial class DocumentationIdHelper
    {
        /// <summary>
        /// Creates an id string used by external documentation comment files to identify declarations
        /// of types, namespaces, methods, properties, etc.
        /// </summary>
        public static string CreateDeclarationId( IDeclaration declaration )
        {
            if ( declaration == null )
            {
                throw new ArgumentNullException( nameof(declaration) );
            }

            using var builder = StringBuilderPool.Default.Allocate();

            var generator = new GeneratorOfDeclarationIdFromDeclaration( builder.Value );
            generator.Visit( declaration );

            return builder.Value.ToString();
        }

        /// <summary>
        /// Creates an id string used to reference type symbols (not strictly declarations, includes
        /// arrays, pointers, type parameters, etc.)
        /// </summary>
        public static string CreateReferenceId( ICompilationElement type )
        {
            if ( type == null )
            {
                throw new ArgumentNullException( nameof(type) );
            }

            using var builder = StringBuilderPool.Default.Allocate();
            var generator = new GeneratorOfReferenceIdFromDeclaration( builder.Value, typeParameterContext: null );
            generator.Visit( type );

            return builder.Value.ToString();
        }

        /// <summary>
        /// Gets the first declaration that matches the declaration id string, order undefined.
        /// </summary>
        public static IDeclaration? GetFirstDeclarationForDeclarationId( string id, CompilationModel compilation )
        {
            if ( id == null )
            {
                throw new ArgumentNullException( nameof(id) );
            }

            if ( compilation == null )
            {
                throw new ArgumentNullException( nameof(compilation) );
            }

            var results = new List<IDeclaration>();

            Parser.ParseDeclaredSymbolId( id, compilation, results );

            return results.Count == 0 ? null : results[0];
        }

        private static int GetTotalTypeParameterCount( INamedType? namedType )
        {
            var n = 0;

            while ( namedType != null )
            {
                n += namedType.TypeParameters.Count;
                namedType = namedType.ContainingDeclaration as INamedType;
            }

            return n;
        }

        // encodes dots with alternate # character
        private static string EncodeName( string name ) => name.Replace( '.', '#' );

        private static string EncodePropertyName( string name )
        {
            // convert C# indexer names to 'Item'
            if ( name == "this[]" )
            {
                name = "Item";
            }
            else if ( name.EndsWith( ".this[]", StringComparison.Ordinal ) )
            {
                name = name.Substring( 0, name.Length - 6 ) + "Item";
            }

            return name;
        }

        private static string DecodePropertyName( string name )
        {
            // special case, csharp names indexers 'this[]', not 'Item'
            if ( name == "Item" )
            {
                name = "this[]";
            }
            else if ( name.EndsWith( ".Item", StringComparison.Ordinal ) )
            {
                name = name.Substring( 0, name.Length - 4 ) + "this[]";
            }

            return name;
        }
    }
}