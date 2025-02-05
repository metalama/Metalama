// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Utilities;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedParameter.Global
// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedParameter.Local
// ReSharper disable MemberCanBePrivate.Global

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Reflection;

public sealed class ReflectionSignatureBuilderTests : UnitTestClass
{
    [Fact]
    public void TestGetMethodSignature()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "" );
        var modelType = (INamedType) compilation.Factory.GetTypeByReflectionType( typeof(C<>) );

        foreach ( var reflectionMethod in typeof(C<>).GetMethods(
                     BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly ) )
        {
            var modelMethod = modelType.Methods.OfName( reflectionMethod.Name ).Single();

            var signature = ReflectionSignatureBuilder.GetMethodSignature( modelMethod );

            Assert.Equal( reflectionMethod.ToString(), signature );
        }
    }

    [Fact]
    public void TestGetConstructorSignature()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "" );
        var modelType = (INamedType) compilation.Factory.GetTypeByReflectionType( typeof(C<>) );

        var parametersCount = 0;

        foreach ( var reflectionConstructor in typeof(C<>).GetConstructors(
                     BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly ) )
        {
            var modelConstructor = modelType.Constructors.Single( c => c.Parameters.Count == parametersCount );

            var signature = ReflectionSignatureBuilder.GetConstructorSignature( modelConstructor );

            Assert.Equal( reflectionConstructor.ToString(), signature );
            parametersCount++;
        }
    }

    [Fact]
    public void TestMethodHasTypeArgument()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "" );
        var modelType = (INamedType) compilation.Factory.GetTypeByReflectionType( typeof(C<>) );

        foreach ( var reflectionMethod in typeof(C<>).GetMethods(
                     BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly ) )
        {
            var modelMethod = modelType.Methods.OfName( reflectionMethod.Name ).Single();

            var hasTypeArgument = ReflectionSignatureBuilder.HasTypeArgument( modelMethod );

            // ReSharper disable UnusedAutoPropertyAccessor.Global, ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            Assert.Equal( reflectionMethod.ToString()?.ContainsOrdinal( "TypeArgument" ), hasTypeArgument );
        }
    }

    [Fact]
    public void TestConstructorHasTypeArgument()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "" );
        var modelType = (INamedType) compilation.Factory.GetTypeByReflectionType( typeof(C<>) );

        var parametersCount = 0;

        foreach ( var reflectionConstructor in typeof(C<>).GetConstructors(
                     BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly ) )
        {
            var modelConstructor = modelType.Constructors.Single( c => c.Parameters.Count == parametersCount );

            var hasTypeArgument = ReflectionSignatureBuilder.HasTypeArgument( modelConstructor );

            // ReSharper disable UnusedAutoPropertyAccessor.Global, ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            Assert.Equal( reflectionConstructor.ToString()?.ContainsOrdinal( "TypeArgument" ), hasTypeArgument );
            parametersCount++;
        }
    }

    // The class and the methods must be public otherwise they are not present in the reference assembly that visible from the compilation model.
    // ReSharper disable InconsistentNaming
    public sealed class C<TypeArgument1>
    {
        // Methods.
        public void M1() { }

        public void M2( TypeArgument1 x, dynamic y ) { }

        public void M3( int p1, object p2, bool p3, byte p4, sbyte p5, Enum e, decimal d ) { }

        public void M4<TypeArgument2>( TypeArgument2 x ) { }

        public void M5<TypeArgument2, TypeArgument3>( TypeArgument2 x, TypeArgument3 y ) { }

        public void M6<TypeArgument2>( out TypeArgument2? x )
        {
            x = default;
        }

        public unsafe void M7<TypeArgument2>( int* x ) { }

        public void M8<TypeArgument2>( TypeArgument2[] x, int[][] y, int[,] z ) { }

        public void M9<TypeArgument2>( List<TypeArgument2> x, Dictionary<int, string> y ) { }

        public void M10<TypeArgument2>( List<List<TypeArgument1>> x, Dictionary<Dictionary<int, double>, Dictionary<bool, string>> y ) { }

        public void M11<TypeArgument2>( TypeArgument2? x ) { }

        public void M12<TypeArgument2>( TypeArgument2? x )
            where TypeArgument2 : struct { }

        public void M13( double[][] x, double y, double[,] z ) { }

        // Constructors.
        public C() { }

        public C( TypeArgument1 p1 ) { }

        public C( out TypeArgument1? p1, Dictionary<int, Dictionary<double, TypeArgument1>> p2 ) { p1 = default; }

        public C( TypeArgument1[,] p1, Dictionary<Dictionary<double[], int>, dynamic> p2, ref List<TypeArgument1> p3 ) { }

        public C( Enum p1, Type p2, dynamic p3, ref ushort p4 ) { }

        public unsafe C( TypeArgument1[] p1, List<TypeArgument1> p2, Type p3, int* p4, double[][] p5 ) { }

        public C( int p1, object p2, bool p3, byte p4, byte p5, sbyte p6 ) { }
    }

    // ReSharper restore InconsistentNaming
}