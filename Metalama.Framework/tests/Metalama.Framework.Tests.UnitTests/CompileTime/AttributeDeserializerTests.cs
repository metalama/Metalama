// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Attribute = System.Attribute;

#pragma warning disable CA1018 // Mark attributes with AttributeUsageAttribute

// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable MemberCanBeInternal
// ReSharper disable UnusedType.Global
// Resharper disable MemberCanBePrivate.Global
// Resharper disable ClassNeverInstantiated.Global
// Resharper disable UnusedMember.Global
// Resharper disable UnusedMember.Local

namespace Metalama.Framework.Tests.UnitTests.CompileTime
{
    public sealed class AttributeDeserializerTests : UnitTestClass
    {
        protected override void ConfigureServices( IAdditionalServiceCollection services )
        {
            base.ConfigureServices( services );

            // services.AddProjectService<CompilationServiceProvider<CompileTimeTypeResolver>>( sp => new ProjectSpecificCompileTimeTypeResolver.Provider( sp ) );
            services.AddProjectService<SystemTypeResolver.Provider>( sp => new HackedSystemTypeResolver.Provider( sp ) );
        }

        private object? GetDeserializedProperty(
            string property,
            string value,
            string? dependentCode = null,
            string? additionalCode = "",
            string className = "TestAttribute" )
        {
            using var testContext = this.CreateTestContext();

            var code = $@"[assembly: Metalama.Framework.Tests.UnitTests.CompileTime.AttributeDeserializerTests.{className}( {property} = {value} )]"
                       + " enum RunTimeEnum { Value = 1}"
                       + " class GenericRunTimeType<T> {}"
                       + " struct GenericStruct {} "
                       + additionalCode;

            var compilation = testContext.CreateCompilationModel( code, dependentCode );

            var domain = testContext.Domain;

            var compileTimeProjectRepository = CompileTimeProjectRepository.Create(
                    domain,
                    testContext.ServiceProvider,
                    compilation.RoslynCompilation,
                    NullDiagnosticAdder.Instance )
                .AssertNotNull();

            var attribute = compilation.Attributes.Single();
            ThrowingDiagnosticAdder diagnosticBag = new();

            if ( !compileTimeProjectRepository.CreateAttributeDeserializer( testContext.ServiceProvider, compilation.CompilationContext )
                    .TryCreateAttribute( attribute, diagnosticBag, out var deserializedAttribute ) )
            {
                throw new AssertionFailedException();
            }

            return deserializedAttribute.GetType().GetProperty( property ).AssertNotNull().GetValue( deserializedAttribute );
        }

        [Fact]
        public void TestPrimitiveTypes()
        {
            Assert.Equal( 5, this.GetDeserializedProperty( nameof(TestAttribute.Int32Property), "5" ) );
            Assert.Equal( "Zuzana", this.GetDeserializedProperty( nameof(TestAttribute.StringProperty), "\"Zuzana\"" ) );
            Assert.Equal( new[] { 5 }, this.GetDeserializedProperty( nameof(TestAttribute.Int32ArrayProperty), "new[]{5}" ) );
        }

        [Fact]
        public void TestInitOnlyProperty()
        {
            Assert.Equal( 5, this.GetDeserializedProperty( nameof(TestAttribute.InitOnlyProperty), "5" ) );
        }

        [Fact]
        public void TestInitOnlyPropertyOfDerivedClass()
        {
            Assert.Equal( 5, this.GetDeserializedProperty( nameof(TestAttribute.InitOnlyProperty), "5", nameof(DerivedAttribute) ) );
        }

