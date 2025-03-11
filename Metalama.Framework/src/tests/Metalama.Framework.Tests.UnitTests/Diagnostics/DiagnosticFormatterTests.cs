// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Utilities;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Diagnostics
{
    public sealed class DiagnosticFormatterTests
    {
        [Theory]
        [InlineData( 1, "1" )]
        [InlineData( DeclarationKind.Attribute, "attribute" )]
        [InlineData( DeclarationKind.TypeParameter, "generic parameter" )]
        [InlineData( DeclarationKind.ManagedResource, "managed resource" )]
        [InlineData( DeclarationKind.AssemblyReference, "assembly reference" )]
        [InlineData( Accessibility.Private, "private" )]
        [InlineData( Accessibility.Internal, "internal" )]
        [InlineData( Accessibility.Protected, "protected" )]
        [InlineData( Accessibility.Public, "public" )]
        [InlineData( Accessibility.PrivateProtected, "private protected" )]
        [InlineData( Accessibility.ProtectedInternal, "protected internal" )]
        [InlineData( new[] { "a", "b" }, "'a', 'b'" )]
        [InlineData( new[] { 1, 2 }, "1, 2" )]
        public void Format( object value, string expected )
        {
            MetalamaEngineModuleInitializer.EnsureInitialized();
            var formatter = MetalamaStringFormatter.Instance;
            Assert.Equal( expected, formatter.Format( "", value, formatter ) );
        }
    }
}