// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Tests.UnitTests.Utilities;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;
using static Metalama.Framework.Code.MethodKind;
using static Metalama.Framework.Code.RefKind;
using static Metalama.Framework.Code.TypeKind;
using SpecialType = Metalama.Framework.Code.SpecialType;
using TypeKind = Metalama.Framework.Code.TypeKind;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace Metalama.Framework.Tests.UnitTests.CodeModel
{
    public sealed partial class CodeModelTests : UnitTestClass
    {
        [Fact]
        public void ObjectIdentity()
        {
            using var testContext = this.CreateTestContext();

            // This basically tests that [Memo] works.

            const string code = "";
            var compilation = testContext.CreateCompilationModel( code );

            var types1 = compilation.Types;
            var types2 = compilation.Types;

            Assert.Same( types1, types2 );
        }

        [Fact]
        public void TypeInfos()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class C
{
    class D { }
}

namespace NS
{
    class C {}
}

class E<T> {}

";

            var compilation = testContext.CreateCompilationModel( code );

            var types = compilation.Types.OrderBySource();
            Assert.Equal( 3, types.Length );

            var c1 = types.ElementAt( 0 );
            Assert.Equal( "C", c1.Name );
            Assert.Equal( "C", c1.FullName );
            Assert.IsAssignableFrom<ICompilation>( c1.ContainingDeclaration );

            var d = c1.Types.Single();
            Assert.Equal( "D", d.Name );
            Assert.Equal( "C.D", d.FullName );
            Assert.Same( c1, d.ContainingDeclaration );

            var c2 = types.ElementAt( 1 );
            Assert.Equal( "C", c2.Name );
            Assert.Equal( "NS.C", c2.FullName );
            Assert.IsAssignableFrom<ICompilation>( c2.ContainingDeclaration );

            var e = types.ElementAt( 2 );
            Assert.Equal( "E", e.Name );
            Assert.Equal( "E", e.FullName );
        }

        [Fact]
        public void LocalFunctions()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class C
{
    void M()
    {
        void Outer()
        {
            void Inner() {}
        }
    }
}";

            var compilation = testContext.CreateCompilationModel( code );

            var type = compilation.Types.Single();
            Assert.Equal( "C", type.Name );

            var methods = type.Methods;

            Assert.Single( methods );

            var method = methods.ElementAt( 0 );
            Assert.Equal( "M", method.Name );
            Assert.Same( type, method.ContainingDeclaration );
        }

        [Fact]
        public void Attributes()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
using System;

enum E
{
    F, G
}

[Test(42, ""foo"", null, E = E.G, Types = new[] { typeof(E), typeof(Action<,>), null, typeof(Action<E>), typeof(E*) })]
class TestAttribute : Attribute
{
    public TestAttribute(int i, string s, object o) {}

    public E E { get; set; }
    public Type[] Types { get; set; }
}";

            var compilation = testContext.CreateCompilationModel( code );

            var attribute = compilation.Types.OrderBySource().ElementAt( 1 ).Attributes.Single();
            Assert.Equal( "TestAttribute", attribute.Type.FullName );
            Assert.Equal( new object?[] { 42, "foo", null }, attribute.ConstructorArguments.Select( a => a.Value ) );
            var namedArguments = attribute.NamedArguments;
            Assert.Equal( 2, namedArguments.Count );
            Assert.Equal( 1, attribute.GetNamedArgumentValue( "E" ) );
            var types = Assert.IsAssignableFrom<object[]>( attribute.GetNamedArgumentValue( "Types" ) );
            Assert.Equal( 5, types.Length );
            var type0 = Assert.IsAssignableFrom<INamedType>( types.ElementAt( 0 ) );
            Assert.Equal( "E", type0.FullName );
            var type1 = Assert.IsAssignableFrom<INamedType>( types.ElementAt( 1 ) );
            Assert.Equal( "System.Action", type1.FullName );
            Assert.Null( types.ElementAt( 2 ) );
        }

        [Fact]
        public void InvalidAttributes()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
using System;

[Test(typeof(ErrorType))]
class TestAttribute : Attribute
{
    public TestAttribute( params Type[] types ) {}
}";

            var compilation = testContext.CreateCompilationModel( code, ignoreErrors: true );
            var type = compilation.Types.Single();
            Assert.Empty( type.Attributes );
        }

        [Fact]
        public void Parameters()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
using System;

interface I<T>
{
    void M1(Int32 i, T t, dynamic d, in object o, out String s);
    ref readonly int M2();
}";

            var compilation = testContext.CreateCompilationModel( code );

            var methods = compilation.Types.Single().Methods.ToReadOnlyList();
            Assert.Equal( 2, methods.Count );

            var m1 = methods.ElementAt( 0 );
            Assert.Equal( "M1", m1.Name );

            Assert.True( m1.ReturnParameter.IsReturnParameter );
            Assert.False( m1.Parameters.ElementAt( 0 ).IsReturnParameter );

            Assert.Equal( 0, m1.Parameters.OfParameterType( typeof(int) ).First().Index );
            Assert.Equal( 0, m1.Parameters.OfParameterType<int>().First().Index );
            Assert.Equal( 0, m1.Parameters.OfParameterType( compilation.Factory.GetSpecialType( SpecialType.Int32 ) ).First().Index );

            CheckParameterData( m1.ReturnParameter, m1, "void", "<return>", -1 );
            Assert.Equal( 5, m1.Parameters.Count );
            CheckParameterData( m1.Parameters.ElementAt( 0 ), m1, "int", "i", 0 );
            CheckParameterData( m1.Parameters.ElementAt( 1 ), m1, "I<T>/T", "t", 1 );
            CheckParameterData( m1.Parameters.ElementAt( 2 ), m1, "dynamic", "d", 2 );
            CheckParameterData( m1.Parameters.ElementAt( 3 ), m1, "object", "o", 3 );
            CheckParameterData( m1.Parameters.ElementAt( 4 ), m1, "string", "s", 4 );

            var m2 = methods.ElementAt( 1 );
            Assert.Equal( "M2", m2.Name );

            CheckParameterData( m2.ReturnParameter, m2, "int", "<return>", -1 );
            Assert.Empty( m2.Parameters );

            static void CheckParameterData( IParameter parameter, IDeclaration containingDeclaration, string typeName, string name, int index )
            {
                Assert.Same( containingDeclaration, parameter.ContainingDeclaration );
                Assert.Equal( typeName, parameter.Type.ToString() );
                Assert.Equal( name, parameter.Name );
                Assert.Equal( index, parameter.Index );
            }
        }

        [Fact]
        public void GenericArguments()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class C<T1, T2>
{
    static C<int, string> GetInstance() => null;
}";

            var compilation = testContext.CreateCompilationModel( code );

            var type = compilation.Types.Single();

            Assert.Equal( new[] { "C<T1, T2>/T1", "C<T1, T2>/T2" }, type.TypeArguments.SelectAsImmutableArray( t => t.ToString().AssertNotNull() ) );

            // Check that it can be accessed by index.
            Assert.Equal( "C<T1, T2>/T1", type.TypeArguments[0].ToString() );

            var method = type.Methods.First();

            Assert.Equal( "C<int, string>", method.ReturnType.ToString() );
            Assert.Equal( new[] { "int", "string" }, ((INamedType) method.ReturnType).TypeArguments.SelectAsImmutableArray( t => t.ToString() ) );
        }

        [Fact]
        public void GlobalAttributes()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
using System;

[module: MyAttribute(""m"")]
[assembly: MyAttribute(""a"")]

class MyAttribute : Attribute
{
    public MyAttribute(string target) {}
}
";

            var compilation = testContext.CreateCompilationModel( code );

            var attributes = compilation.Attributes.ToArray();

            Assert.Equal( 2, attributes.Length );

            Assert.Equal( "MyAttribute", attributes.ElementAt( 0 ).Type.FullName );
            Assert.Equal( "a", Assert.Single( attributes.ElementAt( 0 ).ConstructorArguments.Select( a => a.Value ) ) );

            Assert.Equal( "MyAttribute", attributes.ElementAt( 1 ).Type.FullName );
            Assert.Equal( "m", Assert.Single( attributes.ElementAt( 1 ).ConstructorArguments.Select( a => a.Value ) ) );
        }

        [Fact]
        public void AttributeOnReturnValue()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
using System;