        [Fact]
        public void TestField()
        {
            using var testContext = this.CreateTestContext();

            const string code = $@"[assembly: Metalama.Framework.Tests.UnitTests.CompileTime.AttributeDeserializerTests.TestAttribute( Field = 5 )]";
            var compilation = testContext.CreateCompilationModel( code );

            var domain = testContext.Domain;

            var compileTimeProjectRepository =
                CompileTimeProjectRepository.Create( domain, testContext.ServiceProvider, compilation.RoslynCompilation ).AssertNotNull();

            var attribute = compilation.Attributes.Single();
            DiagnosticBag diagnosticBag = new();

            if ( !compileTimeProjectRepository.CreateAttributeDeserializer( testContext.ServiceProvider, compilation.CompilationContext )
                    .TryCreateAttribute( attribute, diagnosticBag, out var deserializedAttribute ) )
            {
                throw new AssertionFailedException( string.Join( " ", diagnosticBag ) );
            }

            var value = deserializedAttribute.GetType().GetField( "Field" ).AssertNotNull().GetValue( deserializedAttribute );
            Assert.Equal( 5, value );
        }

        [Fact]
        public void TestBoxedTypes()
        {
            Assert.Equal( 5, this.GetDeserializedProperty( nameof(TestAttribute.ObjectProperty), "5" ) );
            Assert.Equal( "Zuzana", this.GetDeserializedProperty( nameof(TestAttribute.ObjectProperty), "\"Zuzana\"" ) );
            Assert.Equal( new[] { 5 }, this.GetDeserializedProperty( nameof(TestAttribute.ObjectProperty), "new[]{5}" ) );
        }

        [Fact]
        public void TestEnums()
        {
            Assert.Equal(
                TestEnum.A,
                this.GetDeserializedProperty(
                    nameof(TestAttribute.EnumProperty),
                    "Metalama.Framework.Tests.UnitTests.CompileTime.AttributeDeserializerTests.TestEnum.A" ) );

            Assert.Equal(
                TestEnum.A,
                this.GetDeserializedProperty(
                    nameof(TestAttribute.ObjectProperty),
                    "Metalama.Framework.Tests.UnitTests.CompileTime.AttributeDeserializerTests.TestEnum.A" ) );

            Assert.Equal(
                new[] { TestEnum.A },
                this.GetDeserializedProperty(
                    nameof(TestAttribute.EnumArrayProperty),
                    "new[]{Metalama.Framework.Tests.UnitTests.CompileTime.AttributeDeserializerTests.TestEnum.A}" ) );
        }

        [Fact]
        public void TestTypes()
        {
            Assert.Equal( typeof(int), this.GetDeserializedProperty( nameof(TestAttribute.TypeProperty), "typeof(int)" ) );

            Assert.Equal(
                typeof(TestEnum),
                this.GetDeserializedProperty(
                    nameof(TestAttribute.TypeProperty),
                    "typeof(Metalama.Framework.Tests.UnitTests.CompileTime.AttributeDeserializerTests.TestEnum)" ) );

            Assert.Null( this.GetDeserializedProperty( nameof(TestAttribute.TypeProperty), "null" ) );

            Assert.Equal(
                new[] { typeof(TestEnum), typeof(int) },
                this.GetDeserializedProperty(
                    nameof(TestAttribute.TypeArrayProperty),
                    "new[]{typeof(Metalama.Framework.Tests.UnitTests.CompileTime.AttributeDeserializerTests.TestEnum),typeof(int)}" ) );

            Assert.Equal(
                new[] { typeof(TestEnum), typeof(int) },
                this.GetDeserializedProperty(
                    nameof(TestAttribute.ObjectArrayProperty),
                    "new[]{typeof(Metalama.Framework.Tests.UnitTests.CompileTime.AttributeDeserializerTests.TestEnum),typeof(int)}" ) );

            Assert.Null( this.GetDeserializedProperty( nameof(TestAttribute.ObjectArrayProperty), "null" ) );

            Assert.Equal(
                new[] { typeof(TestEnum), null },
                this.GetDeserializedProperty(
                    nameof(TestAttribute.ObjectArrayProperty),
                    "new[]{typeof(Metalama.Framework.Tests.UnitTests.CompileTime.AttributeDeserializerTests.TestEnum),null}" ) );
        }

