// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Initialization.TypeConstructing_Lambda
{
    public class Aspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.AddInitializer( nameof(Template), InitializerKind.BeforeTypeConstructor );
        }

        [Template]
        public void Template()
        {
            var action = ExpressionFactory.Capture( new Func<object, string>( _ => { return "Hello, world."; } ) );
            Invoke( action.Value );
        }

        [Introduce]
        public static void Invoke( Func<object, string> action ) { }
    }

    // <target>
    [Aspect]
    public class TargetCode
    {
        static TargetCode() { }
    }
}