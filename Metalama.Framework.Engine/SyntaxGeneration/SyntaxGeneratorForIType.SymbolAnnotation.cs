// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.SyntaxGeneration;

internal sealed partial class SyntaxGeneratorForIType
{
    internal static class SymbolAnnotation
    {
        private const string _kind = "SymbolId";

        public static SyntaxAnnotation Create( ICompilationElement type ) => new( _kind, DeclarationDocumentationCommentId.CreateReferenceId( type ) );
    }
}