        [Fact]
        public void TestRunTimeTypes()
        {
            Assert.IsType<CompileTimeType>(
                this.GetDeserializedProperty(
                    nameof(TestAttribute.TypeProperty),
                    "typeof(RunTimeEnum)" ) );

            Assert.IsType<CompileTimeType>(
                this.GetDeserializedProperty(
                    nameof(TestAttribute.TypeProperty),
                    "typeof(RunTimeEnum[])" ) );

            Assert.IsType<CompileTimeType>(
                this.GetDeserializedProperty(
                    nameof(TestAttribute.TypeProperty),
                    "typeof(System.Collections.Generic.List<RunTimeEnum>)" ) );

            Assert.IsType<CompileTimeType>(
                this.GetDeserializedProperty(
                    nameof(TestAttribute.TypeProperty),
                    "typeof(GenericRunTimeType<int>)" ) );

            Assert.IsType<CompileTimeType>(
                this.GetDeserializedProperty(
                    nameof(TestAttribute.TypeProperty),
                    "typeof(GenericStruct*)" ) );

            const string dependentCode = "public class MyExternClass {} public enum MyExternEnum { A, B }";
            var typeValue = this.GetDeserializedProperty( nameof(TestAttribute.TypeProperty), "typeof(MyExternClass)", dependentCode );
            Assert.Equal( "MyExternClass", Assert.IsType<CompileTimeType>( typeValue ).FullName );

            // When assigning to a run-time-only enum, the enum primitive value is used. 
            var objectValue = this.GetDeserializedProperty( nameof(TestAttribute.ObjectProperty), "MyExternEnum.B", dependentCode );
            Assert.Equal( 1, objectValue );
        }

        [Fact]
        public void TestArrayType()
        {
            Assert.Equal(
                typeof(int[]),
                this.GetDeserializedProperty(
                    nameof(TestAttribute.TypeProperty),
                    "typeof(int[])" ) );

            Assert.Equal(
                typeof(int[,]),
                this.GetDeserializedProperty(
                    nameof(TestAttribute.TypeProperty),
                    "typeof(int[,])" ) );
        }

        [Fact]
        public void TestPointerType()
        {
            Assert.Equal(
                typeof(int*),
                this.GetDeserializedProperty(
                    nameof(TestAttribute.TypeProperty),
                    "typeof(int*)" ) );

            Assert.Equal(
                typeof(int*[]),
                this.GetDeserializedProperty(
                    nameof(TestAttribute.TypeProperty),
                    "typeof(int*[])" ) );
        }

        [Fact]
        public void TestGenericType()
        {
            Assert.Equal(
                typeof(List<int>),
                this.GetDeserializedProperty(
                    nameof(TestAttribute.TypeProperty),
                    "typeof(System.Collections.Generic.List<int>)" ) );

            Assert.Equal(
                typeof(List<>),
                this.GetDeserializedProperty(
                    nameof(TestAttribute.TypeProperty),
                    "typeof(System.Collections.Generic.List<>)" ) );

            Assert.Equal(
                typeof(List<List<int>>),
                this.GetDeserializedProperty(
                    nameof(TestAttribute.TypeProperty),
                    "typeof(System.Collections.Generic.List<System.Collections.Generic.List<int>>)" ) );

            Assert.Equal(
                typeof(List<int[]>),
                this.GetDeserializedProperty(
                    nameof(TestAttribute.TypeProperty),
                    "typeof(System.Collections.Generic.List<int[]>)" ) );
        }

        [Fact]
        public void TestParams()
        {
            object Deserialize( string args )
            {
                using var testContext = this.CreateTestContext();

                var code = $@"[assembly: Metalama.Framework.Tests.UnitTests.CompileTime.AttributeDeserializerTests.TestParamsAttribute( {args} )]";
                var compilation = testContext.CreateCompilationModel( code );

                var domain = testContext.Domain;

                var compileTimeProjectRepository = CompileTimeProjectRepository.Create( domain, testContext.ServiceProvider, compilation.RoslynCompilation )
                    .AssertNotNull();

                var attribute = compilation.Attributes.Single();

                var diagnosticBag = new DiagnosticBag();

                if ( !compileTimeProjectRepository.CreateAttributeDeserializer( testContext.ServiceProvider, compilation.CompilationContext )
                        .TryCreateAttribute( attribute, diagnosticBag, out var deserializedAttribute ) )
                {
                    throw new AssertionFailedException( string.Join( " ", diagnosticBag ) );
                }

                return ((TestParamsAttribute) deserializedAttribute).Value;
            }

            Assert.Equal( new[] { "a", "b" }, Deserialize( "\"a\", \"b\"" ) );
            Assert.Equal( new[] { "a" }, Deserialize( "\"a\"" ) );
            Assert.Equal( new[] { 1, 2 }, Deserialize( "1, 2" ) );
            Assert.Equal( new[] { 1 }, Deserialize( "1" ) );
            Assert.Equal( new[] { typeof(int), typeof(string) }, Deserialize( "typeof(int), typeof(string)" ) );
            Assert.Equal( new[] { typeof(int) }, Deserialize( "typeof(int)" ) );
        }