class C 
{
   [return: MyAttribute]
   void M() {}
}

class MyAttribute : Attribute
{
    public MyAttribute() {}
}
";

            var compilation = testContext.CreateCompilationModel( code );
            Assert.Single( compilation.Types.OfName( "C" ).Single().Methods.Single().ReturnParameter.Attributes );
        }

        [Fact]
        public void Arrays()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class C
{
    void M(int[] i) {}
}
";

            var compilation = testContext.CreateCompilationModel( code );

            var parameterTypes = from type in compilation.Types
                                 from method in type.Methods
                                 from parameter in method.Parameters
                                 select parameter.Type;

            var parameterType = Assert.Single( parameterTypes )!;

            Assert.Equal( "int[]", parameterType.ToString() );
            Assert.True( parameterType.IsConvertibleTo( typeof(int[]) ) );
            Assert.False( parameterType.IsConvertibleTo( typeof(int[,]) ) );

            var arrayType = Assert.IsAssignableFrom<IArrayType>( parameterType );

            Assert.Equal( "int", arrayType.ElementType.ToString() );
            Assert.True( arrayType.ElementType.IsConvertibleTo( typeof(int) ) );
            Assert.Equal( 1, arrayType.Rank );
        }

        [Fact]
        public void Properties()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class C
{
    int Auto { get; set; }
    int GetOnly { get; }
    int ReadWrite { get => 0; set {} }
    int ReadOnly { get => 0; }
    int WriteOnly { set {} }
    int field;
}";

            var compilation = testContext.CreateCompilationModel( code );

            var type = Assert.Single( compilation.Types );

            var propertyNames = type.Properties.SelectAsImmutableArray( p => p.Name );

            Assert.Equal( new[] { "Auto", "GetOnly", "ReadWrite", "ReadOnly", "WriteOnly" }, propertyNames );
        }

        [Fact]
        public void InvalidProperty()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
public sealed class C
{
    public InvalidType Property {get;set;}
}
";

            var compilation = testContext.CreateCompilationModel( code, ignoreErrors: true );
            Assert.Single( compilation.Types );
            Assert.Empty( compilation.Types.ElementAt( 0 ).Properties );
        }

        [Fact]
        public void Fields()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class C
{
    int a = 1, b;
    int c;    
    int AutoProperty { get; set; }
    event Handler EventField;

    delegate void Handler();
}";

            var compilation = testContext.CreateCompilationModel( code );

            var type = Assert.Single( compilation.Types );

            var fieldNames = type.Fields.Where( f => !f.IsImplicitlyDeclared ).Select( p => p.Name );

            Assert.Equal( new[] { "a", "b", "c" }, fieldNames );
        }

        [Fact]
        public void InvalidField()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
public sealed class C
{
   InvalidType field;
}
";

            var compilation = testContext.CreateCompilationModel( code, ignoreErrors: true );
            Assert.Single( compilation.Types );
            Assert.Empty( compilation.Types.ElementAt( 0 ).Fields );
        }

        [Fact]
        public void RefProperties()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class C
{
    int field;

    int None { get; set; }
    ref int Ref => ref field;
    ref readonly int RefReadonly => ref field;
}";

            var compilation = testContext.CreateCompilationModel( code );

            var type = Assert.Single( compilation.Types );

            var refKinds = type.Properties.SelectAsImmutableArray( p => p.RefKind );

            Assert.Equal( new[] { None, Ref, RefReadOnly }, refKinds );
        }

        [Fact]
        public void Events()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class C
{
    event Handler Event
    {
        add {}
        remove {}
    }

    event Handler EventField;

    delegate void Handler();
}";

            var compilation = testContext.CreateCompilationModel( code );

            var type = Assert.Single( compilation.Types );

            var eventNames = type.Events.SelectAsImmutableArray( p => p.Name );

            Assert.Equal( new[] { "Event", "EventField" }, eventNames );
        }

        [Fact]
        public void MethodKinds()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
using System;
class C : IDisposable
{
	void M()
	{
		void L() { }
	}
	C() { }
	static C() { }
	~C() { }
	int P { get; set; }
	event EventHandler E { add {} remove {} }
	void IDisposable.Dispose() { }
	public static explicit operator int(C c) => 42;
	public static C operator -(C c) => c;
}";

            var compilation = testContext.CreateCompilationModel( code );

            var type = Assert.Single( compilation.Types );

            Assert.Equal( new[] { Default, ExplicitInterfaceImplementation, Operator, Operator }, type.Methods.SelectAsImmutableArray( m => m.MethodKind ) );

            Assert.Equal( Finalizer, type.Finalizer?.MethodKind );

            Assert.Equal( new[] { PropertyGet, PropertySet }, type.Properties.SelectMany( p => new[] { p.GetMethod!.MethodKind, p.SetMethod!.MethodKind } ) );
            Assert.Equal( new[] { EventAdd, EventRemove }, type.Events.SelectMany( p => new[] { p.AddMethod.MethodKind, p.RemoveMethod.MethodKind } ) );
            Assert.Single( type.Constructors );
            Assert.NotNull( type.StaticConstructor );
        }

        [Fact]
        public void DefaultConstructors()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"class C {}
";

            var compilation = testContext.CreateCompilationModel( code );

            var type = Assert.Single( compilation.Types );
            var constructor = type.Constructors.Single();
            Assert.True( constructor.IsImplicitlyDeclared );
            Assert.Null( type.StaticConstructor );
        }

        [Fact]
        public void InvalidMethods()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
public sealed class C
{
    public InvalidType M1() {}
    public void M2(InvalidType m) {}
    public void M3(InvalidType[] m) {}
    public void M4(System.List<InvalidType> m) {}
}
";

            var compilation = testContext.CreateCompilationModel( code, ignoreErrors: true );
            Assert.Single( compilation.Types );
            Assert.Empty( compilation.Types.ElementAt( 0 ).Methods );
        }

        [Fact]
        public void TypeKinds()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
using System;
class C<T>
{
    int[] arr;
    C<T> c;
    Action a;
    dynamic d;
    DayOfWeek e;
    T t;
    IDisposable i;
    unsafe void* p;
    int s;
}";

            var compilation = testContext.CreateCompilationModel( code );

            var type = Assert.Single( compilation.Types );

            var typeKinds = new[] { TypeKind.Array, Class, TypeKind.Delegate, Dynamic, TypeKind.Enum, TypeParameter, Interface, Pointer, Struct };

            Assert.Equal( typeKinds, type.Fields.SelectAsImmutableArray( p => p.Type.TypeKind ) );
        }

        [Fact]
        public void DelegateType()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"delegate void D();";

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.OfName( "D" ).Single();
            Assert.Equal( TypeKind.Delegate, type.TypeKind );

            foreach ( var method in type.Methods )
            {
                Assert.True( method.IsImplicitlyDeclared );
            }

            foreach ( var constructor in type.Constructors )
            {
                Assert.True( constructor.IsImplicitlyDeclared );
            }
        }

        [Fact]
        public void ParameterKinds()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class C
{
    int i;

    void M1(int i, in int j, ref int k, out int m) => m = 0;
    ref int M2() => ref i;
    ref readonly int M3() => ref i;
}";

            var compilation = testContext.CreateCompilationModel( code );

            var type = Assert.Single( compilation.Types );

            Assert.Equal( new[] { None, In, Ref, Out }, type.Methods.First().Parameters.SelectAsImmutableArray( p => p.RefKind ) );
            Assert.Equal( new[] { None, Ref, RefReadOnly }, type.Methods.SelectAsImmutableArray( m => m.ReturnParameter.RefKind ) );
        }

        [Fact]
        public void ParameterModifiers()
        {
            using var testContext = this.CreateTestContext();

            const string code =
                """
                static class C
                {
                    static void M(this object o1, object o2, params object[] o3) { }
                }
                """;

            var compilation = testContext.CreateCompilationModel( code );

            var type = Assert.Single( compilation.Types );
            var method = Assert.Single( type.Methods );

            Assert.Equal( [true, false, false], method.Parameters.SelectAsReadOnlyCollection( p => p.IsThis ) );
            Assert.Equal( [false, false, true], method.Parameters.SelectAsReadOnlyCollection( p => p.IsParams ) );
        }

        [Fact]
        public void ParameterDefaultValue()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
using System;

