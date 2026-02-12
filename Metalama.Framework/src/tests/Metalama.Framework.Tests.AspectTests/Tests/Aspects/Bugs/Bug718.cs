// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug718
{
    /*
     * Tests that when searching for a non-interface template, interface members should be ignored.
     * This aspect has an [InterfaceMember] Foo() method and a [Template] Foo<T>() method with the same name.
     * Override(nameof(Foo)) should find the [Template] and not the [InterfaceMember].
     */

    public interface IMyInterface
    {
        void Foo();
    }

    public class MyAspectAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.ImplementInterface( typeof(IMyInterface) );

            foreach ( var method in builder.Target.Methods )
            {
                builder.With( method ).Override( nameof(Foo) );
            }
        }

        [InterfaceMember]
        public void Foo()
        {
            Console.WriteLine( "Interface member implementation" );
        }

        [Template]
        public void Foo<[CompileTime] T>()
        {
            Console.WriteLine( "Before" );
            meta.Proceed();
            Console.WriteLine( "After" );
        }
    }

    // <target>
    [MyAspect]
    public partial class TargetClass
    {
        public void Bar()
        {
            Console.WriteLine( "Original Bar" );
        }
    }
}
