// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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