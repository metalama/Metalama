// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;

namespace Metalama.Framework.GenerateMetaSyntaxRewriter.Model
{
    internal static class TreeFlattening
    {
        public static void FlattenChildren( Tree tree )
        {
            foreach ( var type in tree.Types )
            {
                switch ( type )
                {
                    case AbstractNode node:
                        FlattenChildren( node.Children, node.Fields, makeOptional: false );

                        break;

                    case Node node:
                        FlattenChildren( node.Children, node.Fields, makeOptional: false );

                        break;
                }
            }
        }

        private static void FlattenChildren( List<TreeTypeChild> fieldsAndChoices, List<Field> fields, bool makeOptional )
        {
            foreach ( var fieldOrChoice in fieldsAndChoices )
            {
                switch ( fieldOrChoice )
                {
                    case Field field:
                        if ( makeOptional && !Generator.IsAnyNodeList( field.Type ) )
                        {
                            field.Optional = "true";
                        }

                        fields.Add( field );

                        break;

                    case Choice choice:
                        // Children of choices are always optional (since the point is to
                        // chose from one of them and leave out the rest).
                        FlattenChildren( choice.Children, fields, makeOptional: true );

                        break;

                    case Sequence sequence:
                        FlattenChildren( sequence.Children, fields, makeOptional );

                        break;

                    default:
                        throw new InvalidOperationException( "Unknown child type." );
                }
            }
        }
    }
}