class C
{
    void M(int i, int j = 42, string s = ""forty two"", decimal d = 3.14m, DateTime dt = default, DateTime? dt2 = null, object o = null) {}
}";

            var compilation = testContext.CreateCompilationModel( code );

            var type = Assert.Single( compilation.Types );

            var method = type.Methods.First();

            var parametersWithoutDefaults = new[] { method.ReturnParameter, method.Parameters.ElementAt( 0 ) };

            foreach ( var parameter in parametersWithoutDefaults )
            {
                Assert.Null( parameter.DefaultValue );
            }

            var parametersWithDefaults = method.Parameters.Skip( 1 ).ToReadOnlyList();

            foreach ( var parameter in parametersWithDefaults )
            {
                Assert.NotNull( parameter.DefaultValue );
            }

            Assert.Equal(
                new object?[] { 42, "forty two", 3.14m, null, null, null },
                parametersWithDefaults.SelectAsImmutableArray( p => p.DefaultValue!.Value.Value ) );
        }

        [Fact]
        public void GetTypeByReflectionType()
        {
            using var testContext = this.CreateTestContext();

            var compilation = testContext.CreateCompilationModel( "" );

            Assert.Equal(
                "List<T>.Enumerator",
                compilation.Factory.GetTypeByReflectionType( typeof(List<>.Enumerator) ).ToString() );

            Assert.Equal(
                "Dictionary<int, string>",
                compilation.Factory.GetTypeByReflectionType( typeof(Dictionary<int, string>) ).ToString() );

            Assert.Equal( "int[][,]", compilation.Factory.GetTypeByReflectionType( typeof(int[][,]) ).ToString() );
            Assert.Equal( "void*", compilation.Factory.GetTypeByReflectionType( typeof(void*) ).ToString() );

            Assert.Throws<ArgumentException>( () => compilation.Factory.GetTypeByReflectionType( typeof(int).MakeByRefType() ) );
        }

        [Fact]
        public void TypeName()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
using System.Collections.Generic;

class C<T>
{
    int i;
    List<T>.Enumerator e;
    Dictionary<int, string> d;
    (int i, int j) t;
}";

            var compilation = testContext.CreateCompilationModel( code );

            var type = Assert.Single( compilation.Types );

            var fieldTypes = type.Fields.SelectAsImmutableArray( p => (INamedType) p.Type );

            Assert.Equal( new[] { "Int32", "Enumerator", "Dictionary", "ValueTuple" }, fieldTypes.SelectAsImmutableArray( t => t.Name ) );

            Assert.Equal(
                new[] { "System.Int32", "System.Collections.Generic.List.Enumerator", "System.Collections.Generic.Dictionary", "System.ValueTuple" },
                fieldTypes.SelectAsImmutableArray( t => t.FullName ) );
        }

        [Fact]
        public void PartialType()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
using System.Collections.Generic;

class A
{
}

partial class B
{
}
";

            var compilation = testContext.CreateCompilationModel( code );

            Assert.Equal( 2, compilation.Types.Count );

            Assert.False( compilation.Types.Single( t => t.Name == "A" ).IsPartial );
            Assert.True( compilation.Types.Single( t => t.Name == "B" ).IsPartial );
        }

        [Fact]
        public void ConstructGenericInstance()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class C<TC>
{
    (TC, TM) M<TM>() => default;
}
";

            var compilation = testContext.CreateCompilationModel( code );
            using var userCodeContext = testContext.WithExecutionContext( compilation );

            var type = Assert.Single( compilation.Types );

            var openMethod = type.Methods.First();
            var closedType = type.MakeGenericInstance( typeof(string) );
            var closedTypeMethod = closedType.Methods.First();
            var closedMethod = closedTypeMethod.MakeGenericInstance( typeof(int) );

            Assert.Equal( "(TC, TM)", openMethod.ReturnType.ToString() );
            Assert.Equal( "(string, TM)", closedTypeMethod.ReturnType.ToString() );
            Assert.Equal( "(string, int)", closedMethod.ReturnType.ToString() );

            // Generic type from a typeof.
            _ = ((INamedType) compilation.Factory.GetTypeByReflectionType( typeof(AsyncLocal<>) )).MakeGenericInstance( typeof(int) );

#pragma warning disable CS0618 // Type or member is obsolete
            var closedMethod2 = openMethod.WithTypeArguments( new[] { typeof(int) }, new[] { typeof(string) } );
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.Equal( "(int, string)", closedMethod2.ReturnType.ToString() );
        }

        [Fact]
        public void WithinGenericTypeInstance()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class Class<T>
{
    T field;
    T Property { get; set; }
    event System.Action<T> Event;
    T Method() => default;
    Class( T a ) {}
    static Class() {}
}
";

            var compilation = testContext.CreateCompilationModel( code );
            using var userCodeContext = testContext.WithExecutionContext( compilation );

            var openType = compilation.Types.Single();
            var typeInstance = openType.MakeGenericInstance( typeof(string) );

            Assert.Equal( "string", openType.Fields.Single( f => !f.IsImplicitlyDeclared ).ForTypeInstance( typeInstance ).Type.ToString() );
            Assert.Equal( "string", openType.Properties.Single().ForTypeInstance( typeInstance ).Type.ToString() );
            Assert.Equal( "Action<string>", openType.Events.Single().ForTypeInstance( typeInstance ).Type.ToString() );
            Assert.Equal( "string", openType.Methods.Single().ForTypeInstance( typeInstance ).ReturnType.ToString() );
            Assert.Equal( "string", openType.Constructors.Single().ForTypeInstance( typeInstance ).Parameters.ElementAt( 0 ).Type.ToString() );
            Assert.Equal( typeInstance, openType.StaticConstructor!.ForTypeInstance( typeInstance ).DeclaringType );
        }

        [Fact]
        public void ConstructGenericInstanceNestedType()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class Parent<TParent>
{
    class NestedGeneric<TNested>
    {
        (TParent, TNested, TMethod) GenericMethod<TMethod>() => default;
        (TParent, TNested) NonGenericMethod() => default;
    }

    class NestedNonGeneric 
    {
        (TParent, TMethod) GenericMethod<TMethod>() => default;
        TParent NonGenericMethod() => default;
    }
}
";

            var compilation = testContext.CreateCompilationModel( code );
            using var userCodeContext = testContext.WithExecutionContext( compilation );

            // Find the different types and check the IsGeneric and IsOpenGeneric properties.
            var openParentType = Assert.Single( compilation.Types );

            var genericNestedTypeOnOpenGenericParent = openParentType.Types.OfName( "NestedGeneric" ).Single();
            var genericMethodOnOpenGenericNestedType = genericNestedTypeOnOpenGenericParent.Methods.OfName( "GenericMethod" ).Single();
            Assert.True( genericMethodOnOpenGenericNestedType.IsGeneric );
            var nonGenericMethodOnOpenGenericNestedType = genericNestedTypeOnOpenGenericParent.Methods.OfName( "NonGenericMethod" ).Single();
            Assert.False( nonGenericMethodOnOpenGenericNestedType.IsGeneric );

            var genericMethodOnOpenNonGenericNestedType = genericNestedTypeOnOpenGenericParent.Methods.OfName( "GenericMethod" ).Single();
            Assert.True( genericMethodOnOpenNonGenericNestedType.IsGeneric );
            var nonGenericMethodOnOpenNonGenericNestedType = genericNestedTypeOnOpenGenericParent.Methods.OfName( "NonGenericMethod" ).Single();
            Assert.False( nonGenericMethodOnOpenNonGenericNestedType.IsGeneric );

            // Creating a closed nested type.
            var closedParentType = openParentType.MakeGenericInstance( typeof(string) );
            var closedGenericNestedType = closedParentType.Types.OfName( "NestedGeneric" ).Single().MakeGenericInstance( typeof(int) );
            Assert.Equal( "int", closedGenericNestedType.TypeArguments.ElementAt( 0 ).ToString() );

            // Open method of closed nested type.
            var openMethodOfClosedNestedType = closedGenericNestedType.Methods.OfName( "GenericMethod" ).Single();

            Assert.Equal( "(string, int, TMethod)", openMethodOfClosedNestedType.ReturnType.ToString() );

            // Closed method in closed nested type.
            var closedMethod = openMethodOfClosedNestedType.MakeGenericInstance( typeof(long) );
            Assert.Equal( "(string, int, long)", closedMethod.ReturnType.ToString() );
        }

        [Fact]
        public void Indexer()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
