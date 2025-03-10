// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System.Reflection;

namespace Metalama.Framework.Engine.SyntaxGeneration;

internal partial class SyntaxGeneratorForIType
{
    private static class NullableSyntaxAnnotationEx
    {
        public static SyntaxAnnotation? Oblivious { get; }

        public static SyntaxAnnotation? AnnotatedOrNotAnnotated { get; }

        static NullableSyntaxAnnotationEx()
        {
            var nullableSyntaxAnnotation = typeof(Workspace).Assembly.GetType(
                "Microsoft.CodeAnalysis.CodeGeneration.NullableSyntaxAnnotation",
                throwOnError: false );

            if ( nullableSyntaxAnnotation != null )
            {
                Oblivious = (SyntaxAnnotation?) nullableSyntaxAnnotation.GetField( nameof(Oblivious), BindingFlags.Static | BindingFlags.Public )
                    ?.GetValue( null );

                AnnotatedOrNotAnnotated = (SyntaxAnnotation?) nullableSyntaxAnnotation
                    .GetField( nameof(AnnotatedOrNotAnnotated), BindingFlags.Static | BindingFlags.Public )
                    ?.GetValue( null );
            }
        }
    }
}