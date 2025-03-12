// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
        TreeFlattening.FlattenChildren( tree );

        return tree;
    }
}