public sealed class C
{
    public string this[string key]
    {
        get { return string.Empty; }

        set { }
    }
}
";

            var compilation = testContext.CreateCompilationModel( code );
            Assert.Single( compilation.Types );
            Assert.Single( compilation.Types.ElementAt( 0 ).Indexers );
            Assert.Single( compilation.Types.ElementAt( 0 ).Indexers.ElementAt( 0 ).Parameters );
        }

        [Fact]
        public void InvalidIndexer()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
public sealed class C
{
    public string this[InvalidType key]
    {
        get { return string.Empty; }

        set { }
    }
}
";

            var compilation = testContext.CreateCompilationModel( code, ignoreErrors: true );
            Assert.Single( compilation.Types );
            Assert.Empty( compilation.Types.ElementAt( 0 ).Indexers );
        }

        [Fact]
        public void Record()
        {
            using var testContext = this.CreateTestContext();

            // ReSharper disable once ConvertToConstant.Local
            var code = @"
public record R( int A, int B )
{
  public int C { get; init; }
}
";

#if !NET5_0_OR_GREATER
            code +=
                "namespace System.Runtime.CompilerServices { internal static class IsExternalInit {} }";
#endif

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.OfName( "R" ).Single();
            var mainConstructor = type.Constructors.Single( p => p.Parameters.Count == 2 );
            Assert.False( mainConstructor.IsImplicitlyDeclared );

            var copyConstructor = type.Constructors.Single( p => p.Parameters.Count == 1 );
            Assert.True( copyConstructor.IsImplicitlyDeclared );

            Assert.False( type.Properties["A"].IsImplicitlyDeclared );

            Assert.False( type.Properties["C"].IsImplicitlyDeclared );

            Assert.True( type.Properties["EqualityContract"].IsImplicitlyDeclared );
        }

        [Fact]
        public void Depth()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class C 
{
    void M() { void N() {} }

    class D : C, L
    {
    }
}

interface I {}
interface J : I {}
interface K : I {}
interface L : J, K {}

namespace Ns1 
{
    class E {}

    namespace Ns2 
    {
        class F {}
    } 
}
";

            var compilation = testContext.CreateCompilationModel( code );

            var type = compilation.Types.OfName( "C" ).Single();

            Assert.Equal( 3, compilation.GetDepth( type ) );
            Assert.Equal( 4, compilation.GetDepth( type.Methods.OfName( "M" ).Single() ) );
            Assert.Equal( 3, compilation.GetDepth( compilation.Types.OfName( "I" ).Single() ) );
            Assert.Equal( 4, compilation.GetDepth( compilation.Types.OfName( "J" ).Single() ) );
            Assert.Equal( 4, compilation.GetDepth( compilation.Types.OfName( "K" ).Single() ) );
            Assert.Equal( 5, compilation.GetDepth( compilation.Types.OfName( "L" ).Single() ) );
            Assert.Equal( 6, compilation.GetDepth( type.Types.OfName( "D" ).Single() ) );

            var ns1 = compilation.GlobalNamespace.GetDescendant( "Ns1" ).AssertNotNull();
            var ns2 = compilation.GlobalNamespace.GetDescendant( "Ns1.Ns2" ).AssertNotNull();
            Assert.Equal( 3, compilation.GetDepth( ns1 ) );
            Assert.Equal( 4, compilation.GetDepth( ns2 ) );
            Assert.Equal( 4, compilation.GetDepth( compilation.Types.OfName( "E" ).Single() ) );
            Assert.Equal( 5, compilation.GetDepth( compilation.Types.OfName( "F" ).Single() ) );
        }

        [Fact]
        public void CompileTimeOnlyTypesAreInvisible()
        {
            var testServices = new AdditionalServiceCollection( new TestClassificationService() );
            using var testContext = this.CreateTestContext( testServices );

            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

[CompileTime]
class C { }

class D 
{
    [CompileTime]
    class E { }
}

";

            var compilation = testContext.CreateCompilationModel( code );
            Assert.Single( compilation.Types );
            Assert.Empty( compilation.Types.Single().Types );
        }

        [Fact]
        public void ExternalNamespace()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"";

            var compilation = testContext.CreateCompilationModel( code );

            var systemText = ((INamedType) compilation.Factory.GetTypeByReflectionType( typeof(StringBuilder) )).ContainingNamespace;
            Assert.Equal( "System.Text", systemText.FullName );
            Assert.Equal( "Text", systemText.Name );
            Assert.NotEmpty( systemText.Types );

            var system = systemText.ContainingNamespace.AssertNotNull();
            Assert.Equal( "System", system.FullName );
            Assert.Equal( "System", system.Name );
            Assert.True( systemText.IsDescendantOf( system ) );
            Assert.True( system.IsDescendantOf( compilation.GlobalNamespace ) );

            Assert.Single( system.Types.OfName( nameof(Math) ) );
            Assert.NotNull( system.Namespaces.OfName( "Collections" ) );

            Assert.False( system.BelongsToCurrentProject );
            Assert.Same( system.DeclaringAssembly, system.ContainingNamespace?.ContainingDeclaration );
        }

        [Fact]
        public void Namespaces()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
namespace Ns1
{
    namespace Ns2
    {
        class T1 {}

        namespace Ns4 
        {
            class T3 {}
        }
    }

    namespace Ns3
    {
        // Empty namespaces may or may not appear in the code model.
        // They do appear, de facto, in a complete compilation. They do not in a partial compilation.
    }
}

class T2 {}

namespace System { class MySystemClass {} }
";

            var compilation = testContext.CreateCompilationModel( code );

            Assert.True( compilation.GlobalNamespace.IsGlobalNamespace );
            var descendants = string.Join( ", ", compilation.GlobalNamespace.Descendants().SelectAsImmutableArray( x => x.FullName ).OrderBy( x => x ) );
            Assert.Equal( "Ns1, Ns1.Ns2, Ns1.Ns2.Ns4, Ns1.Ns3, System", descendants );
            Assert.Equal( "", compilation.GlobalNamespace.Name );
            Assert.Equal( "", compilation.GlobalNamespace.FullName );
            Assert.Null( compilation.GlobalNamespace.ContainingNamespace );
            Assert.Same( compilation, compilation.GlobalNamespace.ContainingDeclaration );

            var t2 = compilation.GlobalNamespace.Types.Single();
            Assert.Same( compilation.GlobalNamespace, t2.ContainingNamespace );

            var ns1 = compilation.GlobalNamespace.Namespaces.OfName( "Ns1" ).AssertNotNull();
            Assert.Equal( "Ns1", ns1.Name );
            Assert.Equal( "Ns1", ns1.FullName );
            var descendantsAndSelf = string.Join( ", ", ns1.DescendantsAndSelf().SelectAsImmutableArray( x => x.FullName ).OrderBy( x => x ) );
            Assert.Equal( "Ns1, Ns1.Ns2, Ns1.Ns2.Ns4, Ns1.Ns3", descendantsAndSelf );
            Assert.True( ns1.IsDescendantOf( compilation.GlobalNamespace ) );
            Assert.True( compilation.GlobalNamespace.IsAncestorOf( ns1 ) );

            Assert.Equal( 2, ns1.Namespaces.Count );

            var ns2 = ns1.Namespaces.OfName( "Ns2" ).AssertNotNull();
            Assert.Equal( "Ns2", ns2.Name );
            Assert.Equal( "Ns1.Ns2", ns2.FullName );
            Assert.Same( ns1, ns2.ContainingNamespace );
            Assert.True( ns2.IsDescendantOf( compilation.GlobalNamespace ) );
            Assert.True( ns2.IsDescendantOf( ns1 ) );
            Assert.True( compilation.GlobalNamespace.IsAncestorOf( ns2 ) );
            Assert.True( ns1.IsAncestorOf( ns2 ) );

            var t1 = ns2.Types.Single();
            Assert.Same( ns2, t1.ContainingNamespace );

            Assert.Same( ns2, compilation.GlobalNamespace.GetDescendant( "Ns1.Ns2" ) );
            Assert.Same( compilation.GlobalNamespace, compilation.GlobalNamespace.GetDescendant( "" ) );

            var externalType = (INamedType) compilation.Factory.GetTypeByReflectionType( typeof(EventHandler) );
            Assert.True( externalType.DeclaringAssembly.IsExternal );

            var systemNs = compilation.GlobalNamespace.GetDescendant( "System" ).AssertNotNull();
            Assert.Single( systemNs.Types );
            Assert.Empty( systemNs.Namespaces );
        }

        [Theory]
        [InlineData( SpecialType.Int32, SpecialType.Double, true )]
        [InlineData( SpecialType.Double, SpecialType.Int32, false )]
        public void TypeIs( SpecialType @from, SpecialType to, bool expectedResult )
        {
            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( "" );
            var fromType = compilation.Factory.GetSpecialType( @from );
            var toType = compilation.Factory.GetSpecialType( to );
            var result = fromType.IsConvertibleTo( toType, ConversionKind.Implicit );

            Assert.Equal( expectedResult, result );
        }

        [Fact]
        public void Generic()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"

