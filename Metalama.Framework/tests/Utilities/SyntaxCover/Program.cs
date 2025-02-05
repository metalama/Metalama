using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SyntaxCover
{
    class Program
    {
        static readonly HashSet<string> _unsupportedSyntaxKinds = new HashSet<string>();

        static void Main(string[] args)
        {
            string workDir = Path.GetFullPath(@"..\..\..\..\artifacts\tests\");

            Dictionary<string, int> syntaxKindCounts = GetAllSyntaxKinds().ToDictionary( s => s.ToString(), s => 0 );

            var testOutputFiles = Directory.GetFiles( Path.Combine( workDir, "SyntaxCover" ), "*.txt", SearchOption.AllDirectories );
            foreach ( var testOutputFile in testOutputFiles )
            {
                foreach (var line in File.ReadAllLines( testOutputFile ))
                {
                    string usedSyntaxKind;
                    if ( line.EndsWith("*"))
                    {
                        // This SyntaxKind is unsupported.
                        usedSyntaxKind = line.Substring( 0, line.Length - 1 );
                        _unsupportedSyntaxKinds.Add( usedSyntaxKind );
                    }
                    else
                    {
                        usedSyntaxKind = line;
                    }

                    if (syntaxKindCounts.ContainsKey(usedSyntaxKind))
                    {
                        syntaxKindCounts[usedSyntaxKind]++;
                    }
                }
            }

            WriteCounts( Path.Combine( workDir, @"SyntaxCoverReport.csv" ), syntaxKindCounts );
        }

        static IEnumerable<SyntaxKind> GetAllSyntaxKinds()
        {
            var excludedNamesRegexes = new[] { "Trivia$", "^Xml", "Cref", "List$" };

            var syntaxKinds = ((SyntaxKind[]) Enum.GetValues( typeof( SyntaxKind ) ))
                .Where(
                    kind =>
                        !SyntaxFacts.IsAnyToken( kind ) &&
                        !excludedNamesRegexes.Any( regex => Regex.IsMatch( kind.ToString(), regex ) ) );

            return syntaxKinds;
        }

        static void WriteCounts( string filePath, Dictionary<string, int> syntaxKindCounts )
        {
            using ( var writer = File.CreateText( filePath) )
            {
                foreach ( var item in syntaxKindCounts.OrderBy(s => s.Key) )
                {
                    string unsupported = _unsupportedSyntaxKinds.Contains( item.Key ) ? "UNSUPPORTED" : "";
                    writer.WriteLine($"{item.Key},{item.Value},{unsupported}");
                }
            }
        }
    }
}
