// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.Parameters;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(IntroduceAspectAttribute) )]

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.Parameters
{
    // Tests single OverrideMethod aspect with trivial template on methods with trivial bodies.

    public class OverrideAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            var result = meta.Proceed();
            Console.WriteLine( "This is the override method." );

            foreach (var param in meta.Target.Method.Parameters)
            {
                Console.WriteLine( $"Param {param.Name} = {param.Value}" );
            }

            Console.WriteLine( $"Returns {meta.Target.Method.ReturnParameter.Type.ToString()}" );

            return result;
        }
    }

    internal class IntroduceAspectAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var method in builder.AdvisedTarget.Methods)
            {
                builder.With( method ).AddAspect( new OverrideAttribute() );
            }
        }

        [Introduce]
        public void IntroducedVoidNoParameters()
        {
            Console.WriteLine( "This is the original method." );
        }

        [Introduce]
        public void IntroducedVoidTwoParameters( int x, int y )
        {
            Console.WriteLine( "This is the introduced method." );
        }

        [Introduce]
        public void IntroducedVoidGeneric<T>( T param )
        {
            Console.WriteLine( "This is the introduced method." );
        }

        [Introduce]
        public int IntroducedIntNoParameters()
        {
            Console.WriteLine( "This is the introduced method." );

            return 42;
        }

        [Introduce]
        public object IntroducedObjectNoParameters()
        {
            Console.WriteLine( "This is the introduced method." );

            return new object();
        }

        [Introduce]
        public T IntroducedGeneric<T>( T param )
        {
            Console.WriteLine( "This is the introduced method." );

            return param;
        }

        [Introduce]
        public void IntroducedOutParameter( out int x )
        {
            Console.WriteLine( "This is the introduced method." );
            x = 42;
        }

        [Introduce]
        public void IntroducedRefParameter( ref int x )
        {
            Console.WriteLine( "This is the introduced method." );
            x = 42;
        }

        [Introduce]
        public void IntroducedInParameter( in DateTime x )
        {
            Console.WriteLine( "This is the introduced method." );
        }
    }

    // <target>
    [IntroduceAspect]
    internal class TargetClass
    {
        [Override]
        public void VoidNoParameters()
        {
            Console.WriteLine( "This is the original method." );
        }

        [Override]
        public void VoidTwoParameters( int x, int y )
        {
            Console.WriteLine( "This is the original method." );
        }

        public void VoidGeneric<T>( T param )
        {
            Console.WriteLine( "This is the original method." );
        }

        [Override]
        public int IntNoParameters()
        {
            Console.WriteLine( "This is the original method." );

            return 42;
        }

        [Override]
        public object ObjectNoParameters()
        {
            Console.WriteLine( "This is the original method." );

            return new object();
        }

        public T Generic<T>( T param )
        {
            Console.WriteLine( "This is the original method." );

            return param;
        }

        [Override]
        public void OutParameter( out int x )
        {
            Console.WriteLine( "This is the original method." );
            x = 42;
        }

        [Override]
        public void RefParameter( ref int x )
        {
            Console.WriteLine( "This is the original method." );
            x = 42;
        }

        [Override]
        public void InParameter( in DateTime x )
        {
            Console.WriteLine( "This is the original method." );
        }
    }
}