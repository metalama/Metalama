// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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