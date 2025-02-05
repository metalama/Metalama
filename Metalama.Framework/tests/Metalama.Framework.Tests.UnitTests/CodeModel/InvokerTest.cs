// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Engine.CodeModel.Invokers;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel
{
    public sealed class InvokerTest : UnitTestClass
    {
        [Fact]
        public void Methods()
        {
            const string code = @"
class TargetCode
{
    void ToString(string format) {}
    
    static void Foo() {}

    void Method()
    {
        void Local() {}
    }

    static void ByRef( out int o, ref string s )
    {
        o = 5;
    }

}";

            using var testContext = this.CreateTestContext();
            var serviceProvider = testContext.ServiceProvider;

            var compilation = testContext.CreateCompilationModel( code );

            var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );
            var generator = syntaxSerializationContext.SyntaxGenerationContext.SyntaxGenerator;

            using ( TemplateExpansionContext.WithTestingContext(
                       syntaxSerializationContext,
                       serviceProvider ) )
            {
                var type = compilation.Types.Single();
                var toString = type.Methods.OfName( "ToString" ).Single();
                var fooMethod = type.Methods.OfName( "Foo" ).Single();
                var byRefMethod = type.Methods.OfName( "ByRef" ).Single();

                // Test normal case.
                AssertEx.DynamicEquals(
                    toString.With( new TypedExpressionSyntaxImpl( generator.ThisExpression(), compilation ) )
                        .Invoke( new TypedExpressionSyntaxImpl( SyntaxFactoryEx.LiteralExpression( "x" ), compilation ) ),
                    @"((global::TargetCode)this).ToString((global::System.String)""x"")" );

                AssertEx.DynamicEquals(
                    toString.With( new TypedExpressionSyntaxImpl( generator.IdentifierName( "a" ), compilation ), InvokerOptions.NullConditional )
                        .Invoke( new TypedExpressionSyntaxImpl( SyntaxFactoryEx.LiteralExpression( "x" ), compilation ) ),
                    @"((global::TargetCode)a)?.ToString((global::System.String)""x"")" );

                AssertEx.DynamicEquals(
                    toString.With( new TypedExpressionSyntaxImpl( SyntaxFactoryEx.LiteralExpression( 42 ), compilation ) )
                        .Invoke( new TypedExpressionSyntaxImpl( SyntaxFactoryEx.LiteralExpression( 43 ), compilation ) ),
                    @"((global::TargetCode)42).ToString((global::System.String)43)" );

                // Test static call.
                AssertEx.DynamicEquals(
                    fooMethod.Invoke(),
                    @"global::TargetCode.Foo()" );

                // Test exception related to the 'instance' parameter.
                AssertEx.DynamicEquals(
                    fooMethod.Invoke(),
                    @"global::TargetCode.Foo()" );

                // Test in/out.
                var intType = compilation.Factory.GetTypeByReflectionType( typeof(int) );

                AssertEx.DynamicEquals(
                    byRefMethod.Invoke(
                        new TypedExpressionSyntaxImpl( generator.IdentifierName( "x" ), intType, compilation, isReferenceable: true ),
                        new TypedExpressionSyntaxImpl( generator.IdentifierName( "y" ), intType, compilation, isReferenceable: true ) ),
                    @"global::TargetCode.ByRef(out x, ref y)" );

                AssertEx.ThrowsWithDiagnostic(
                    GeneralDiagnosticDescriptors.CannotPassExpressionToByRefParameter,
                    () => byRefMethod.Invoke(
                        new TypedExpressionSyntaxImpl( generator.IdentifierName( "x" ), compilation, isReferenceable: false ),
                        new TypedExpressionSyntaxImpl( generator.IdentifierName( "y" ), compilation, isReferenceable: false ) ) );
            }
        }

        [Fact]
        public void OpenGenerics()
        {
            const string code = @"
class TargetCode
{
    class Nested<T1> {
        static void StaticGenericMethod<T2>() {}
        static void StaticNonGenericMethod() {}
        static int StaticField;
        static string StaticProperty {get;set;}
        static event System.Action<object> StaticEvent;
        void InstanceGenericMethod<T2>() {}
        void InstanceNonGenericMethod() {}
        int InstanceField;
        string InstanceProperty {get;set;}
        event System.Action<object> InstanceEvent;
    }

    void Method()
    {
    }
}";

            using var testContext = this.CreateTestContext();
            var serviceProvider = testContext.ServiceProvider;

            var compilation = testContext.CreateCompilationModel( code );

            var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

            using ( TemplateExpansionContext.WithTestingContext(
                       syntaxSerializationContext,
                       serviceProvider ) )
            {
                var type = compilation.Types.OfName( "TargetCode" ).Single();
                var nestedType = type.Types.Single();

                // Testing static members.
                var staticGenericMethod = nestedType.Methods.OfName( "StaticGenericMethod" ).Single();
                var staticNonGenericMethod = nestedType.Methods.OfName( "StaticNonGenericMethod" ).Single();
                var staticField = nestedType.Fields.OfName( "StaticField" ).Single();
                var staticProperty = nestedType.Properties.OfName( "StaticProperty" ).Single();
                var staticEvent = nestedType.Events.OfName( "StaticEvent" ).Single();

                AssertEx.DynamicEquals( staticGenericMethod.Invoke(), "global::TargetCode.Nested<T1>.StaticGenericMethod<T2>()" );

                AssertEx.DynamicEquals( staticNonGenericMethod.Invoke(), "global::TargetCode.Nested<T1>.StaticNonGenericMethod()" );

                AssertEx.DynamicEquals( staticField.Value, "global::TargetCode.Nested<T1>.StaticField" );
                AssertEx.DynamicEquals( staticProperty.Value, "global::TargetCode.Nested<T1>.StaticProperty" );
                AssertEx.DynamicEquals( staticEvent.Add( null ), "global::TargetCode.Nested<T1>.StaticEvent += null" );

                // Testing instance members on a generic type.
                var instance = new TypedExpressionSyntaxImpl( SyntaxFactory.ParseExpression( "abc" ), syntaxSerializationContext.CompilationModel );
                var instanceGenericMethod = nestedType.Methods.OfName( "InstanceGenericMethod" ).Single();
                var instanceNonGenericMethod = nestedType.Methods.OfName( "InstanceNonGenericMethod" ).Single();
                var instanceField = nestedType.Fields.OfName( "InstanceField" ).Single();
                var instanceProperty = nestedType.Properties.OfName( "InstanceProperty" ).Single();
                var instanceEvent = nestedType.Events.OfName( "InstanceEvent" ).Single();

                AssertEx.DynamicEquals(
                    instanceGenericMethod.With( instance ).Invoke(),
                    "((global::TargetCode.Nested<T1>)abc).InstanceGenericMethod<T2>()" );

                AssertEx.DynamicEquals(
                    instanceNonGenericMethod.With( instance ).Invoke(),
                    "((global::TargetCode.Nested<T1>)abc).InstanceNonGenericMethod()" );

                AssertEx.DynamicEquals( instanceField.With( instance ).Value, "((global::TargetCode.Nested<T1>)abc).InstanceField" );
                AssertEx.DynamicEquals( instanceProperty.With( instance ).Value, "((global::TargetCode.Nested<T1>)abc).InstanceProperty" );
                AssertEx.DynamicEquals( instanceEvent.With( instance ).Add( null ), "((global::TargetCode.Nested<T1>)abc).InstanceEvent += null" );
            }
        }

        [Fact]
        public void Generics()
        {
            const string code = @"
class TargetCode
{
    class Nested<T1> {
        static void StaticGenericMethod<T2>() {}
        static void StaticNonGenericMethod() {}
        static int StaticField;
        static string StaticProperty {get;set;}
        static event System.Action<object> StaticEvent;
        void InstanceGenericMethod<T2>() {}
        void InstanceNonGenericMethod() {}
        int InstanceField;
        string InstanceProperty {get;set;}
        event System.Action<object> InstanceEvent;
    }

    void Method()
    {
    }
}";

            using var testContext = this.CreateTestContext();
            var serviceProvider = testContext.ServiceProvider;

            var compilation = testContext.CreateCompilationModel( code );

            var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

            using ( TemplateExpansionContext.WithTestingContext(
                       syntaxSerializationContext,
                       serviceProvider ) )
            {
                var type = compilation.Types.OfName( "TargetCode" ).Single();
                var nestedType = type.Types.Single().WithTypeArguments( compilation.Factory.GetTypeByReflectionType( typeof(string) ) );

                // Testing static members.
                var staticGenericMethod = nestedType.Methods.OfName( "StaticGenericMethod" )
                    .Single()
                    .MakeGenericInstance( compilation.Factory.GetTypeByReflectionType( typeof(int) ) );

                var staticNonGenericMethod = nestedType.Methods.OfName( "StaticNonGenericMethod" ).Single();
                var staticField = nestedType.Fields.OfName( "StaticField" ).Single();
                var staticProperty = nestedType.Properties.OfName( "StaticProperty" ).Single();
                var staticEvent = nestedType.Events.OfName( "StaticEvent" ).Single();

                AssertEx.DynamicEquals(
                    staticGenericMethod.Invoke(),
                    @"global::TargetCode.Nested<global::System.String>.StaticGenericMethod<global::System.Int32>()" );

                AssertEx.DynamicEquals(
                    staticNonGenericMethod.Invoke(),
                    @"global::TargetCode.Nested<global::System.String>.StaticNonGenericMethod()" );

                AssertEx.DynamicEquals( staticField.Value, "global::TargetCode.Nested<global::System.String>.StaticField" );
                AssertEx.DynamicEquals( staticProperty.Value, "global::TargetCode.Nested<global::System.String>.StaticProperty" );
                AssertEx.DynamicEquals( staticEvent.Add( null ), "global::TargetCode.Nested<global::System.String>.StaticEvent += null" );

                // Testing instance members on a generic type.
                var instance = new TypedExpressionSyntaxImpl( SyntaxFactory.ParseExpression( "abc" ), syntaxSerializationContext.CompilationModel );

                var instanceGenericMethod = nestedType.Methods.OfName( "InstanceGenericMethod" )
                    .Single()
                    .MakeGenericInstance( compilation.Factory.GetTypeByReflectionType( typeof(int) ) );

                var instanceNonGenericMethod = nestedType.Methods.OfName( "InstanceNonGenericMethod" ).Single();
                var instanceField = nestedType.Fields.OfName( "InstanceField" ).Single();
                var instanceProperty = nestedType.Properties.OfName( "InstanceProperty" ).Single();
                var instanceEvent = nestedType.Events.OfName( "InstanceEvent" ).Single();

                AssertEx.DynamicEquals(
                    instanceGenericMethod.With( instance ).Invoke(),
                    @"((global::TargetCode.Nested<global::System.String>)abc).InstanceGenericMethod<global::System.Int32>()" );

                AssertEx.DynamicEquals(
                    instanceNonGenericMethod.With( instance ).Invoke(),
                    @"((global::TargetCode.Nested<global::System.String>)abc).InstanceNonGenericMethod()" );

                AssertEx.DynamicEquals(
                    instanceField.With( instance ).Value,
                    "((global::TargetCode.Nested<global::System.String>)abc).InstanceField" );

                AssertEx.DynamicEquals(
                    instanceProperty.With( instance ).Value,
                    "((global::TargetCode.Nested<global::System.String>)abc).InstanceProperty" );

                AssertEx.DynamicEquals(
                    instanceEvent.With( instance ).Add( null ),
                    "((global::TargetCode.Nested<global::System.String>)abc).InstanceEvent += null" );
            }
        }

        [Fact]
        public void Parameters()
        {
            const string code = @"
class TargetCode
{
    void Method( ref int i, ref long j )
    {
    }
}";

            using var testContext = this.CreateTestContext();
            var serviceProvider = testContext.ServiceProvider;

            var compilation = testContext.CreateCompilationModel( code );

            using ( TemplateExpansionContext.WithTestingContext(
                       new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted ),
                       serviceProvider ) )
            {
                var method = compilation.Types.Single().Methods.Single();

                var advisedParameterList = method.Parameters;

                // ReSharper disable once IDE0058
                AssertEx.DynamicEquals( advisedParameterList[0].Value, @"i" );

                // ReSharper disable once IDE0058
                AssertEx.DynamicEquals( advisedParameterList[1].Value, @"j" );

                Assert.Equal( advisedParameterList[0], advisedParameterList["i"] );
                Assert.Equal( advisedParameterList[1], advisedParameterList["j"] );

                Assert.Equal( "i", Assert.Single( advisedParameterList, t => t.Type.IsConvertibleTo( typeof(int) ) ).Name );
            }
        }

        [Fact]
        public void Properties()
        {
            const string code = @"
class TargetCode
{
    TargetCode P { get; set; }
    int this[int index] => 42;
}";

            using var testContext = this.CreateTestContext();
            var serviceProvider = testContext.ServiceProvider;

            var compilation = testContext.CreateCompilationModel( code );

            using ( TemplateExpansionContext.WithTestingContext(
                       new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted ),
                       serviceProvider ) )
            {
                var type = compilation.Types.Single();
                var property = type.Properties.OfName( "P" ).Single();

                AssertEx.DynamicEquals( property.Value, @"this.P" );

                AssertEx.DynamicEquals(
                    property.With( SyntaxFactory.IdentifierName( "a" ), InvokerOptions.NullConditional ).Value,
                    @"((global::TargetCode)a)?.P" );

                AssertEx.DynamicEquals(
                    ((FieldOrPropertyInvoker) property.With( SyntaxFactory.IdentifierName( "a" ) )).SetValue( SyntaxFactory.IdentifierName( "b" ) ),
                    @"((global::TargetCode)a).P = b" );
            }
        }

        [Fact]
        public void PropertyAccessors()
        {
            const string code = @"
class TargetCode
{
    TargetCode P { get; set; }
    int this[int index] => 42;
}";

            using var testContext = this.CreateTestContext();
            var serviceProvider = testContext.ServiceProvider;

            var compilation = testContext.CreateCompilationModel( code );

            using ( TemplateExpansionContext.WithTestingContext(
                       new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted ),
                       serviceProvider ) )
            {
                var type = compilation.Types.Single();
                var property = type.Properties.OfName( "P" ).Single();

                AssertEx.DynamicEquals( property.Value, @"this.P" );

                AssertEx.DynamicEquals(
                    property.GetMethod!.With( SyntaxFactory.IdentifierName( "a" ), InvokerOptions.NullConditional ).Invoke(),
                    @"((global::TargetCode)a)?.P" );
            }
        }

        [Fact]
        public void Events()
        {
            const string code = @"
class TargetCode
{
    event System.EventHandler MyEvent;
}";

            using var testContext = this.CreateTestContext();
            var serviceProvider = testContext.ServiceProvider;

            var compilation = testContext.CreateCompilationModel( code );

            var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

            using ( TemplateExpansionContext.WithTestingContext(
                       syntaxSerializationContext,
                       serviceProvider ) )
            {
                var type = compilation.Types.Single();
                var @event = type.Events.Single();

                TypedExpressionSyntaxImpl parameterExpression = new(
                    SyntaxFactory.IdentifierName( "value" ),
                    syntaxSerializationContext.CompilationModel );

                AssertEx.DynamicEquals( @event.Add( parameterExpression ), @"this.MyEvent += value" );
                AssertEx.DynamicEquals( @event.Remove( parameterExpression ), @"this.MyEvent -= value" );

#if NET5_0_OR_GREATER
                AssertEx.DynamicEquals(
                    @event.Raise( parameterExpression, parameterExpression ),
                    @"this.MyEvent?.Invoke((global::System.Object? )value, (global::System.EventArgs)value)" );
#else
                AssertEx.DynamicEquals(
                    @event.Raise( parameterExpression, parameterExpression ),
                    @"this.MyEvent?.Invoke((global::System.Object)value, (global::System.EventArgs)value)" );
#endif
            }
        }

        [Fact]
        public void EventAccessors()
        {
            const string code = @"
class TargetCode
{
    event System.EventHandler MyEvent;
}";

            using var testContext = this.CreateTestContext();
            var serviceProvider = testContext.ServiceProvider;

            var compilation = testContext.CreateCompilationModel( code );

            var syntaxSerializationContext = new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted );

            using ( TemplateExpansionContext.WithTestingContext(
                       syntaxSerializationContext,
                       serviceProvider ) )
            {
                var type = compilation.Types.Single();
                var @event = type.Events.Single();

                TypedExpressionSyntaxImpl parameterExpression = new(
                    SyntaxFactory.IdentifierName( "value" ),
                    syntaxSerializationContext.CompilationModel );

                AssertEx.DynamicEquals(
                    @event.AddMethod.Invoke( parameterExpression ),
                    @"this.MyEvent += value" );

                AssertEx.DynamicEquals(
                    @event.RemoveMethod.Invoke( parameterExpression ),
                    @"this.MyEvent -= value" );

#if NET5_0_OR_GREATER
                AssertEx.DynamicEquals(
                    @event.RaiseMethod?.Invoke( parameterExpression, parameterExpression ),
                    @"this.MyEvent?.Invoke((global::System.Object? )value, (global::System.EventArgs)value)" );
#else
                AssertEx.DynamicEquals(
                    @event.RaiseMethod?.Invoke( parameterExpression, parameterExpression ),
                    @"this.MyEvent?.Invoke((global::System.Object)value, (global::System.EventArgs)value)" );
#endif
            }
        }

        [Fact]
        public void ToArrayTest()
        {
            const string code = @"
class TargetCode
{
    void A( int a, string b, object c, out System.DateTime d, ref System.TimeSpan e)
    {
        d = default;
    }

    void B( int a, int b, int c, int d, int e, int f, int g, int h, int i, int j, int k, int l)
    {
    }

    void C()
    {
    }
}";

            using var testContext = this.CreateTestContext();
            var serviceProvider = testContext.ServiceProvider;

            var compilation = testContext.CreateCompilationModel( code );

            using ( TemplateExpansionContext.WithTestingContext(
                       new SyntaxSerializationContext( compilation, SyntaxGenerationOptions.Formatted ),
                       serviceProvider ) )
            {
                var type = compilation.Types.Single();
                var method = type.Methods.OfName( "A" ).Single();
                var longMethod = type.Methods.OfName( "B" ).Single();
                var noParameterMethod = type.Methods.OfName( "C" ).Single();

                AssertEx.DynamicEquals(
                    method.Parameters.ToValueArray(),
                    """
                    new object[]
                    {
                        a,
                        b,
                        c,
                        default(global::System.DateTime),
                        e
                    }
                    """ );

                AssertEx.DynamicEquals(
                    longMethod.Parameters.ToValueArray(),
                    """
                    new object[]
                    {
                        a,
                        b,
                        c,
                        d,
                        e,
                        f,
                        g,
                        h,
                        i,
                        j,
                        k,
                        l
                    }
                    """ );

                AssertEx.DynamicEquals(
                    noParameterMethod.Parameters.ToValueArray(),
                    """
                    new object[]
                    {
                    }
                    """ );
            }
        }
    }
}