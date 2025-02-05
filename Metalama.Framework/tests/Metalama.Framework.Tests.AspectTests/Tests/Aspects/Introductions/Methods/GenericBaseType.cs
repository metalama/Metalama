using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Introductions.Methods.GenericBaseType
{
    internal class Aspect : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.New )]
        public int ExistingBaseHiddenMethod( int value )
        {
            Console.WriteLine( "This is the hiding method." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public void ExistingBaseHiddenMethod_Void( int value )
        {
            Console.WriteLine( "This is the hiding method." );
            meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.Override )]
        public int ExistingBaseOverriddenMethod( int value )
        {
            Console.WriteLine( "This is the overriding method." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.Override )]
        public void ExistingBaseOverriddenMethod_Void( int value )
        {
            Console.WriteLine( "This is the overriding method." );
            meta.Proceed();
        }
    }

    internal class Base<T>
    {
        public int ExistingBaseHiddenMethod( T param )
        {
            Console.WriteLine( "This is the base method." );

            return 27;
        }

        public void ExistingBaseHiddenMethod_Void( T param )
        {
            Console.WriteLine( "This is the base method." );
        }

        public virtual int ExistingBaseOverriddenMethod( T param )
        {
            Console.WriteLine( "This is the base method." );

            return 27;
        }

        public virtual void ExistingBaseOverriddenMethod_Void( T param )
        {
            Console.WriteLine( "This is the base method." );
        }
    }

    // <target>
    [Aspect]
    internal class TargetCode : Base<int> { }
}