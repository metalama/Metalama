// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Helpers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Tests.UnitTests.Utilities
{
    internal static class CodeModelExtensions
    {
        public static ImmutableArray<T> OrderBySource<T>( this IEnumerable<T> items )
            where T : IDeclaration
            => items.Select( item => (Item: item, Declaration: item.GetPrimaryDeclarationSyntax()) )
                .OrderBy( item => item.Declaration?.SyntaxTree.FilePath )
                .ThenBy( item => item.Declaration?.SpanStart )
                .Select( item => item.Item )
                .ToImmutableArray();
    }
}