class NonGeneric { }

class Base<T>
{ 
   public virtual void Method(T p ) {} 
}

class Derived : Base<int> 
{
  public override void Method(int p ) {}
  public void GenericMethod<T>() {} 
}
";

            var compilation = testContext.CreateCompilationModel( code );

            var nonGenericClass = compilation.Types.OfName( "NonGeneric" ).Single();
            Assert.False( nonGenericClass.IsGeneric );
            Assert.Same( nonGenericClass, nonGenericClass.Definition );

            var baseClass = compilation.Types.OfName( "Base" ).Single();
            Assert.True( baseClass.IsGeneric );
            Assert.Same( baseClass, baseClass.Definition );

            var baseMethod = baseClass.Methods.OfName( "Method" ).Single();
            Assert.False( baseMethod.IsGeneric );
            Assert.Same( baseMethod, baseMethod.Definition );

            var derivedClass = compilation.Types.OfName( "Derived" ).Single();
            Assert.False( derivedClass.IsGeneric );
            Assert.True( derivedClass.BaseType!.IsGeneric );
            Assert.Same( baseClass, derivedClass.BaseType.Definition );

            var derivedMethod = derivedClass.Methods.OfName( "Method" ).Single();
            Assert.False( derivedMethod.IsGeneric );
            Assert.False( derivedMethod.OverriddenMethod!.IsGeneric );
            Assert.Same( baseMethod, derivedMethod.OverriddenMethod.Definition );

            var genericMethod = derivedClass.Methods.OfName( "GenericMethod" ).Single();
            Assert.True( genericMethod.IsGeneric );
            Assert.Same( genericMethod, genericMethod.Definition );
        }

        [Fact]
        public void PrivateExternalMembersAreHidden()
        {
            using var testContext = this.CreateTestContext();

            const string masterCode = @"
public class PublicClass
{ 
   private int _privateField;
   public int PublicField;
   private int PrivateProperty { get; set; }
   public int PublicProperty { get; set; }
   private void PrivateMethod() {}
   public void PublicMethod() {}

   private class PrivateNestedClass {}
   public class PublicNestedClass {}
}


";

            var compilation = testContext.CreateCompilationModel( "", masterCode );
            var type = compilation.Factory.GetTypeByReflectionName( "PublicClass" );
            Assert.True( type.DeclaringAssembly.IsExternal );
            Assert.Single( type.Fields, f => !f.IsImplicitlyDeclared );
            Assert.Single( type.Methods );
            Assert.Single( type.Properties );
            Assert.Single( type.Types );
        }

        [Fact]
        public void NullableValueTypes()
        {
            using var testContext = this.CreateTestContext();

            var compilation = testContext.CreateCompilationModel( "" );
            var intType = (INamedType) compilation.Factory.GetTypeByReflectionType( typeof(int) );
            Assert.False( intType.IsNullable );
            Assert.Same( intType, intType.ToNonNullable() );
            Assert.Same( intType, intType.UnderlyingType );
            var nullableIntType = intType.ToNullable();
            Assert.NotSame( intType, nullableIntType );
            Assert.True( nullableIntType.IsNullable );
            Assert.Same( intType, nullableIntType.ToNonNullable() );
        }

        [Fact]
        public void NullableReferenceTypes()
        {
            using var testContext = this.CreateTestContext();

            var compilation = testContext.CreateCompilationModel( "" );
            var objectType = (INamedType) compilation.Factory.GetTypeByReflectionType( typeof(object) );
            Assert.Null( objectType.IsNullable );
            Assert.Same( objectType, objectType.UnderlyingType );
            var nonNullableObjectType = (INamedType) objectType.ToNonNullable();
            Assert.False( nonNullableObjectType.IsNullable );
            Assert.Same( objectType, nonNullableObjectType.UnderlyingType );
            var nullableObjectType = objectType.ToNullable();
            Assert.NotSame( objectType, nullableObjectType );
            Assert.True( nullableObjectType.IsNullable );
            Assert.Same( nonNullableObjectType, nullableObjectType.ToNonNullable() );
            Assert.Same( objectType, nullableObjectType.UnderlyingType );
            Assert.Equal( objectType, nullableObjectType, compilation.Comparers.Default );
            Assert.Equal( objectType, nonNullableObjectType, compilation.Comparers.Default );
            Assert.Equal( nullableObjectType, nonNullableObjectType, compilation.Comparers.Default );
            Assert.NotEqual( objectType, nullableObjectType, compilation.Comparers.IncludeNullability );
            Assert.NotEqual( objectType, nonNullableObjectType, compilation.Comparers.IncludeNullability );
            Assert.NotEqual( nullableObjectType, nonNullableObjectType, compilation.Comparers.IncludeNullability );
        }

        [Fact]
        public void AutomaticPropertiesAndBackingFields()
        {
            using var testContext = this.CreateTestContext();

            var compilation = testContext.CreateCompilationModel( @"class C { int P {get; set;} }" );
            var type = compilation.Types.Single();
            var property = type.Properties.Single();
            Assert.True( property.IsAutoPropertyOrField );
            var backingField = type.Fields.Single();
            Assert.True( backingField.IsImplicitlyDeclared );
        }

        [Fact]
        public void MetadataNames()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
public class C<T>
{ 
   class D<A,B> {}
   class E {}
}

class F { class G {} }

";

            var compilation = testContext.CreateCompilationModel( code );
            var c = compilation.Types.OfName( "C" ).Single();
            Assert.Equal( "C`1", c.GetFullMetadataName() );
            var d = c.Types.OfName( "D" ).Single();
            Assert.Equal( "C`1+D`2", d.GetFullMetadataName() );
            var e = c.Types.OfName( "E" ).Single();
            Assert.Equal( "C`1+E", e.GetFullMetadataName() );
            var f = compilation.Types.OfName( "F" ).Single();
            Assert.Equal( "F", f.GetFullMetadataName() );
            var g = f.Types.OfName( "G" ).Single();
            Assert.Equal( "F+G", g.GetFullMetadataName() );
        }

        [Fact]
        public void Compilation()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
public class C<T>
{ 
   class D<A,B> {}
   class E {}
}

class F { class G {} }

";

            var compilation = testContext.CreateCompilationModel( code );
            Assert.Null( compilation.ContainingDeclaration );
        }

        [Fact]
        public void DuplicateFile()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
