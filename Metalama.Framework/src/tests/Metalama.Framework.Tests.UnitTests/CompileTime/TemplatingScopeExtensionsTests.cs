// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime
{
    public sealed class TemplatingScopeExtensionsTests
    {
        /// <summary>
        /// Regression test for https://github.com/metalama/Metalama/issues/701.
        /// Verifies that ToExecutionScope does not throw for any TemplatingScope value.
        /// </summary>
        [Fact]
        public void ToExecutionScope_DoesNotThrow()
        {
            var allValues = Enum.GetValues( typeof(TemplatingScope) ).Cast<TemplatingScope>();
            var failures = new List<string>();

            foreach ( var scope in allValues )
            {
                var exception = Record.Exception( () => scope.ToExecutionScope() );

                if ( exception != null )
                {
                    failures.Add( $"{scope}: {exception.Message}" );
                }
            }

            Assert.Empty( failures );
        }
    }
}
