// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Tags
{
    /*
     * Tests that tags are correctly passed to templates of interface members.
     */

    public interface IInterface1
    {
        int InterfaceMethod1();

        event EventHandler Event1;

        int Property1 { get; set; }
    }

    public interface IInterface2
    {
        int InterfaceMethod2();

        event EventHandler Event2;

        int Property2 { get; set; }
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( typeof(IInterface1), tags: new { TestTag = "TestValue_For_Interface1" } );
            aspectBuilder.ImplementInterface( typeof(IInterface2), tags: new { TestTag = "TestValue_For_Interface2" } );
        }

        [InterfaceMember]
        public int InterfaceMethod1()
        {
            Console.WriteLine( $"This is introduced interface member with Tag {meta.Tags["TestTag"]}." );

            return meta.Proceed();
        }

        [InterfaceMember]
        public event EventHandler? Event1
        {
            add
            {
                Console.WriteLine( $"This is introduced interface member with Tag {meta.Tags["TestTag"]}." );
            }

            remove
            {
                Console.WriteLine( $"This is introduced interface member with Tag {meta.Tags["TestTag"]}." );
            }
        }

        [InterfaceMember]
        public int Property1
        {
            get
            {
                Console.WriteLine( $"This is introduced interface member with Tag {meta.Tags["TestTag"]}." );

                return 42;
            }

            set
            {
                Console.WriteLine( $"This is introduced interface member with Tag {meta.Tags["TestTag"]}." );
            }
        }

        [InterfaceMember]
        public int InterfaceMethod2()
        {
            Console.WriteLine( $"This is introduced interface member with Tag {meta.Tags["TestTag"]}." );

            return meta.Proceed();
        }

        [InterfaceMember]
        public event EventHandler? Event2
        {
            add
            {
                Console.WriteLine( $"This is introduced interface member with Tag {meta.Tags["TestTag"]}." );
            }

            remove
            {
                Console.WriteLine( $"This is introduced interface member with Tag {meta.Tags["TestTag"]}." );
            }
        }

        [InterfaceMember]
        public int Property2
        {
            get
            {
                Console.WriteLine( $"This is introduced interface member with Tag {meta.Tags["TestTag"]}." );

                return 42;
            }

            set
            {
                Console.WriteLine( $"This is introduced interface member with Tag {meta.Tags["TestTag"]}." );
            }
        }
    }

    // <target>
    [Introduction]
    public class TargetClass { }
}