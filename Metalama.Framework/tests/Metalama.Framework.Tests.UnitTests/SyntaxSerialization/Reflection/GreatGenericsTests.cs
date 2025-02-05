// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.ReflectionMocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Reflection
{
    public sealed class GreatGenericsTests : ReflectionTestBase
    {
        private readonly IEnumerable<INamedType> _topLevelTypes;

        public GreatGenericsTests( ITestOutputHelper helper ) : base( helper )
        {
            const string code = @"
class Origin<T1> { 
    private T1 privateField;
    public T1 Field;
    private T1 privateProperty { get; }
    public T1 Property { get; }
    public event System.Action<T1> Actioned;
    public T1 Method(T1 parameter) => default(T1);
    public Origin(T1 input) { }
    public class NestedInOrigin<T2> : Origin<int> {
        public NestedInOrigin() : base (default) {}

        public T2 Method2(T2 input) => default(T2);
        public T2 Method21(T1 input) => default(T2);
    }
}
class Descendant<T3> : Origin<string>.NestedInOrigin<T3> {
    public T3 Field;
}
class User {
    public Descendant<float> FullyInstantiated;
}";

            using var testContext = this.CreateSerializationTestContext( code );
            var compilation = testContext.Compilation;
            this._topLevelTypes = compilation.Types;
        }

        [Fact]
        public void TestGenericTemplates()
        {
            // Pure types
            var origin = this._topLevelTypes.Single( t => t.Name == "Origin" );
            var nested = origin.Types.Single();
            var descendant = this._topLevelTypes.Single( t => t.Name == "Descendant" );

            this.TestSerializable(
                nested.Method( "Method21" ),
                m =>
                {
                    Assert.Equal( "T2", m.ReturnType.Name );
                    Assert.Equal( "T1", m.GetParameters()[0].ParameterType.Name );
                },
                @"((global::System.Reflection.MethodInfo)global::Metalama.Framework.RunTime.ReflectionHelper.GetMethod(typeof(global::Origin<>.NestedInOrigin<>), ""Method21"", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance, ""T2 Method21(T1)"")!)" );

            this.TestSerializable(
                descendant.BaseType!.Method( "Method21" ),
                m =>
                {
                    Assert.Equal( "T3", m.ReturnType.Name );
                    Assert.Equal( "String", m.GetParameters()[0].ParameterType.Name );
                },
                @"((global::System.Reflection.MethodInfo)global::Metalama.Framework.RunTime.ReflectionHelper.GetMethod(typeof(global::Origin<global::System.String>.NestedInOrigin<T3>), ""Method21"", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance, ""T3 Method21(System.String)"")!)" );
        }

        [Fact]
        public void TestGenericInstances()
        {
            // Generic instances
            var user = this._topLevelTypes.Single( t => t.Name == "User" );
            var instantiatedDescendant = (INamedType) user.Fields.Single().Type;
            var instantiatedNested = instantiatedDescendant.BaseType!;
            var instantiatedBaseOrigin = instantiatedNested.BaseType!;

            this.TestSerializable(
                instantiatedNested.Method( "Method21" ),
                m =>
                {
                    Assert.Equal( typeof(float), m.ReturnType );
                    Assert.Equal( typeof(string), m.GetParameters()[0].ParameterType );
                },
                @"((global::System.Reflection.MethodInfo)typeof(global::Origin<global::System.String>.NestedInOrigin<global::System.Single>).GetMethod(""Method21"", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance, null, new[] { typeof(global::System.String) }, null)!)" );

            this.TestSerializable(
                instantiatedNested.Constructors.Single(),
                c =>
                {
                    Assert.Equal( typeof(string), c.DeclaringType!.GenericTypeArguments[0] );
                    Assert.Equal( typeof(float), c.DeclaringType.GenericTypeArguments[1] );
                    Assert.Equal( typeof(int), c.DeclaringType.BaseType!.GenericTypeArguments[0] );
                },
                @"((global::System.Reflection.ConstructorInfo)typeof(global::Origin<global::System.String>.NestedInOrigin<global::System.Single>).GetConstructor(global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance, null, global::System.Type.EmptyTypes, null)!)" );

            this.TestSerializable(
                ((INamedType) instantiatedNested.ContainingDeclaration!).Method( "Method" ),
                m => Assert.Equal( typeof(string), m.ReturnType ),
                @"((global::System.Reflection.MethodInfo)typeof(global::Origin<global::System.String>).GetMethod(""Method"", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance, null, new[] { typeof(global::System.String) }, null)!)" );

            this.TestSerializable(
                instantiatedDescendant.Field( "Field" ),
                ( FieldInfo f ) => Assert.Equal( typeof(float), f.FieldType ),
                @"typeof(global::Descendant<global::System.Single>).GetField(""Field"", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance)!" );

            this.TestSerializable(
                instantiatedBaseOrigin.Field( "Field" ),
                ( FieldInfo f ) => Assert.Equal( typeof(int), f.FieldType ),
                @"typeof(global::Origin<global::System.Int32>).GetField(""Field"", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance)!" );

            this.TestSerializable(
                instantiatedBaseOrigin.Field( "privateField" ),
                ( FieldInfo f ) => Assert.Equal( typeof(int), f.FieldType ),
                @"typeof(global::Origin<global::System.Int32>).GetField(""privateField"", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance)!" );

            this.TestSerializable(
                instantiatedBaseOrigin.Property( "Property" ),
                ( PropertyInfo p ) => Assert.Equal( typeof(int), p.PropertyType ),
                @"typeof(global::Origin<global::System.Int32>).GetProperty(""Property"", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance)!" );

            this.TestSerializable(
                instantiatedBaseOrigin.Property( "privateProperty" ),
                ( PropertyInfo p ) => Assert.Equal( typeof(int), p.PropertyType ),
                @"typeof(global::Origin<global::System.Int32>).GetProperty(""privateProperty"", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance)!" );

            this.TestSerializable(
                instantiatedBaseOrigin,
                t =>
                {
                    Assert.Equal( "Origin`1", t.Name );
                    Assert.Equal( typeof(int), t.GenericTypeArguments[0] );
                },
                @"typeof(global::Origin<global::System.Int32>)" );

            this.TestSerializable(
                instantiatedBaseOrigin.Event( "Actioned" ),
                e =>
                {
                    Assert.Equal( "Actioned", e.Name );
                    Assert.Equal( typeof(Action<int>), e.EventHandlerType );
                },
                @"typeof(global::Origin<global::System.Int32>).GetEvent(""Actioned"", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance)!" );
        }

        private void TestSerializable( IType type, Action<Type> withResult, string expectedCode )
        {
            var compilation = type.GetCompilationModel();
            using var testContext = this.CreateSerializationTestContext( compilation );

            this.TestExpression<Type>(
                compilation.RoslynCompilation.SyntaxTrees.First().ToString(),
                testContext.Serialize<Type>( CompileTimeType.Create( type ) ).ToString(),
                withResult,
                expectedCode );
        }

        private void TestSerializable( IMethod method, Action<MethodInfo> withResult, string expectedCode )
        {
            var compilation = method.GetCompilationModel();
            using var testContext = this.CreateSerializationTestContext( compilation );

            this.TestExpression<MethodInfo>(
                compilation.RoslynCompilation.SyntaxTrees.First().ToString(),
                testContext.Serialize( CompileTimeMethodInfo.Create( method ) ).ToString(),
                withResult,
                expectedCode );
        }

        private void TestSerializable( IConstructor method, Action<ConstructorInfo> withResult, string expectedCode )
        {
            var compilation = method.GetCompilationModel();
            using var testContext = this.CreateSerializationTestContext( compilation );

            this.TestExpression<ConstructorInfo>(
                compilation.RoslynCompilation.SyntaxTrees.First().ToString(),
                testContext.Serialize( CompileTimeConstructorInfo.Create( method ) ).ToString(),
                withResult,
                expectedCode );
        }

        private void TestSerializable<T>( IFieldOrProperty property, Action<T> withResult, string expectedCode )
        {
            var compilation = property.GetCompilationModel();
            using var testContext = this.CreateSerializationTestContext( compilation );

            this.TestExpression<T>(
                compilation.RoslynCompilation.SyntaxTrees.First().ToString(),
                MetalamaPropertyInfoTests.StripLocationInfo( testContext.Serialize( CompileTimeFieldOrPropertyInfo.Create( property ) ).ToString() ),
                withResult,
                expectedCode );
        }

        private void TestSerializable( IEvent @event, Action<EventInfo> withResult, string expectedCode )
        {
            var compilation = @event.GetCompilationModel();
            using var testContext = this.CreateSerializationTestContext( compilation );

            this.TestExpression<EventInfo>(
                compilation.RoslynCompilation.SyntaxTrees.First().ToString(),
                testContext.Serialize( CompileTimeEventInfo.Create( @event ) ).ToString(),
                withResult,
                expectedCode );
        }

        private void TestExpression<T>( string context, string expression, Action<T> withResult, string expectedCode )
        {
            this.TestExpression( context, expression, withResult );
            this.AssertEqual( expectedCode, expression );
        }
    }
}