        [Fact]
        public void TestParams2()
        {
            void Test( string args, object firstValue, object otherValues )
            {
                using var testContext = this.CreateTestContext();

                var code = $@"[assembly: Metalama.Framework.Tests.UnitTests.CompileTime.AttributeDeserializerTests.TestParams2Attribute( {args} )]";
                var compilation = testContext.CreateCompilationModel( code );

                var domain = testContext.Domain;

                var compileTimeProjectRepository = CompileTimeProjectRepository.Create( domain, testContext.ServiceProvider, compilation.RoslynCompilation )
                    .AssertNotNull();

                var attribute = compilation.Attributes.Single();

                var diagnosticBag = new DiagnosticBag();

                if ( !compileTimeProjectRepository.CreateAttributeDeserializer( testContext.ServiceProvider, compilation.CompilationContext )
                        .TryCreateAttribute( attribute, diagnosticBag, out var deserializedAttribute ) )
                {
                    throw new AssertionFailedException( string.Join( " ", diagnosticBag ) );
                }

                Assert.Equal( firstValue, ((TestParams2Attribute) deserializedAttribute).FirstValue );
                Assert.Equal( otherValues, ((TestParams2Attribute) deserializedAttribute).OtherValues );
            }

            Test( "\"a\", \"b\"", "a", new[] { "b" } );
            Test( "\"a\"", "a", Array.Empty<string>() );
            Test( "1, 2", 1, new[] { 2 } );
            Test( "1", 1, Array.Empty<int>() );
            Test( "typeof(int), typeof(string)", typeof(int), new[] { typeof(string) } );
            Test( "typeof(int)", typeof(int), Type.EmptyTypes );
        }

        [Fact]
        public void AttributesOfUnknownTypeAreIgnored()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"[assembly: UnknownType] [UnknownType] class C {}";
            var compilation = testContext.CreateCompilationModel( code, ignoreErrors: true );

            Assert.Empty( compilation.Attributes );
            Assert.Empty( compilation.Types.Single().Attributes );
        }

        [Fact]
        public void PropertiesOfInvalidNameAreIgnored()
        {
            using var testContext = this.CreateTestContext();

            const string code = $@"[assembly: Metalama.Framework.Tests.UnitTests.CompileTime.AttributeDeserializerTests.TestAttribute( InvalidProperty = 0 )]";
            var compilation = testContext.CreateCompilationModel( code, ignoreErrors: true );

            var domain = testContext.Domain;

            var compileTimeProjectRepository =
                CompileTimeProjectRepository.Create( domain, testContext.ServiceProvider, compilation.RoslynCompilation ).AssertNotNull();

            var attribute = compilation.Attributes.Single();
            DiagnosticBag diagnosticBag = new();

            Assert.True(
                compileTimeProjectRepository.CreateAttributeDeserializer( testContext.ServiceProvider, compilation.CompilationContext )
                    .TryCreateAttribute( attribute, diagnosticBag, out _ ) );

            Assert.Empty( diagnosticBag );
        }

        [Fact]
        public void AttributesWithMissingConstructorAreIgnored()
        {
            using var testContext = this.CreateTestContext();

            const string code = $@"[assembly: Metalama.Framework.Tests.UnitTests.CompileTime.AttributeDeserializerTests.TestAttribute( 0 )] "
                                + "[Metalama.Framework.Tests.UnitTests.CompileTime.AttributeDeserializerTests.TestAttribute( 0 )] class C {}";

            var compilation = testContext.CreateCompilationModel( code, ignoreErrors: true );

            Assert.Empty( compilation.Attributes );
            Assert.Empty( compilation.Types.Single().Attributes );
        }