[System.Obsolete]
class C {} 
";

            var compilation = testContext.CreateCompilation(
                new Dictionary<string, string> { { "file1.cs", code }, { "file2.cs", code } },
                ignoreErrors: true );

            var type = compilation.Types.OfName( "C" ).Single();
            var attributes = type.Attributes.OfAttributeType( typeof(ObsoleteAttribute) ).ToReadOnlyList();
            Assert.Equal( 2, attributes.Count );

            // Check that roundtrip attribute reference resolution work.
            foreach ( var attribute in attributes )
            {
                var roundtrip = attribute.ToRef().GetTarget( compilation );

                // Note that the code model does not preserve reference identity of attributes.
                Assert.Same(
                    attribute.GetDeclaringSyntaxReferences()[0].SyntaxTree,
                    roundtrip.GetDeclaringSyntaxReferences()[0].SyntaxTree );
            }
        }

        [Fact]
        public void AreInternalsVisibleTo()
        {
            using var testContext = this.CreateTestContext();

            var compilation = testContext.CreateCompilationModel(
                "",
                ignoreErrors: true,
                additionalReferences: new[]
                {
                    MetadataReference.CreateFromFile( typeof(Aspect).Assembly.Location ),
                    MetadataReference.CreateFromFile( this.GetType().Assembly.Location )
                } );

            var assemblyWithInternals = ((INamedType) compilation.Factory.GetTypeByReflectionType( typeof(Aspect) )).DeclaringAssembly;
            var consumingAssembly = ((INamedType) compilation.Factory.GetTypeByReflectionType( this.GetType() )).DeclaringAssembly;

            Assert.True( assemblyWithInternals.AreInternalsVisibleFrom( consumingAssembly ) );
            Assert.False( consumingAssembly.AreInternalsVisibleFrom( assemblyWithInternals ) );
        }

        [Theory]
        [InlineData( "int _f = 5", 5 )]
        [InlineData( "string _f = \"s\"", "s" )]
        [InlineData( "object? _f = null", null )]
        [InlineData( "object _f = null!", null )]
        public void InitializerExpression_TypedConstants( string fieldCode, object? value )
        {
            var code = $$"""
                                 public class C
                                 {
                                    {{fieldCode}};
                                 }
                         """;

            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.Single();
            var field = type.Fields.Single();
            var source = (ISourceExpression) field.InitializerExpression!;
            Assert.NotNull( source.AsTypedConstant );

            Assert.Equal( value, source.AsTypedConstant!.Value.Value );
        }

        [Fact]
        public void InitializerExpression_EnumMembers()
        {
            const string code = """
                                        public enum C
                                        {
                                            None = 0,
                                            One,
                                            Two = 2,
                                            Second = Two,
                                            Three = 2 + 1,
                                            Third = C.Three
                                        }
                                """;

            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.Single();

            Assert.Equal( 0, ((ISourceExpression?) type.Fields["None"].InitializerExpression)?.AsTypedConstant?.Value );
            Assert.Null( type.Fields["One"].InitializerExpression );
            Assert.Equal( 2, ((ISourceExpression?) type.Fields["Two"].InitializerExpression)?.AsTypedConstant?.Value );
            var threeExpression = (ISourceExpression) type.Fields["Three"].InitializerExpression.AssertNotNull();
            Assert.Equal( "2 + 1", threeExpression.AsString );
            var secondExpression = (ISourceExpression) type.Fields["Second"].InitializerExpression.AssertNotNull();
            Assert.Equal( 2, secondExpression.AsTypedConstant!.Value.Value );
            var thirdExpression = (ISourceExpression) type.Fields["Third"].InitializerExpression.AssertNotNull();
            Assert.Equal( 3, thirdExpression.AsTypedConstant!.Value.Value );
        }

        [Theory]
        [InlineData( "int F {get;} = 5", 5 )]
        [InlineData( "string F {get;} = \"s\"", "s" )]
        [InlineData( "object? F {get;} = null", null )]
        [InlineData( "object F {get;} = null!", null )]
        public void InitializerExpression_Property_TypedConstants( string fieldCode, object? value )
        {
            var code = $$"""
                                 public class C
                                 {
                                    {{fieldCode}};
                                 }
                         """;

            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.Single();
            var field = type.Properties.Single();
            var source = (ISourceExpression) field.InitializerExpression!;
            Assert.NotNull( source.AsTypedConstant );

            Assert.Equal( value, source.AsTypedConstant!.Value.Value );
        }

        [Fact]
        public void InitializerExpression_NotTypedConstant()
        {
            const string code = $$"""
                                          public class C
                                          {
                                             object _f = new object();
                                          }
                                  """;

            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.Single();
            var field = type.Fields.Single();
            var source = (ISourceExpression) field.InitializerExpression!;
            Assert.Null( source.AsTypedConstant );
        }

        [Fact]
        public void InitializerExpression_TypedConstants_Decimal() => this.InitializerExpression_TypedConstants( "decimal _f = 5m", 5m );

        [Theory]
        [InlineData( "object _f = System.ConsoleColor.Blue", ConsoleColor.Blue )]
        public void InitializerExpression_TypedConstants_Enum( string fieldCode, object value )
        {
            var code = $$"""
                                 public class C
                                 {
                                    {{fieldCode}};
                                 }
                         """;

            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.Single();
            var field = type.Fields.Single();
            var source = (ISourceExpression) field.InitializerExpression!;
            Assert.NotNull( source.AsTypedConstant );

            Assert.Equal( (int) value, source.AsTypedConstant!.Value.Value );
        }

        [Fact]
        public void FieldConstantValue()
        {
            const string code = $$"""
                                          public class C
                                          {
                                              const int _f = 5;
                                              int _a = 5;
                                  
                                          }
                                  """;

            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.Single();
            var field = type.Fields.OfName( "_f" ).Single();
            Assert.True( field.Writeability == Writeability.None );
            Assert.NotNull( field.ConstantValue );
            Assert.Equal( 5, field.ConstantValue!.Value.Value );
            Assert.Null( type.Fields.OfName( "_a" ).Single().ConstantValue );
        }

        [Fact]
        public void EnumType()
        {
            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( "" );
            var enumType = (INamedType) compilation.Factory.GetTypeByReflectionType( typeof(ConsoleColor) );

            var blue = enumType.Fields[nameof(ConsoleColor.Blue)];
            Assert.Equal( (int) ConsoleColor.Blue, blue.ConstantValue!.Value.Value );
        }

        [Fact]
        public void PartialMethods()
        {
            using var testContext = this.CreateTestContext();

            const string code = """
                                public partial class C
                                {
                                    public void NonPartial() {}
                                    partial void PartialVoid_NoImpl();
                                    partial void PartialVoid_Impl();
                                    public partial int PartialNonVoid();
                                }

                                public partial class C
                                {
                                    partial void PartialVoid_Impl() {}
                                    public partial int PartialNonVoid() => 42;
                                }
                                """;

            var compilation = testContext.CreateCompilationModel( code );
            var nonPartial = compilation.Types.ElementAt( 0 ).Methods.OfName( "NonPartial" ).Single();
            var partialVoidNoImpl = compilation.Types.ElementAt( 0 ).Methods.OfName( "PartialVoid_NoImpl" ).Single();
            var partialVoidImpl = compilation.Types.ElementAt( 0 ).Methods.OfName( "PartialVoid_Impl" ).Single();
            var partialNonVoid = compilation.Types.ElementAt( 0 ).Methods.OfName( "PartialNonVoid" ).Single();

            Assert.False( nonPartial.IsPartial );
            Assert.True( partialVoidNoImpl.IsPartial );
            Assert.True( partialVoidImpl.IsPartial );
            Assert.True( partialNonVoid.IsPartial );

            var partialDefinition = (IMethodSymbol) compilation.Types.Single().GetSymbol().AssertSymbolNotNull().GetMembers( "PartialVoid_Impl" ).Single();
            var partialImplementation = partialDefinition.PartialImplementationPart;

            Assert.NotSame( partialDefinition, partialImplementation );

            // Ensure references of both parts are the same.
            var partialImplementationRef = partialDefinition.ToRef( compilation.RefFactory );
            var partialDefinitionRef = partialImplementation.ToRef( compilation.RefFactory );
            Assert.Same( partialImplementationRef, partialDefinitionRef );

            // Ensure declarations of both parts are the same.
            Assert.Same( compilation.Factory.GetMethod( partialDefinition ), compilation.Factory.GetMethod( partialImplementation ) );
        }

