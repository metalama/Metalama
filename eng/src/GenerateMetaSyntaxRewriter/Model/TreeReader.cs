// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace Metalama.Framework.GenerateMetaSyntaxRewriter.Model;

internal static class TreeReader
{
    public static Tree ReadTree( string inputFile )
    {
        SyntaxXmlCleaner.Clean( inputFile );
        var reader = XmlReader.Create( inputFile, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit } );
        var serializer = new XmlSerializer( typeof(Tree) );
        var tree = (Tree) serializer.Deserialize( reader )!;
        RemoveExperimentalDeclarations( tree );
        TreeFlattening.FlattenChildren( tree );

        return tree;
    }

    /// <summary>
    /// Removes the nodes and the fields that belong to an experimental Roslyn feature.
    /// </summary>
    /// <remarks>
    /// The <c>Syntax-*.xml</c> files are copied unchanged from the Roslyn version they describe, so they declare the
    /// experimental nodes as well. Roslyn annotates the corresponding API with <c>ExperimentalAttribute</c>, which turns
    /// every reference from generated code into an <c>RSEXPERIMENTAL</c> error. Experimental features are not supported,
    /// so the declarations are removed here rather than omitted from the grammar file: the file has to keep describing
    /// the Roslyn version it is named after.
    /// </remarks>
    private static void RemoveExperimentalDeclarations( Tree tree )
    {
        tree.Types.RemoveAll( t => t.IsExperimental );

        foreach ( var type in tree.Types )
        {
            RemoveExperimentalChildren( type.Children );
        }
    }

    /// <summary>
    /// Removes the fields that belong to an experimental Roslyn feature from a list of children, and from the
    /// children of every <see cref="Choice"/> and <see cref="Sequence"/> that the list contains.
    /// </summary>
    /// <remarks>
    /// The children of a node form a tree, because a <see cref="Choice"/> and a <see cref="Sequence"/> hold children
    /// of their own. An experimental field can appear at any depth of that tree, so the removal is recursive. The
    /// <see cref="Choice"/> and <see cref="Sequence"/> nodes themselves are never removed: they carry no
    /// <c>ExperimentalUrl</c> attribute, and one that becomes empty generates no code.
    /// </remarks>
    private static void RemoveExperimentalChildren( List<TreeTypeChild> children )
    {
        children.RemoveAll( c => c is Field { IsExperimental: true } );

        foreach ( var child in children )
        {
            switch ( child )
            {
                case Choice choice:
                    RemoveExperimentalChildren( choice.Children );

                    break;

                case Sequence sequence:
                    RemoveExperimentalChildren( sequence.Children );

                    break;
            }
        }
    }
}
