// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel
{
    /// <summary>
    /// Tests of <see cref="MemberExtensions.CanBeDeclaredExplicitly"/>.
    /// </summary>
    public sealed class CanBeDeclaredExplicitlyTests : UnitTestClass
    {
        private const string _code = @"
record BaseRecord(int X);

record DerivedRecord(int X, int Y) : BaseRecord(X);

record struct RecordStruct(int X);

class PlainClass
{
    public int P { get; set; }

    public void M() { }
}
";

        [Fact]
        public void RecordMembersThatTheCompilerOmitsWhenDeclared()
        {
            using var testContext = this.CreateTestContext();

            var compilation = testContext.CreateCompilationModel( _code );
            var record = compilation.Types.OfName( "BaseRecord" ).Single();

            Assert.True( GetTypedEquals( record ).CanBeDeclaredExplicitly() );
            Assert.True( record.Methods.OfName( "GetHashCode" ).Single().CanBeDeclaredExplicitly() );
            Assert.True( record.Methods.OfName( "ToString" ).Single().CanBeDeclaredExplicitly() );
            Assert.True( record.Methods.OfName( "PrintMembers" ).Single().CanBeDeclaredExplicitly() );
            Assert.True( record.Methods.OfName( "Deconstruct" ).Single().CanBeDeclaredExplicitly() );
            Assert.True( record.Properties.OfName( "EqualityContract" ).Single().CanBeDeclaredExplicitly() );
            Assert.True( record.Constructors.Single( c => c.IsRecordCopyConstructor() ).CanBeDeclaredExplicitly() );
        }

        [Fact]
        public void RecordMembersThatTheCompilerAlwaysAdds()
        {
            using var testContext = this.CreateTestContext();

            var compilation = testContext.CreateCompilationModel( _code );
            var baseRecord = compilation.Types.OfName( "BaseRecord" ).Single();
            var derivedRecord = compilation.Types.OfName( "DerivedRecord" ).Single();

            Assert.False( GetUntypedEquals( baseRecord ).CanBeDeclaredExplicitly() );
            Assert.False( GetOperator( baseRecord, OperatorKind.Equality ).CanBeDeclaredExplicitly() );
            Assert.False( GetOperator( baseRecord, OperatorKind.Inequality ).CanBeDeclaredExplicitly() );

            // The Equals overload of the derived record whose parameter is the base record.
            var baseEquals = derivedRecord.Methods.OfName( "Equals" )
                .Single( m => m.Parameters[0].Type.Equals( baseRecord, TypeComparison.Default ) );

            Assert.False( baseEquals.CanBeDeclaredExplicitly() );
        }

        [Fact]
        public void RecordStructMembers()
        {
            using var testContext = this.CreateTestContext();

            var compilation = testContext.CreateCompilationModel( _code );
            var recordStruct = compilation.Types.OfName( "RecordStruct" ).Single();

            Assert.True( GetTypedEquals( recordStruct ).CanBeDeclaredExplicitly() );
            Assert.True( recordStruct.Methods.OfName( "GetHashCode" ).Single().CanBeDeclaredExplicitly() );
            Assert.False( GetUntypedEquals( recordStruct ).CanBeDeclaredExplicitly() );
            Assert.False( GetOperator( recordStruct, OperatorKind.Equality ).CanBeDeclaredExplicitly() );
            Assert.False( GetOperator( recordStruct, OperatorKind.Inequality ).CanBeDeclaredExplicitly() );
        }

        [Fact]
        public void MembersOfNonRecordType()
        {
            using var testContext = this.CreateTestContext();

            var compilation = testContext.CreateCompilationModel( _code );
            var type = compilation.Types.OfName( "PlainClass" ).Single();
            var property = type.Properties.OfName( "P" ).Single();
            var backingField = type.Fields.Single( f => f.IsAutoPropertyBackingField() );

            Assert.True( type.Methods.OfName( "M" ).Single().CanBeDeclaredExplicitly() );
            Assert.True( property.CanBeDeclaredExplicitly() );
            Assert.All( property.Accessors, a => Assert.True( a.CanBeDeclaredExplicitly() ) );

            // The name of a backing field is not a valid C# identifier, so neither the field nor its accessors
            // can be declared explicitly.
            Assert.False( backingField.CanBeDeclaredExplicitly() );
            Assert.All( backingField.Accessors, a => Assert.False( a.CanBeDeclaredExplicitly() ) );
        }

        /// <summary>
        /// Gets the <c>Equals</c> overload whose parameter is the declaring record.
        /// </summary>
        private static IMethod GetTypedEquals( INamedType record )
            => record.Methods.OfName( "Equals" ).Single( m => m.Parameters[0].Type.Equals( record, TypeComparison.Default ) );

        /// <summary>
        /// Gets the <c>Equals</c> overload whose parameter is <see cref="object"/>.
        /// </summary>
        private static IMethod GetUntypedEquals( INamedType record )
            => record.Methods.OfName( "Equals" ).Single( m => m.Parameters[0].Type.SpecialType == SpecialType.Object );

        /// <summary>
        /// Gets an operator of the given kind.
        /// </summary>
        private static IMethod GetOperator( INamedType record, OperatorKind operatorKind )
            => record.Methods.Single( m => m.OperatorKind == operatorKind );
    }
}
