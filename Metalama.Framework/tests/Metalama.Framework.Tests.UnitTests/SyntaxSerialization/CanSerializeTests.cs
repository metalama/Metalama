// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Diagnostics;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization
{
    public sealed class CanSerializeTests : SerializerTestsBase
    {
        private void AssertCanSerialize( bool expected, Type type )
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            var serializableTypes = testContext.SerializationService.GetSerializableTypes( testContext.SerializationContext.CompilationContext );
            var diagnosticList = new DiagnosticBag();
            var result = serializableTypes.IsSerializable( testContext.SerializationContext.GetTypeSymbol( type ), Location.None, diagnosticList );

            Assert.Equal( expected, result );

            if ( !result )
            {
                Assert.NotEmpty( diagnosticList );
            }
            else
            {
                Assert.Empty( diagnosticList );
            }
        }

        [Fact]
        public void Serializable()
        {
            this.AssertCanSerialize( true, typeof(int) );
            this.AssertCanSerialize( true, typeof(Guid) );
            this.AssertCanSerialize( true, typeof(DateTime) );
            this.AssertCanSerialize( true, typeof(MethodInfo) );
            this.AssertCanSerialize( true, typeof(MethodBase) );
        }

        [Fact]
        public void Nullable()
        {
            this.AssertCanSerialize( true, typeof(int?) );
        }

        [Fact]
        public void Generic()
        {
            this.AssertCanSerialize( true, typeof(IEnumerable<int>) );
            this.AssertCanSerialize( true, typeof(List<int>) );
            this.AssertCanSerialize( true, typeof(List<List<int>>) );
            this.AssertCanSerialize( true, typeof(List<Guid?>) );
        }

        [Fact]
        public void NonSerializable()
        {
            this.AssertCanSerialize( false, typeof(AppDomain) );
            this.AssertCanSerialize( false, typeof(IEnumerable<AppDomain>) );
            this.AssertCanSerialize( false, typeof(Queue<>) );
        }
    }
}