#if ROSLYN_4_12_0_OR_GREATER
        [Fact]
        public void PartialProperties()
        {
            using var testContext = this.CreateTestContext();

            const string code = """
                                partial class C
                                {
                                    int P { get; set; }
                                    partial int PartialP { get; set; }
                                    partial int PartialP { get => 0; set {} }
                                }
                                """;

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.Single();

            var nonPartialProperty = type.Properties.OfName( "P" ).Single();
            var partialProperty = type.Properties.OfName( "PartialP" ).Single();

            Assert.False( nonPartialProperty.IsPartial );
            Assert.True( partialProperty.IsPartial );

            var partialDefinition = (IPropertySymbol) type.GetSymbol().AssertSymbolNotNull().GetMembers( "PartialP" ).Single();
            var partialImplementation = partialDefinition.PartialImplementationPart;

            Assert.NotSame( partialDefinition, partialImplementation );

            // Ensure references of both parts are the same.
            var partialImplementationRef = partialDefinition.ToRef( compilation.RefFactory );
            var partialDefinitionRef = partialImplementation.ToRef( compilation.RefFactory );
            Assert.Same( partialImplementationRef, partialDefinitionRef );

            // Ensure declarations of both parts are the same.
            Assert.Same( compilation.Factory.GetProperty( partialDefinition ), compilation.Factory.GetProperty( partialImplementation ) );
        }

        [Fact]
        public void PartialIndexers()
        {
            using var testContext = this.CreateTestContext();

            const string code = """
                                partial class C
                                {
                                    int this[int i] { get => 0; set {} }
                                    partial int this[string s] { get; set; }
                                    partial int this[string s] { get => 0; set {} }
                                }
                                """;

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.Single();

            var intType = compilation.Factory.GetSpecialType( SpecialType.Int32 );
            var stringType = compilation.Factory.GetSpecialType( SpecialType.String );

            var nonPartialIndexer = type.Indexers.OfExactSignature( [intType] );
            var partialIndexer = type.Indexers.OfExactSignature( [stringType] );

            Assert.False( nonPartialIndexer.IsPartial );
            Assert.True( partialIndexer.IsPartial );

            var partialDefinition = type.GetSymbol()
                .AssertSymbolNotNull()
                .GetMembers( "this[]" )
                .Cast<IPropertySymbol>()
                .Single( i => i.Parameters.Single().Type.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_String );

            var partialImplementation = partialDefinition.PartialImplementationPart;

            Assert.NotSame( partialDefinition, partialImplementation );

            // Ensure references of both parts are the same.
            var partialImplementationRef = partialDefinition.ToRef( compilation.RefFactory );
            var partialDefinitionRef = partialImplementation.ToRef( compilation.RefFactory );
            Assert.Same( partialImplementationRef, partialDefinitionRef );

            // Ensure declarations of both parts are the same.
            Assert.Same( compilation.Factory.GetProperty( partialDefinition ), compilation.Factory.GetProperty( partialImplementation ) );
        }
#endif

        [Fact]
        public void HasImplementation()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
public interface I
{
    void Method();
    int Property { get; set; }
    event System.EventHandler Event;
" +
#if NET6_0_OR_GREATER
                                @"
    virtual void VirtualMethod() {}
    virtual int VirtualProperty {
        get => 42;
        set {}
    }
    virtual event System.EventHandler VirtualEvent {
        add {}
        remove {}
    }
" +
#endif
                                @"
}

public abstract class A
{
    public const int Const = 42;
    public int Field;

    public A() {}
    static A() {}

    public void Method() {}
    public abstract void AbstractMethod();  

    public int Property { get;set; }
    public abstract int AbstractProperty { get; set;}

    public event System.EventHandler Event;
    public abstract event System.EventHandler AbstractEvent;

    public static void StaticMethod() {}
    public static extern void StaticExternMethod();
}

public partial class B
{
    public int Field = 42;
    public static int StaticField = 42;
    partial void PartialVoid_NoImpl();
    public partial void PartialVoid_Impl();
    public partial int Partial();
}

public partial class B
{
    public partial void PartialVoid_Impl() {}
    public partial int Partial() => 42;
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var typeI = compilation.Types.OfName( "I" ).Single();
            var typeA = compilation.Types.OfName( "A" ).Single();
            var typeB = compilation.Types.OfName( "B" ).Single();

            Assert.False( GetForMethod( typeI, "Method" ) );
            Assert.False( GetForProperty( typeI, "Property" ) );
            Assert.False( GetForEvent( typeI, "Event" ) );
#if NET6_0_OR_GREATER
            Assert.True( GetForMethod( typeI, "VirtualMethod" ) );
            Assert.True( GetForProperty( typeI, "VirtualProperty" ) );
            Assert.True( GetForEvent( typeI, "VirtualEvent" ) );
#endif

            Assert.True( GetForField( typeA, "Field" ) );
            Assert.False( GetForField( typeA, "Const" ) );
            Assert.True( GetForConstructor( typeA ) );
            Assert.True( GetForStaticConstructor( typeA ) );
            Assert.True( GetForMethod( typeA, "Method" ) );
            Assert.True( GetForProperty( typeA, "Property" ) );
            Assert.True( GetForEvent( typeA, "Event" ) );
            Assert.False( GetForMethod( typeA, "AbstractMethod" ) );
            Assert.False( GetForProperty( typeA, "AbstractProperty" ) );
            Assert.False( GetForEvent( typeA, "AbstractEvent" ) );
            Assert.True( GetForMethod( typeA, "StaticMethod" ) );
            Assert.False( GetForMethod( typeA, "StaticExternMethod" ) );

            Assert.False( GetForMethod( typeB, "PartialVoid_NoImpl" ) );
            Assert.True( GetForConstructor( typeB ) );
            Assert.True( GetForStaticConstructor( typeB ) );
            Assert.True( GetForMethod( typeB, "PartialVoid_Impl" ) );
            Assert.True( GetForMethod( typeB, "Partial" ) );

            static bool GetForMethod( INamedType type, string name ) => type.Methods.OfName( name ).Single().HasImplementation;

            static bool GetForProperty( INamedType type, string name ) => type.Properties.OfName( name ).Single().HasImplementation;

            static bool GetForEvent( INamedType type, string name ) => type.Events.OfName( name ).Single().HasImplementation;

            static bool GetForConstructor( INamedType type ) => type.Constructors.Single().HasImplementation;

            static bool GetForStaticConstructor( INamedType type ) => type.StaticConstructor.AssertNotNull().HasImplementation;

            static bool GetForField( INamedType type, string name ) => type.Fields.OfName( name ).Single().HasImplementation;
        }

        [Fact]
        private void SourceReferencesToMethods()
        {
            using var testContext = this.CreateTestContext();

            const string code = """
                                public partial class C
                                {
                                    public void NonPartial() {}
                                    partial void PartialVoid_NoImpl();
                                    partial void PartialVoid_Impl();
                                    public partial int PartialNonVoid();
                                }

                                public partial class C
                                {
                                    partial void PartialVoid_Impl() {}
                                    public partial int PartialNonVoid() => 42;
                                }
                                """;

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.Single();
            Assert.Equal( 2, type.Sources.Length );
            var partialMethod = type.Methods.OfName( "PartialNonVoid" ).Single();
            Assert.Equal( 2, partialMethod.Sources.Length );
            Assert.True( partialMethod.HasImplementation );
            Assert.Single( partialMethod.Sources, s => s.IsImplementationPart );
        }

#if ROSLYN_4_12_0_OR_GREATER
        [Fact]
        private void SourceReferencesToProperties()
        {
            using var testContext = this.CreateTestContext();

            const string code =
                """
                public partial class C
                {
                    public int P { get; set; }
                    partial int PartialP { get; set; }
                    partial int PartialP { get => 0; set {} }
                }
                """;

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.Single();
            Assert.Single( type.Sources );
            var partialProperty = type.Properties["PartialP"];
            Assert.Equal( 2, partialProperty.Sources.Length );
            Assert.True( partialProperty.HasImplementation );
            Assert.Single( partialProperty.Sources, s => s.IsImplementationPart );
        }
