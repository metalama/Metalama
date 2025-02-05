// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Testing.UnitTesting;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Templating
{
    public sealed class MetaApiTests : UnitTestClass
    {
        [Fact]
        public async Task OutOfContextAsync()
        {
            Assert.Throws<InvalidOperationException>( () => meta.Base );
            Assert.Throws<InvalidOperationException>( () => meta.Tags );
            Assert.Throws<InvalidOperationException>( () => meta.Target );
            Assert.Throws<InvalidOperationException>( () => meta.This );
            Assert.Throws<InvalidOperationException>( () => meta.BaseType );
            Assert.Throws<InvalidOperationException>( () => meta.ThisType );
            Assert.Throws<InvalidOperationException>( meta.Proceed );
            await Assert.ThrowsAsync<InvalidOperationException>( meta.ProceedAsync );
            Assert.Throws<InvalidOperationException>( meta.ProceedEnumerable );
            Assert.Throws<InvalidOperationException>( meta.ProceedEnumerator );

            Assert.Throws<InvalidOperationException>( () => meta.CompileTime( 0 ) );
            Assert.Throws<InvalidOperationException>( () => meta.RunTime( 0 ) );
            Assert.Throws<InvalidOperationException>( meta.DebugBreak );
            Assert.Throws<InvalidOperationException>( () => meta.InsertComment( "" ) );
            Assert.Throws<InvalidOperationException>( () => ExpressionFactory.Parse( "" ) );

#if NET5_0_OR_GREATER
            Assert.Throws<InvalidOperationException>( meta.ProceedAsyncEnumerable );
            Assert.Throws<InvalidOperationException>( meta.ProceedAsyncEnumerator );
#endif
        }
    }
}