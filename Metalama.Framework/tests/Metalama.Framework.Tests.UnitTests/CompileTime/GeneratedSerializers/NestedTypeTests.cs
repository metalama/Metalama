// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime.GeneratedSerializers
{
    public sealed class NestedTypeTests : SerializerTestBase
    {
        [Fact]
        public void FabricNestedInRuntimeType()
        {
            // Verifies that serializable nested type fabric can be serialized and deserialized.
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Fabrics;

public class A
{
    [CompileTime]
    public class B : TypeFabric
    {
        public int Field;
        public int Property { get; set; }

        public override void AmendType( ITypeAmender amender )
        {
        }
    }
}
";

            using var testContext = this.CreateTestContext();
            var domain = testContext.Domain;

            var project = CreateCompileTimeProject( domain, testContext, code );

            var type = project.GetType( "A_B" );
            var lamaSerializer = GetSerializer( type );

            dynamic instance = Activator.CreateInstance( type )!;
            type.GetField( "Field" )!.SetValue( instance, 13 );
            type.GetProperty( "Property" )!.SetValue( instance, 42 );

            var constructorArgumentsWriter = new TestArgumentsWriter();
            var initializationArgumentsWriter = new TestArgumentsWriter();
            lamaSerializer.SerializeObject( instance, constructorArgumentsWriter, initializationArgumentsWriter );

            var constructorArgumentsReader = constructorArgumentsWriter.ToReader();
            var initializationArgumentsReader = initializationArgumentsWriter.ToReader();

            dynamic deserialized = lamaSerializer.CreateInstance( type, constructorArgumentsReader );
            lamaSerializer.DeserializeFields( ref deserialized, initializationArgumentsReader );

            Assert.Equal( 13, type.GetField( "Field" )!.GetValue( instance ) );
            Assert.Equal( 42, type.GetProperty( "Property" )!.GetValue( instance ) );
        }
    }
}