#endif

        [Fact]
        public void RecordImplicitPropertyInitializer()
        {
            using var testContext = this.CreateTestContext();

#if !NET5_0_OR_GREATER
            var code =
#else
            const string code =
#endif
                """
                record R(int P);
                """;

#if !NET5_0_OR_GREATER
            code += "namespace System.Runtime.CompilerServices { internal static class IsExternalInit {} }";
#endif

            var compilation = testContext.CreateCompilationModel( code );
            var record = compilation.Types.OfName( "R" ).Single();
            var property = record.Properties.OfName( "P" ).Single();

            Assert.Null( property.InitializerExpression );
        }

        [Fact]
        public void NullableGeneric()
        {
            using var testContext = this.CreateTestContext();

            const string code = """
                                public partial class C<T1,T2,T3,T4>
                                  where T2 : struct
                                  where T3 : class
                                  where T4 : notnull
                                {
                                    public void M(T1? p1, T2? p2, T3? p3, T4 p4) {}
                                }
                                """;

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.Single();
            var method = type.Methods.Single();

            var parameterType1 = method.Parameters[0].Type;
            Assert.True( parameterType1.IsNullable );
            Assert.IsAssignableFrom<ITypeParameter>( parameterType1 );
            Assert.Equal( parameterType1, parameterType1.ToNonNullable().ToNullable() );
            Assert.Equal( SpecialType.None, parameterType1.SpecialType );

            var parameterType2 = method.Parameters[1].Type;
            Assert.True( parameterType2.IsNullable );
            var parameterType2AsNamedType = Assert.IsAssignableFrom<INamedType>( parameterType2 );
            Assert.Same( parameterType2AsNamedType.UnderlyingType, parameterType2AsNamedType );
            Assert.Equal( parameterType2, parameterType2.ToNonNullable().ToNullable() );
            Assert.Equal( SpecialType.None, parameterType2.SpecialType );

            var parameterType3 = method.Parameters[2].Type;
            Assert.True( parameterType3.IsNullable );
            Assert.IsAssignableFrom<ITypeParameter>( parameterType3 );
            Assert.Equal( parameterType3, parameterType3.ToNonNullable().ToNullable() );
            Assert.Equal( SpecialType.None, parameterType3.SpecialType );

            var parameterType4 = method.Parameters[3].Type;
            Assert.False( parameterType4.IsNullable );
            Assert.IsAssignableFrom<ITypeParameter>( parameterType4 );
            Assert.Equal( SpecialType.None, parameterType4.SpecialType );
        }

        [Fact]
        public void NullableGenericTypeParameter()
        {
            using var testContext = this.CreateTestContext();

            const string code =
                """
                using System;

                class C<T1,T2,T3>
                    where T2 : class?
                    where T3 : IDisposable?
                {
                    public void M(T1? n1, T2? n2, T3? n3, T1 nn1, T2 nn2, T3 nn3) {}
                }
                """;

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.Single();
            var method = type.Methods.Single();

            var typeParameter1 = type.TypeParameters[0];
            Assert.Null( typeParameter1.IsNullable );
            Assert.True( typeParameter1.ToNullable().IsNullable );
            Assert.Null( typeParameter1.ToNonNullable().IsNullable );
            Assert.True( method.Parameters["n1"].Type.IsNullable );
            Assert.Null( method.Parameters["nn1"].Type.IsNullable );

            var typeParameter2 = type.TypeParameters[1];
            Assert.Null( typeParameter2.IsNullable );
            Assert.True( typeParameter2.ToNullable().IsNullable );
            Assert.Null( typeParameter2.ToNonNullable().IsNullable );
            Assert.True( method.Parameters["n2"].Type.IsNullable );
            Assert.Null( method.Parameters["nn2"].Type.IsNullable );

            var typeParameter3 = type.TypeParameters[2];
            Assert.Null( typeParameter3.IsNullable );
            Assert.True( typeParameter3.ToNullable().IsNullable );
            Assert.Null( typeParameter3.ToNonNullable().IsNullable );
            Assert.True( method.Parameters["n3"].Type.IsNullable );
            Assert.Null( method.Parameters["nn3"].Type.IsNullable );
        }

        [Fact]
        public void TypeParameterEquality()
        {
            using var testContext = this.CreateTestContext();

            const string code = "class C<T>;";

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.Single();
            var typeParameterSymbol = type.TypeParameters.Single().GetSymbol();

            var factory = compilation.Factory;

            var asType = factory.GetIType( typeParameterSymbol );
            var asTypeParameter = factory.GetTypeParameter( typeParameterSymbol );

            var nullableAsType = factory.GetIType( typeParameterSymbol.WithNullableAnnotation( NullableAnnotation.Annotated ) );

            var nullableAsTypeParameter =
                factory.GetTypeParameter( (ITypeParameterSymbol) typeParameterSymbol.WithNullableAnnotation( NullableAnnotation.Annotated ) );

            var nullableFromIType = asType.ToNullable();

            Assert.Same( asType, asTypeParameter );

            // Additional expectations (try not to break it when implementing constructed ITypes from built types).
            Assert.Same( nullableAsType, nullableAsTypeParameter );
            Assert.NotSame( asType, nullableAsType );
            Assert.True( compilation.Comparers.Default.Equals( asType, nullableAsType ) );
            Assert.False( compilation.Comparers.IncludeNullability.Equals( asType, nullableAsType ) );
            Assert.Same( nullableAsType, nullableFromIType );
        }

        [Fact]
        public void HalfOverriddenProperty()
        {
            using var testContext = this.CreateTestContext();

            const string code = """
                                class Base
                                {
                                    public virtual int P { get; set; }
                                }

                                class Derived : Base
                                {
                                    public override int P { get => 42; }
                                }
                                """;

            var compilation = testContext.CreateCompilationModel( code );
            var derived = compilation.Types.OfName( "Derived" ).Single();
            var property = derived.Properties.OfName( "P" ).Single();

            Assert.Null( property.SetMethod );
        }

        /*
        [Fact]
        public void ExternalInternalAutomaticProperty()
        {
            using var testContext = this.CreateTestContext();
            var dependency = testContext.CreateCSharpCompilation( "public class C { public string P { get; internal set; } }" );
            var dependencyStream = new MemoryStream();
            Assert.True( dependency.Emit( dependencyStream ).Success );
            dependencyStream.Seek( 0, SeekOrigin.Begin );
            var dependencyReference = MetadataReference.CreateFromStream( dependencyStream );

            var compilation = testContext.CreateCompilationModel( "class D : C {}", additionalReferences:new[]{dependencyReference}  );

            var type = compilation.Types.OfName( "D" ).Single().BaseType.AssertNotNull(  );
            var property = type.Properties.Single();
            Assert.Equal( Writeability.None, property.Writeability );
            Assert.Null( property.SetMethod );

        }
        */

        [Fact]
        public void ParametersInGenericMethodsAreMappedInGenericMethodInstances()
        {
            using var testContext = this.CreateTestContext();

            const string code = """
                                class C
                                {
                                    T M<T>(T p) => p;
                                }
                                """;

            var compilation = testContext.CreateCompilationModel( code );

            using ( testContext.WithExecutionContext( compilation ) )
            {
                var genericMethodDefinition = compilation.Types.Single().Methods.Single();

                var genericMethodInstance = genericMethodDefinition.MakeGenericInstance( typeof(int) );

                Assert.Equal( SpecialType.Int32, genericMethodInstance.ReturnType.SpecialType );
                Assert.Equal( SpecialType.Int32, genericMethodInstance.Parameters[0].Type.SpecialType );
            }
        }

        [Fact]
        public void TryFindImplementationForInterfaceMember()
        {
            using var testContext = this.CreateTestContext();

            const string code = """
                                using System.Collections.Generic;
                                class C
                                {
                                    IReadOnlyCollection<int> f1;
                                    List<int> f2;
                                }
                                """;

            var compilation = testContext.CreateCompilationModel( code );

            var interfaceType = (INamedType) compilation.Types.Single().Fields.OfName( "f1" ).Single().Type;
            var interfaceMethod = interfaceType.Properties.OfName( "Count" ).Single().GetMethod;

            Assert.True( interfaceType.TryFindImplementationForInterfaceMember( interfaceMethod, out var roundtrip ) );
            Assert.Same( interfaceMethod, roundtrip );

            var classType = (INamedType) compilation.Types.Single().Fields.OfName( "f2" ).Single().Type;
            Assert.True( classType.TryFindImplementationForInterfaceMember( interfaceMethod, out _ ) );
        }

        [Fact]
        public void IsDefinition()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
using System;

class B<TB>
{
   public virtual TB BaseMethod() => default;
   public virtual TB BaseMethod2() => default; 
}

class C<TC> : B<TC>
{    
    TC field;
    TC Property { get; set; }
    TC Method( TC p1, TC[] p2, Action<TC> p3 ) => p1;
    event Action<TC> Event;
   public override TC BaseMethod() => base.BaseMethod();
}";

            var compilation = testContext.CreateCompilationModel( code );

            // All members are definitions.
            foreach ( var member in compilation.Types.OfName( "B" ).Single().Members() )
            {
                Assert.True( member.GetSymbol().IsDefinitionSafe() );
            }

            foreach ( var member in compilation.Types.OfName( "C" ).Single().Members() )
            {
                Assert.True( member.GetSymbol().IsDefinitionSafe() );
            }

            // Inherited members are not definitions.
            var inheritedMethod = compilation.Types.OfName( "C" ).Single().AllMethods.OfName( "BaseMethod2" ).Single();
            Assert.False( inheritedMethod.GetSymbol().IsDefinitionSafe() );
        }
    }
}