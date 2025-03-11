// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Diagnostics;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using System.Collections.Immutable;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.UserProfile
{
    public sealed class UserProfileTests
    {
        [Fact]
        public void DiagnosticUserProfileSerialization()
        {
            var originalDiagnostic = new UserDiagnosticRegistration( "MY001", DiagnosticSeverity.Error, "Category", "Title" );

            var file = new UserDiagnosticsConfiguration
            {
                Diagnostics = ImmutableDictionary<string, UserDiagnosticRegistration>.Empty.Add( "MY001", originalDiagnostic ),
                Suppressions = ImmutableHashSet.Create( "MY001" )
            };

            var json = JsonConvert.SerializeObject( file );
            var roundtrip = JsonConvert.DeserializeObject<UserDiagnosticsConfiguration>( json )!;

            Assert.Contains( "MY001", roundtrip.Suppressions );
            Assert.Contains( "MY001", roundtrip.Diagnostics.Keys );
        }
    }
}