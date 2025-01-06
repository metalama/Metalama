// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Linq;

namespace Metalama.Framework.GenerateMetaSyntaxRewriter.Model;

internal static class VersionDetector
{
    public static void DetectVersions( SyntaxDocument[] syntaxVersions )
    {
        var latestSyntaxVersion = syntaxVersions[^1];

        foreach ( var type in latestSyntaxVersion.Types.OfType<Node>() )
        {
            // Get the corresponding type in all versions.
            var types = syntaxVersions.Select( s => (s.Version, Type: s.GetNode( type.Name )) ).Where( x => x.Type != null ).ToList();

            // Get the minimal version of the syntax defining this type.
            var typeMinimalVersion = types.Select( s => s.Version ).Min();

            // Update the MinimalRoslynVersion property in all syntax documents.
            foreach ( var anyVersionType in types.Select( x => x.Type! ) )
            {
                anyVersionType.MinimalRoslynVersion = typeMinimalVersion;
            }

            foreach ( var fieldName in types.SelectMany( t => t.Type!.Fields ).Select( f => f.Name ).Distinct() )
            {
                // Get the corresponding fields in all versions.
                var fields = types.Select( x => (x.Version, Field: x.Type!.Fields.SingleOrDefault( f => f.Name == fieldName )) )
                    .Where( x => x.Field != null )
                    .ToList();

                // Get the minimal and maximal version of the syntax defining this field.
                var fieldMinimalVersion = fields.Min( f => f.Version );
                var fieldMaximalVersion = fields.Max( f => f.Version );

                // Gets the minimal version for each kind of the field.
                var kindsMinimalVersion = fields.SelectMany( f => f.Field!.Kinds.Select( k => (Kind: k, f.Version) ) )
                    .GroupBy( k => k.Kind )
                    .Select( g => (Kind: g.Key, Version: g.Select( i => i.Version ).Min()) )
                    .ToList();

                // Update the MinimalRoslynVersion property in all syntax documents.
                foreach ( var anyVersionField in fields )
                {
                    anyVersionField.Field!.MinimalRoslynVersion = fieldMinimalVersion;
                    anyVersionField.Field.MaximalRoslynVersion = fieldMaximalVersion;

                    anyVersionField.Field.KindsMinimalRoslynVersions = kindsMinimalVersion.Where( k => k.Version?.Index <= anyVersionField.Version.Index )
                        .ToDictionary( i => i.Kind, i => i.Version );
                }
            }
        }
    }
}