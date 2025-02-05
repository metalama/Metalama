#pragma warning disable CS0067
#pragma warning disable CS0169

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Introduce_BuildReflection_NotNull;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Verification), typeof(Introduction) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Introduce_BuildReflection_NotNull
{
    public class Introduction : TypeAspect
    {
        // Events
        [Introduce]
        public event EventHandler? EventField;

        // Public fields.
        [Introduce]
        public int IntroducedField;

        [Introduce]
        public int IntroducedField_Initializer = 42;

        [Introduce]
        public static int IntroducedField_Static;

        [Introduce]
        public static int IntroducedField_Static_Initializer = 42;

        // Private fields.
        [Introduce]
        private int IntroducedField_Private;

        [Introduce]
        private int IntroducedField_Initializer_Private = 42;

        [Introduce]
        private static int IntroducedField_Static_Private;

        // Public methods.
        [Introduce]
        public void IntroducedMethod_Void()
        {
            Console.WriteLine( "This is introduced method." );
            Console.WriteLine( IntroducedField_Initializer_Private );
            meta.Proceed();
        }

        [Introduce]
        public int IntroducedMethod_Int()
        {
            Console.WriteLine( "This is introduced method." );

            return meta.Proceed();
        }

        [Introduce]
        public int IntroducedMethod_Param( int x )
        {
            Console.WriteLine( $"This is introduced method, x = {x}." );

            return meta.Proceed();
        }

        [Introduce]
        public static int IntroducedMethod_StaticSignature()
        {
            Console.WriteLine( "This is introduced method." );

            return meta.Proceed();
        }

        [Introduce( IsVirtual = true )]
        public int IntroducedMethod_VirtualExplicit()
        {
            Console.WriteLine( "This is introduced method." );

            return meta.Proceed();
        }

        // Private methods.
        [Introduce]
        private void IntroducedMethod_Void_Private()
        {
            Console.WriteLine( "This is introduced method." );
            meta.Proceed();
        }

        [Introduce]
        private int IntroducedMethod_Int_Private()
        {
            Console.WriteLine( "This is introduced method." );

            return meta.Proceed();
        }

        [Introduce]
        private int IntroducedMethod_Param_Private( int x )
        {
            Console.WriteLine( $"This is introduced method, x = {x}." );

            return meta.Proceed();
        }

        [Introduce]
        private static int IntroducedMethod_StaticSignature_Private()
        {
            Console.WriteLine( "This is introduced method." );

            return meta.Proceed();
        }

        // Generic methods
        [Introduce]
        public T GenericMethod<T>( T a )
        {
            return a;
        }

        // Methods with modifiers.
        [Introduce]
        public void OutMethod( out int x )
        {
            x = 42;
            Console.WriteLine( "OutMethod with parameter." );
        }

        [Introduce]
        public int RefMethod( ref int x )
        {
            x += 42;

            return 42;
        }

        // Public properties.
        [Introduce]
        public int IntroducedProperty_Auto { get; set; }

        [Introduce]
        public int IntroducedProperty_Auto_Initializer { get; set; } = 42;

        [Introduce]
        public int IntroducedProperty_Auto_GetOnly { get; }

        [Introduce]
        public int IntroducedProperty_Auto_GetOnly_Initializer { get; } = 42;

        [Introduce]
        public static int IntroducedProperty_Auto_Static { get; set; }

        [Introduce]
        public int IntroducedProperty_Accessors
        {
            get
            {
                Console.WriteLine( "Get" );

                return 42;
            }

            set
            {
                Console.WriteLine( value );
            }
        }

        // Private properties.
        [Introduce]
        private int IntroducedProperty_Auto_Private { get; set; }

        [Introduce]
        private int IntroducedProperty_Auto_Initializer_Private { get; set; } = 42;

        [Introduce]
        private int IntroducedProperty_Auto_GetOnly_Private { get; }

        [Introduce]
        private int IntroducedProperty_Auto_GetOnly_Initializer_Private { get; } = 42;

        [Introduce]
        private static int IntroducedProperty_Auto_Static_Private { get; set; }

        [Introduce]
        private int IntroducedProperty_Accessors_Private
        {
            get
            {
                Console.WriteLine( "Get" );

                return 42;
            }

            set
            {
                Console.WriteLine( value );
            }
        }
    }

    public static class Assert
    {
        public static void NotNull( object? obj )
        {
            if (obj == null)
            {
                throw new Exception();
            }
        }
    }

    public class Verification : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            var type = meta.Target.Type;

            foreach (var f in type.Fields.OrderBy( f => f.Name ))
            {
                var fieldInfo = f.ToFieldInfo();
                Assert.NotNull( fieldInfo );
                var fieldOrPropertyInfo = f.ToFieldOrPropertyInfo();
                Assert.NotNull( fieldOrPropertyInfo );
            }

            foreach (var e in type.Events.OrderBy( f => f.Name ))
            {
                var eventInfo = e.ToEventInfo();
                Assert.NotNull( eventInfo );
            }

            foreach (var p in type.Properties.OrderBy( f => f.Name ))
            {
                var propertyInfo = p.ToPropertyInfo();
                Assert.NotNull( propertyInfo );
                var fieldOrPropertyInfo = p.ToFieldOrPropertyInfo();
                Assert.NotNull( fieldOrPropertyInfo );
            }

            foreach (var m in type.Methods.OrderBy( f => f.Name ))
            {
                var methodInfo = m.ToMethodInfo();
                Assert.NotNull( methodInfo );
            }

            foreach (var c in type.Constructors.OrderBy( f => f.Name ))
            {
                var constructorInfo = c.ToConstructorInfo();
                Assert.NotNull( constructorInfo );
            }

            return default;
        }
    }

    //<target>
    [Introduction]
    internal class Target
    {
        [Verification]
        public static void Verify() { }

        private Target() { }

        private Target( int x ) { }

        private Target( ref int x ) { }
    }

    public static class Program
    {
        public static void TestMain()
        {
            Target.Verify();
            Console.WriteLine( "Correct!" );
        }
    }
}