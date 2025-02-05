using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Eligibility.TypeOf
{
    internal class TestAspect : MethodAspect
    {
        public override void BuildAspect( IAspectBuilder<IMethod> builder ) { }

        public override void BuildEligibility( IEligibilityBuilder<IMethod> builder )
        {
            var runTimeClass = typeof(RunTimeClass);

            builder.MustSatisfy(
                method => method.Attributes.Any(
                    a => a.ConstructorArguments is { Length: 1 } && a.ConstructorArguments.Single().Value as string == runTimeClass.Name ),
                method => $"{method} must have a an attribute with {runTimeClass} argument" );

            var runTimeOrCompileTimeClass = typeof(RunTimeOrCompileTimeClass);

            builder.MustSatisfy(
                method => method.Attributes.Any(
                    a => a.ConstructorArguments is { Length: 1 } && a.ConstructorArguments.Single().Value as string == runTimeOrCompileTimeClass.Name ),
                method => $"{method} must have a an attribute with {runTimeOrCompileTimeClass} argument" );

            var compileTimeClass = typeof(CompileTimeClass);

            builder.MustSatisfy(
                method => method.Attributes.Any(
                    a => a.ConstructorArguments is { Length: 1 } && a.ConstructorArguments.Single().Value as string == compileTimeClass.Name ),
                method => $"{method} must have a an attribute with {compileTimeClass} argument" );

            AssertEqual( "CompileTimeType", typeof(RunTimeClass).GetType().Name );
            AssertEqual( "RunTimeClass", typeof(RunTimeClass).Name );
            AssertEqual( "Metalama.Framework.Tests.AspectTests.Aspects.Eligibility.TypeOf", typeof(RunTimeClass).Namespace );
            AssertEqual( "Metalama.Framework.Tests.AspectTests.Aspects.Eligibility.TypeOf.RunTimeClass", typeof(RunTimeClass).FullName );
            AssertEqual( "Metalama.Framework.Tests.AspectTests.Aspects.Eligibility.TypeOf.RunTimeClass", typeof(RunTimeClass).ToString() );

            AssertEqual( "CompileTimeType", typeof(GlobalNamespaceRuntimeClass).GetType().Name );
            AssertEqual( "GlobalNamespaceRuntimeClass", typeof(GlobalNamespaceRuntimeClass).Name );
            AssertEqual( null, typeof(GlobalNamespaceRuntimeClass).Namespace );
            AssertEqual( "GlobalNamespaceRuntimeClass", typeof(GlobalNamespaceRuntimeClass).FullName );
            AssertEqual( "GlobalNamespaceRuntimeClass", typeof(GlobalNamespaceRuntimeClass).ToString() );

            AssertEqual( "CompileTimeType", typeof(GenericRunTimeClass<>).GetType().Name );
            AssertEqual( "GenericRunTimeClass`1", typeof(GenericRunTimeClass<>).Name );
            AssertEqual( "Metalama.Framework.Tests.AspectTests.Aspects.Eligibility.TypeOf", typeof(GenericRunTimeClass<>).Namespace );
            AssertEqual( "Metalama.Framework.Tests.AspectTests.Aspects.Eligibility.TypeOf.GenericRunTimeClass`1", typeof(GenericRunTimeClass<>).FullName );
            AssertEqual( "Metalama.Framework.Tests.AspectTests.Aspects.Eligibility.TypeOf.GenericRunTimeClass`1[T]", typeof(GenericRunTimeClass<>).ToString() );

            AssertEqual( "CompileTimeType", typeof(GenericRunTimeClass<int>).GetType().Name );
            AssertEqual( "GenericRunTimeClass`1", typeof(GenericRunTimeClass<int>).Name );
            AssertEqual( "Metalama.Framework.Tests.AspectTests.Aspects.Eligibility.TypeOf", typeof(GenericRunTimeClass<int>).Namespace );

            AssertEqual(
                "Metalama.Framework.Tests.AspectTests.Aspects.Eligibility.TypeOf.GenericRunTimeClass`1[System.Int32]",
                typeof(GenericRunTimeClass<int>).FullName );

            AssertEqual(
                "Metalama.Framework.Tests.AspectTests.Aspects.Eligibility.TypeOf.GenericRunTimeClass`1[System.Int32]",
                typeof(GenericRunTimeClass<int>).ToString() );

            void AssertEqual( string? expected, string? actual )
            {
                if (expected != actual)
                {
                    throw new InvalidOperationException( $"{expected} != {actual}" );
                }
            }
        }
    }

    internal class GenericRunTimeClass<T> { }

    internal class RunTimeClass { }

    [RunTimeOrCompileTime]
    internal class RunTimeOrCompileTimeClass { }

    [CompileTime]
    internal class CompileTimeClass { }

    [AttributeUsage( AttributeTargets.Method, AllowMultiple = true )]
    internal class TypeAttribute : Attribute
    {
        public TypeAttribute( string type ) { }
    }

    // <target>
    public partial class TargetClass
    {
        [TestAspect]
        [Type( nameof(RunTimeClass) )]
        [Type( nameof(RunTimeOrCompileTimeClass) )]
        [Type( nameof(CompileTimeClass) )]
        private void M1() { }

        [TestAspect]
        [Type( nameof(RunTimeClass) )]
        [Type( nameof(RunTimeOrCompileTimeClass) )]
        private void M2() { }

        [TestAspect]
        [Type( nameof(RunTimeClass) )]
        private void M3() { }

        [TestAspect]
        private void M4() { }
    }
}

internal class GlobalNamespaceRuntimeClass { }