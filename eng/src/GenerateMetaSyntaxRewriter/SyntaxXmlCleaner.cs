// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Metalama.Framework.GenerateMetaSyntaxRewriter;

/// <summary>
/// Cleans the Syntax-*.xml files so that they can be easily compared.
/// </summary>
internal static class SyntaxXmlCleaner
{
    public static void Clean( string fileName )
    {
        var document = XDocument.Load( fileName );

        var hasChange = false;

        var nodesToRemove = document.Root!.XPathSelectElements(
                "//TypeComment | //PropertyComment | //FactoryComment | //summary",
                new XmlNamespaceManager( new NameTable() ) )
            .ToList();

        foreach ( var element in nodesToRemove )
        {
            hasChange = true;
            element.Remove();
        }

        if ( hasChange )
        {
            document.Save( fileName );
        }
    }
}