        [Fact]
        public void ThrowingConstructorFailsSafely()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"[assembly: Metalama.Framework.Tests.UnitTests.CompileTime.AttributeDeserializerTests.ThrowingAttribute( true )]";
            var compilation = testContext.CreateCompilationModel( code );
            var attribute = compilation.Attributes.Single();

            var domain = testContext.Domain;

            var compileTimeProjectRepository =
                CompileTimeProjectRepository.Create( domain, testContext.ServiceProvider, compilation.RoslynCompilation ).AssertNotNull();

            DiagnosticBag diagnosticBag = new();

            Assert.False(
                compileTimeProjectRepository.CreateAttributeDeserializer( testContext.ServiceProvider, compilation.CompilationContext )
                    .TryCreateAttribute( attribute, diagnosticBag, out var deserializedAttribute ) );

            Assert.Contains( diagnosticBag, d => d.Id == GeneralDiagnosticDescriptors.ExceptionInUserCode.Id );
            Assert.Null( deserializedAttribute );
        }

        [Fact]
        public void ThrowingPropertyFailsSafely()
        {
            using var testContext = this.CreateTestContext();

            const string code =
                @"[assembly: Metalama.Framework.Tests.UnitTests.CompileTime.AttributeDeserializerTests.ThrowingAttribute( false, Property = 0 )]";

            var compilation = testContext.CreateCompilationModel( code );
            var attribute = compilation.Attributes.Single();

            var domain = testContext.Domain;

            var compileTimeProjectRepository =
                CompileTimeProjectRepository.Create( domain, testContext.ServiceProvider, compilation.RoslynCompilation ).AssertNotNull();

            DiagnosticBag diagnosticBag = new();

            Assert.False(
                compileTimeProjectRepository.CreateAttributeDeserializer( testContext.ServiceProvider, compilation.CompilationContext )
                    .TryCreateAttribute( attribute, diagnosticBag, out var deserializedAttribute ) );

            Assert.Contains( diagnosticBag, d => d.Id == GeneralDiagnosticDescriptors.ExceptionInUserCode.Id );
            Assert.Null( deserializedAttribute );
        }

        [Fact]
        public void RunTimeOnlyAttributeType()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"[assembly: MyAttribute] class MyAttribute : System.Attribute {}";

            var compilation = testContext.CreateCompilationModel( code );
            var attribute = compilation.Attributes.Single();

            var domain = testContext.Domain;

            var compileTimeProjectRepository =
                CompileTimeProjectRepository.Create( domain, testContext.ServiceProvider, compilation.RoslynCompilation ).AssertNotNull();

            DiagnosticBag diagnosticBag = new();

            Assert.False(
                compileTimeProjectRepository.CreateAttributeDeserializer( testContext.ServiceProvider, compilation.CompilationContext )
                    .TryCreateAttribute( attribute, diagnosticBag, out _ ) );

            Assert.Contains( diagnosticBag, d => d.Id == AttributeDeserializerDiagnostics.CannotFindAttributeType.Id );
        }

        [Fact]
        public void GenericAttribute()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"[assembly: Metalama.Framework.Tests.UnitTests.CompileTime.AttributeDeserializerTests.GenericTestAttribute<string>( ""x"" )]";

            var compilation = testContext.CreateCompilationModel( code );
            var attribute = compilation.Attributes.Single();
            Assert.Equal( "x", attribute.ConstructorArguments[0].Value );
        }

        [Fact]
        public void GenericAttribute_Params()
        {
            using var testContext = this.CreateTestContext();

            const string code =
                @"[assembly: Metalama.Framework.Tests.UnitTests.CompileTime.AttributeDeserializerTests.GenericTestAttribute<string>( ""x"", ""y"" )]";

            var compilation = testContext.CreateCompilationModel( code );
            var attribute = compilation.Attributes.Single();
            Assert.Equal( "x", attribute.ConstructorArguments[0].Values[0].Value );
            Assert.Equal( "y", attribute.ConstructorArguments[0].Values[1].Value );
        }

        [Fact]
        public void GenericAttribute_Property()
        {
            using var testContext = this.CreateTestContext();

            const string code =
                @"[assembly: Metalama.Framework.Tests.UnitTests.CompileTime.AttributeDeserializerTests.GenericTestAttribute<string>( Property = ""x"" )]";

            var compilation = testContext.CreateCompilationModel( code );
            var attribute = compilation.Attributes.Single();
            Assert.Equal( "x", attribute.NamedArguments["Property"].Value );
        }

        // ReSharper disable UnusedParameter.Local
