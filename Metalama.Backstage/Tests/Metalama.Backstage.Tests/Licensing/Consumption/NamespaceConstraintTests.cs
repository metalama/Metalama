// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing.Consumption;
using Xunit;

namespace Metalama.Backstage.Tests.Licensing.Consumption
{
    public class NamespaceConstraintTests
    {
        private static readonly NamespaceLicenseInfo _constraint = new( "Ns1.Ns2" );

        private static void AssertNamespaceConstraint( string? requestedNamespace, bool expectedAllowed )
        {
            Assert.Equal( expectedAllowed, _constraint.AllowsNamespace( requestedNamespace ) );
            Assert.Equal( expectedAllowed, _constraint.AllowsNamespace( requestedNamespace?.ToLowerInvariant() ) );
            Assert.Equal( expectedAllowed, _constraint.AllowsNamespace( requestedNamespace?.ToUpperInvariant() ) );
        }

        private static void AssertAllowed( string? requestedNamespace )
        {
            AssertNamespaceConstraint( requestedNamespace, true );
        }

        private static void AssertForbidden( string? requestedNamespace )
        {
            AssertNamespaceConstraint( requestedNamespace, false );
        }

        [Fact]
        public void NoNamespaceIsAlwaysAllowed()
        {
            AssertAllowed( null );
        }

        [Fact]
        public void OuterNamespaceIsForbidden()
        {
            AssertForbidden( "Ns1" );
        }

        [Fact]
        public void OuterFullAssemblyNameIsForbidden()
        {
            AssertForbidden( "Ns1, Versions..." );
        }

        [Fact]
        public void OuterNamespaceOfSameBeginningIsForbidden()
        {
            AssertForbidden( "Ns11" );
        }

        [Fact]
        public void OuterFullAssemblyNameOfSameBeginningIsForbidden()
        {
            AssertForbidden( "Ns11, Version..." );
        }

        [Fact]
        public void SameNamespaceIsAllowed()
        {
            AssertAllowed( "Ns1.Ns2" );
        }

        [Fact]
        public void SameFullAssemblyNameIsAllowed()
        {
            AssertAllowed( "Ns1.Ns2, Version..." );
        }

        [Fact]
        public void NamespaceOfSameBeginningIsForbidden()
        {
            AssertForbidden( "Ns1.Ns22" );
        }

        [Fact]
        public void FullAssemblyNameOfSameBeginningIsForbidden()
        {
            AssertForbidden( "Ns1.Ns22, Version..." );
        }

        [Fact]
        public void InnerNamespaceIsAllowed()
        {
            AssertAllowed( "Ns1.Ns2.Ns3" );
        }

        [Fact]
        public void InnerFullAssemblyNameIsAllowed()
        {
            AssertAllowed( "Ns1.Ns2.Ns3, Version..." );
        }

        [Fact]
        public void InnerNamespaceOfSameBeginningIsForbidden()
        {
            AssertForbidden( "Ns1.Ns22.Ns3" );
        }

        [Fact]
        public void InnerFullAssemblyNameOfSameBeginningIsForbidden()
        {
            AssertForbidden( "Ns1.Ns22.Ns3, Version..." );
        }
    }
}