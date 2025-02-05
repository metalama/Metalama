// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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