#pragma warning disable SA1401
#pragma warning disable CA1822

        public enum TestEnum
        {
            A,
            B
        }

        public class TestAttribute : Attribute
        {
            public TestAttribute() { }

            public TestAttribute( string s ) { }

            public int Int32Property { get; set; }

            public string? StringProperty { get; set; }

            public object? ObjectProperty { get; set; }

            public TestEnum EnumProperty { get; set; }

            public Type? TypeProperty { get; set; }

            public int[]? Int32ArrayProperty { get; set; }

            public object[]? ObjectArrayProperty { get; set; }

            public Type[]? TypeArrayProperty { get; set; }

            public TestEnum[]? EnumArrayProperty { get; set; }

            public int Field;

            public int InitOnlyProperty { get; init; }
        }

        public class DerivedAttribute : TestAttribute;

        public class TestParamsAttribute : Attribute
        {
            public object Value { get; }

            public TestParamsAttribute( params string[] p ) { this.Value = p; }

            public TestParamsAttribute( params int[] p ) { this.Value = p; }

            public TestParamsAttribute( params Type[] p ) { this.Value = p; }

            public TestParamsAttribute( params object[] p ) { this.Value = p; }
        }

        public class TestParams2Attribute : Attribute
        {
            public object FirstValue { get; }

            public object OtherValues { get; }

            public TestParams2Attribute( string a, params string[] p )
            {
                this.FirstValue = a;
                this.OtherValues = p;
            }

            public TestParams2Attribute( int a, params int[] p )
            {
                this.FirstValue = a;
                this.OtherValues = p;
            }

            public TestParams2Attribute( Type a, params Type[] p )
            {
                this.FirstValue = a;
                this.OtherValues = p;
            }

            public TestParams2Attribute( object a, params object[] p )
            {
                this.FirstValue = a;
                this.OtherValues = p;
            }
        }

        public class ThrowingAttribute : Attribute
        {
            // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
            public ThrowingAttribute( bool throws )
            {
                if ( throws )
                {
                    throw new InvalidOperationException();
                }
            }

            public int Property
            {
                get => throw new InvalidOperationException();
                set => throw new InvalidOperationException();
            }
        }

        public class GenericTestAttribute<T> : Attribute
            where T : class
        {
            public GenericTestAttribute() { }

            public GenericTestAttribute( T value ) { }

            public GenericTestAttribute( params T[] values ) { }

            public T? Property { get; set; }
        }

        private sealed class HackedSystemTypeResolver : SystemTypeResolver
        {
            // We provide a non-standard CompileTimeTypeFactory to break a conflict in the initialization of dependencies.
            // Another CompileTimeTypeFactory instance is created by the ServiceProviderFactory. It should not matter for this test.

            private HackedSystemTypeResolver( in ProjectServiceProvider serviceProvider, CompilationContext compilationContext ) : base(
                serviceProvider,
                compilationContext ) { }

            protected override bool IsSupportedAssembly( string assemblyName )
                => base.IsSupportedAssembly( assemblyName ) || assemblyName == this.GetType().Assembly.GetName().Name;

            public new sealed class Provider : SystemTypeResolver.Provider
            {
                public Provider( in ProjectServiceProvider serviceProvider ) : base( in serviceProvider ) { }

                protected override SystemTypeResolver Create( CompilationContext compilationContext )
                {
                    return new HackedSystemTypeResolver( this.ServiceProvider, compilationContext );
                }
            }
        }
    }
}