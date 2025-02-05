// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel
{
    public sealed class AccessibilityTests
    {
        [Theory]
        [InlineData( Accessibility.Public, Accessibility.Internal, true )]
        [InlineData( Accessibility.Public, Accessibility.Protected, true )]
        [InlineData( Accessibility.Public, Accessibility.ProtectedInternal, true )]
        [InlineData( Accessibility.Public, Accessibility.PrivateProtected, true )]
        [InlineData( Accessibility.Public, Accessibility.Private, true )]
        [InlineData( Accessibility.Public, Accessibility.Public, false )]
        [InlineData( Accessibility.Protected, Accessibility.Internal, false )]
        [InlineData( Accessibility.Protected, Accessibility.Protected, false )]
        [InlineData( Accessibility.Protected, Accessibility.ProtectedInternal, false )]
        [InlineData( Accessibility.Protected, Accessibility.PrivateProtected, true )]
        [InlineData( Accessibility.Protected, Accessibility.Private, true )]
        [InlineData( Accessibility.Protected, Accessibility.Public, false )]
        [InlineData( Accessibility.Internal, Accessibility.Internal, false )]
        [InlineData( Accessibility.Internal, Accessibility.Protected, false )]
        [InlineData( Accessibility.Internal, Accessibility.ProtectedInternal, false )]
        [InlineData( Accessibility.Internal, Accessibility.PrivateProtected, true )]
        [InlineData( Accessibility.Internal, Accessibility.Private, true )]
        [InlineData( Accessibility.Internal, Accessibility.Public, false )]
        [InlineData( Accessibility.ProtectedInternal, Accessibility.Internal, true )]
        [InlineData( Accessibility.ProtectedInternal, Accessibility.Protected, true )]
        [InlineData( Accessibility.ProtectedInternal, Accessibility.ProtectedInternal, false )]
        [InlineData( Accessibility.ProtectedInternal, Accessibility.PrivateProtected, true )]
        [InlineData( Accessibility.ProtectedInternal, Accessibility.Private, true )]
        [InlineData( Accessibility.ProtectedInternal, Accessibility.Public, false )]
        [InlineData( Accessibility.PrivateProtected, Accessibility.Internal, false )]
        [InlineData( Accessibility.PrivateProtected, Accessibility.Protected, false )]
        [InlineData( Accessibility.PrivateProtected, Accessibility.ProtectedInternal, false )]
        [InlineData( Accessibility.PrivateProtected, Accessibility.PrivateProtected, false )]
        [InlineData( Accessibility.PrivateProtected, Accessibility.Private, true )]
        [InlineData( Accessibility.PrivateProtected, Accessibility.Public, false )]
        [InlineData( Accessibility.Private, Accessibility.Internal, false )]
        [InlineData( Accessibility.Private, Accessibility.Protected, false )]
        [InlineData( Accessibility.Private, Accessibility.ProtectedInternal, false )]
        [InlineData( Accessibility.Private, Accessibility.PrivateProtected, false )]
        [InlineData( Accessibility.Private, Accessibility.Private, false )]
        [InlineData( Accessibility.Private, Accessibility.Public, false )]
        public void TestSuperset( Accessibility right, Accessibility left, bool result )
        {
            Assert.Equal( result, right.IsSupersetOf( left ) );
            Assert.Equal( right == left || result, right.IsSupersetOrEqual( left ) );
        }

        [Theory]
        [InlineData( Accessibility.Public, Accessibility.Internal, false )]
        [InlineData( Accessibility.Public, Accessibility.Protected, false )]
        [InlineData( Accessibility.Public, Accessibility.ProtectedInternal, false )]
        [InlineData( Accessibility.Public, Accessibility.PrivateProtected, false )]
        [InlineData( Accessibility.Public, Accessibility.Private, false )]
        [InlineData( Accessibility.Public, Accessibility.Public, false )]
        [InlineData( Accessibility.Protected, Accessibility.Internal, false )]
        [InlineData( Accessibility.Protected, Accessibility.Protected, false )]
        [InlineData( Accessibility.Protected, Accessibility.ProtectedInternal, true )]
        [InlineData( Accessibility.Protected, Accessibility.PrivateProtected, false )]
        [InlineData( Accessibility.Protected, Accessibility.Private, false )]
        [InlineData( Accessibility.Protected, Accessibility.Public, true )]
        [InlineData( Accessibility.Internal, Accessibility.Internal, false )]
        [InlineData( Accessibility.Internal, Accessibility.Protected, false )]
        [InlineData( Accessibility.Internal, Accessibility.ProtectedInternal, true )]
        [InlineData( Accessibility.Internal, Accessibility.PrivateProtected, false )]
        [InlineData( Accessibility.Internal, Accessibility.Private, false )]
        [InlineData( Accessibility.Internal, Accessibility.Public, true )]
        [InlineData( Accessibility.ProtectedInternal, Accessibility.Internal, false )]
        [InlineData( Accessibility.ProtectedInternal, Accessibility.Protected, false )]
        [InlineData( Accessibility.ProtectedInternal, Accessibility.ProtectedInternal, false )]
        [InlineData( Accessibility.ProtectedInternal, Accessibility.PrivateProtected, false )]
        [InlineData( Accessibility.ProtectedInternal, Accessibility.Private, false )]
        [InlineData( Accessibility.ProtectedInternal, Accessibility.Public, true )]
        [InlineData( Accessibility.PrivateProtected, Accessibility.Internal, true )]
        [InlineData( Accessibility.PrivateProtected, Accessibility.Protected, true )]
        [InlineData( Accessibility.PrivateProtected, Accessibility.ProtectedInternal, true )]
        [InlineData( Accessibility.PrivateProtected, Accessibility.PrivateProtected, false )]
        [InlineData( Accessibility.PrivateProtected, Accessibility.Private, false )]
        [InlineData( Accessibility.PrivateProtected, Accessibility.Public, true )]
        [InlineData( Accessibility.Private, Accessibility.Internal, true )]
        [InlineData( Accessibility.Private, Accessibility.Protected, true )]
        [InlineData( Accessibility.Private, Accessibility.ProtectedInternal, true )]
        [InlineData( Accessibility.Private, Accessibility.PrivateProtected, true )]
        [InlineData( Accessibility.Private, Accessibility.Private, false )]
        [InlineData( Accessibility.Private, Accessibility.Public, true )]
        public void TestSubset( Accessibility right, Accessibility left, bool result )
        {
            Assert.Equal( result, right.IsSubsetOf( left ) );
            Assert.Equal( right == left || result, right.IsSubsetOrEqual( left ) );
        }
    }
}