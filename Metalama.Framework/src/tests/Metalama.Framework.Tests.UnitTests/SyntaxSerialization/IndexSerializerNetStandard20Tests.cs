// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxSerialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization;

/// <summary>
/// Regression test for issue #1459: GetSerializableTypes should not throw when
/// System.Index/System.Range are not available in the target compilation
/// (e.g., netstandard2.0 / .NET Framework targets).
/// </summary>
public sealed class IndexSerializerNetStandard20Tests : SerializerTestsBase
{
    public IndexSerializerNetStandard20Tests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Creates a compilation that does not include System.Index,
    /// simulating a netstandard2.0 or .NET Framework target.
    /// </summary>
    private static CSharpCompilation CreateCompilationWithoutSystemIndex()
    {
        // Build a minimal "runtime" assembly that has fundamental types but NOT System.Index.
        var minimalSource = @"
namespace System
{
    public class Object { }
    public class ValueType : Object { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public struct Int64 { }
    public struct Double { }
    public struct Single { }
    public struct Byte { }
    public struct SByte { }
    public struct Int16 { }
    public struct UInt16 { }
    public struct UInt32 { }
    public struct UInt64 { }
    public struct Char { }
    public struct Decimal { }
    public struct IntPtr { }
    public struct UIntPtr { }
    public class String { }
    public class Enum : ValueType { }
    public class Attribute { }
    public class Type { }
    public struct DateTime { }
    public struct TimeSpan { }
    public struct DateTimeOffset { }
    public struct Guid { }
    public class Exception { }
    public class Array { }
    public struct Nullable<T> where T : struct { }
    public class MulticastDelegate : Delegate { }
    public class Delegate { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets validOn) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public enum AttributeTargets { All = 32767 }
    // NOTE: System.Index is intentionally omitted to simulate netstandard2.0
}
namespace System.Collections
{
    public interface IEnumerable { }
}
namespace System.Collections.Generic
{
    public interface IEnumerable<out T> : System.Collections.IEnumerable { }
    public class List<T> { }
    public class Dictionary<TKey, TValue> { }
}
namespace System.Globalization
{
    public class CultureInfo { }
}
namespace System.Reflection
{
    public abstract class MemberInfo { }
    public abstract class MethodBase : MemberInfo { }
    public class MethodInfo : MethodBase { }
    public class FieldInfo : MemberInfo { }
    public class PropertyInfo : MemberInfo { }
    public class ConstructorInfo : MethodBase { }
    public class EventInfo : MemberInfo { }
    public class ParameterInfo { }
}
";

        // Compile the minimal runtime to an in-memory assembly.
        var minimalCompilation = CSharpCompilation.Create(
                "MinimalRuntime",
                new[] { CSharpSyntaxTree.ParseText( minimalSource ) },
                Array.Empty<MetadataReference>(),
                new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary ) );

        using var ms = new MemoryStream();
        var emitResult = minimalCompilation.Emit( ms );

        // The minimal compilation may have warnings (missing System.Runtime.CompilerServices types, etc.)
        // but should emit successfully for our purposes. We only need the type metadata.
        if ( !emitResult.Success )
        {
            var errors = string.Join(
                Environment.NewLine,
                emitResult.Diagnostics.Where( d => d.Severity == DiagnosticSeverity.Error ).Select( d => d.ToString() ) );

            throw new InvalidOperationException( $"Failed to compile minimal runtime:{Environment.NewLine}{errors}" );
        }

        ms.Seek( 0, SeekOrigin.Begin );
        var minimalReference = MetadataReference.CreateFromStream( ms );

        // Create the test compilation referencing ONLY the minimal runtime (no System.Index).
        return CSharpCompilation.Create( "TestAssembly" )
            .WithOptions( new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary ) )
            .AddReferences( minimalReference )
            .AddSyntaxTrees( CSharpSyntaxTree.ParseText( "class C { }" ) );
    }

    [Fact]
    public void GetSerializableTypes_DoesNotThrow_WhenSystemIndexMissing()
    {
        // Arrange: Create a compilation without System.Index
        var compilation = CreateCompilationWithoutSystemIndex();

        // Verify precondition: System.Index should not be resolvable
        var indexType = compilation.GetTypeByMetadataName( "System.Index" );
        Assert.Null( indexType );

        // Act & Assert: GetSerializableTypes should not throw
        var compilationContext = new CompilationContext( compilation );
        var service = new SyntaxSerializationService();

        // This should gracefully handle the missing System.Index type
        // instead of throwing InvalidOperationException.
        var serializableTypes = service.GetSerializableTypes( compilationContext );
        Assert.NotNull( serializableTypes );
    }

    [Fact]
    public void GetSerializableTypes_StillIncludesPrimitiveTypes_WhenSystemIndexMissing()
    {
        // Arrange
        var compilation = CreateCompilationWithoutSystemIndex();

        // Verify precondition
        Assert.Null( compilation.GetTypeByMetadataName( "System.Index" ) );

        var compilationContext = new CompilationContext( compilation );
        var service = new SyntaxSerializationService();

        // Act
        var serializableTypes = service.GetSerializableTypes( compilationContext );

        // Assert: Primitive types should still be recognized as serializable
        var intType = compilation.GetSpecialType( SpecialType.System_Int32 );
        Assert.True( serializableTypes.IsSerializable( intType ) );
    }
}
