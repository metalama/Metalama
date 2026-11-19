// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.LinqPad.Tests.Assets;
using Metalama.Testing.UnitTesting;
using System;
using System.Linq;
using Xunit;

namespace Metalama.LinqPad.Tests
{
    public sealed class FacadeObjectTests : UnitTestClass
    {
        static FacadeObjectTests()
        {
            Initializer.Initialize();
        }

        private static readonly FacadeObjectFactory _facadeObjectFactory = new( publicAssemblies: new[] { typeof(Impl).Assembly } );

        private static object? DumpClass<T>( T? obj )
            where T : class
        {
            var dump = _facadeObjectFactory.GetFacade( obj );

            if ( obj != null )
            {
                Assert.NotNull( dump );
            }
            else
            {
                Assert.Null( dump );
            }

            return dump;
        }

        private static object? DumpStruct<T>( T obj )
            where T : struct
            => _facadeObjectFactory.GetFacade( obj );

        [Fact]
        public void Tests()
        {
            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilation( "class C {}" );

            Assert.NotNull( DumpClass( compilation.Project ) );
            Assert.Null( DumpStruct( compilation.Project.AssemblyReferences ) );
            Assert.NotNull( DumpClass( compilation.Project.AssemblyReferences[0] ) );
            Assert.Null( DumpStruct( compilation.Project.AssemblyReferences[0].PublicKey ) );
        }

        [Fact]
        public void InheritedInterfacePropertiesAreAvailable()
        {
            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilation( "class C {}" );

            var type = _facadeObjectFactory.GetFormatterType( compilation.Types.Single().GetType() );
            Assert.Contains( "Methods", type.PropertyNames );
            Assert.Contains( "DeclaringAssembly", type.PropertyNames );
            Assert.DoesNotContain( "CanBeInherited", type.PropertyNames );
        }

        [Fact]
        public void ValueTupleTest()
        {
            var type = _facadeObjectFactory.GetFormatterType( typeof(ValueTuple<int, string>) );
            Assert.Contains( "Item1", type.PropertyNames );
            Assert.Contains( "Item2", type.PropertyNames );
        }

        [Fact]
        public void AnonymousTypeTest()
        {
            var o = new { Id = 1, Name = "name" };
            var type = _facadeObjectFactory.GetFormatterType( o.GetType() );
            Assert.Contains( "Id", type.PropertyNames );
            Assert.Contains( "Name", type.PropertyNames );
        }

        [Fact]
        public void PropertyHiding()
        {
            var type = _facadeObjectFactory.GetFormatterType( typeof(Impl) );
            Assert.Single( type.PropertyTypes );
            var propertyType = type.PropertyTypes.First();
            Assert.Equal( typeof(string), propertyType );
        }
    }
}