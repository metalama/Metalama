// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.NameConflict_Source
{
    /*
     * Verifies that names coming from the method builder are included in lexical scope of the property.
     */
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder ) { }

        [Introduce]
        public int Bar()
        {
            Foo();

            return ExpressionFactory.Parse( "Foo()" ).Value;

            void Foo() { }
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass
    {
        public int Foo()
        {
            return 42;
        }
    }
}