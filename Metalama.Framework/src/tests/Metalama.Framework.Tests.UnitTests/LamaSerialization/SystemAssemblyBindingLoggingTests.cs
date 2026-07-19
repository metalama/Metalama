// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Testing;
using Metalama.Framework.Engine.Services;
using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.LamaSerialization
{
    /// <summary>
    /// Tests that binding a system (corlib) type during compile-time deserialization does not log a spurious
    /// "is not a known assembly name" warning (issue #1732).
    /// </summary>
    /// <remarks>
    /// Under .NET Core, <c>CompileTimeSerializationBinder.BindToType</c> rewrites the corlib simple name
    /// (<c>mscorlib</c> / <c>System.Private.CoreLib</c>) to a full assembly name before delegating to the base binder,
    /// whose known-assembly dictionary is keyed by simple name. The base binder suppresses the resulting mismatch warning
    /// for the .NET Framework corlib (<c>mscorlib, </c>) but not for the .NET Core corlib (<c>System.Private.CoreLib, </c>),
    /// so a warning was logged once per distinct system type bound during deserialization.
    /// </remarks>
    public sealed class SystemAssemblyBindingLoggingTests : SerializationTestsBase
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public SystemAssemblyBindingLoggingTests( ITestOutputHelper testOutputHelper )
        {
            this._testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void BindingSystemType_DoesNotLogUnknownAssemblyWarning()
        {
            var loggerFactory = new TestLoggerFactory( this._testOutputHelper );

            var services = CreateAdditionalServiceCollection();
            ((AdditionalServiceCollection) services).BackstageServices.Add( _ => loggerFactory );

            using ( var testContext = (SerializationTestContext) this.CreateTestContext( new SerializationTestContextOptions(), services ) )
            {
                // Round-trip System.Type values that reference corlib types. Deserialization resolves each referenced
                // type through the binder, which is where the spurious warning was produced under .NET Core.
                SerializeDeserialize( typeof(DateTime), testContext );
                SerializeDeserialize( typeof(int), testContext );
                SerializeDeserialize( typeof(object), testContext );
            }

            var unknownAssemblyWarnings = loggerFactory.Entries
                .Where( e => e.Severity == TestLoggerFactory.Severity.Warning && e.Message.Contains( "is not a known assembly name" ) )
                .Select( e => e.Message )
                .ToList();

            Assert.Empty( unknownAssemblyWarnings );
        }
    }
}
