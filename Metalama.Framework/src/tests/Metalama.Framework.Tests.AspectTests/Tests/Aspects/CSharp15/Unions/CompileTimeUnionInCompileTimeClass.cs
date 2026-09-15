// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(15.0)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;
using System;

// Verifies that a union nested in a compile-time class reaches the compile-time compilation and can be used by a
// template. Issue #1942 lists this position beside the run-time one: the member list of a compile-time type used to
// route a nested union to the arm that copies a member verbatim, rather than to the arm that transforms a nested
// type, and it now routes it by the same syntax-kind predicate as the other type declarations.
//
// The template creates a value of the union at compile time and calls a method of it, so the generated code carries
// the description as a literal. That is the proof that the union was compiled into the compile-time assembly and
// executed there. The two arms produce the same output for this union, so the test pins that the position works
// rather than separating them.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.Unions.CompileTimeUnionInCompileTimeClass
{
    [CompileTime]
    public record Circle( double Radius );

    [CompileTime]
    public record Rectangle( double Width, double Height );

    [CompileTime]
    public class Shapes
    {
        public union Shape( Circle, Rectangle )
        {
            public string Describe() => this.Value switch
            {
                Circle circle => $"circle of radius {circle.Radius}",
                Rectangle rectangle => $"rectangle {rectangle.Width} by {rectangle.Height}",
                _ => "unknown shape"
            };
        }
    }

    public class DescribeAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            var shape = new Shapes.Shape( new Circle( 2.5 ) );

            Console.WriteLine( $"The shape is a {shape.Describe()}." );

            return meta.Proceed();
        }
    }

    // <target>
    internal class TargetCode
    {
        [Describe]
        private int Method() => 42;
    }
}
