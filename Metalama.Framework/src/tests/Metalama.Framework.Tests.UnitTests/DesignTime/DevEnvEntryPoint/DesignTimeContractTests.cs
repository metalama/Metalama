// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if NETFRAMEWORK
using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using Metalama.Testing.UnitTesting;
using System;
using System.IO;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.DevEnvEntryPoint;

public sealed class DesignTimeContractTests : UnitTestClass
{
    private static readonly Assembly _loadFileAssembly = Assembly.Load( File.ReadAllBytes( typeof(DesignTimeEntryPointManager).Assembly.Location ) );

    public DesignTimeContractTests( ITestOutputHelper? testOutputHelper = null ) : base( testOutputHelper ) { }

    /// <summary>
    /// Verifies that every type of the design-time contract keeps the same identity when the assembly is loaded
    /// twice, which is what allows two versions of Metalama loaded side by side to exchange these types.
    /// </summary>
    /// <remarks>
    /// Only the public types are examined. A type that is not visible outside this assembly cannot take part in an
    /// exchange between two versions, so equivalence is meaningless for it, and some of the internal types must
    /// deliberately not be equivalent: the assembly compiles the named-lock implementation from shared source, and
    /// each version is required to use its own copy of it rather than the copy of whichever version happened to
    /// load first. Restricting the examination to the public types leaves the guard at full strength, because a
    /// contract type that lacked its <see cref="System.Runtime.InteropServices.ComImportAttribute"/> and
    /// <see cref="System.Runtime.InteropServices.GuidAttribute"/> would still be caught here.
    /// </remarks>
    [Fact]
    public void TypesAreEquivalent()
    {
        var mainAssembly = typeof(DesignTimeEntryPointManager).Assembly;

        Assert.NotSame( mainAssembly, _loadFileAssembly );

        foreach ( var type in _loadFileAssembly.GetTypes() )
        {
            if ( (type.IsInterface || type.IsValueType) && type.IsPublic
                                                        && !type.Namespace!.StartsWith( "System", StringComparison.Ordinal )
                                                        && type.DeclaringType == null )
            {
                this.TestOutput.WriteLine( type.FullName );

                var otherType = mainAssembly.GetType( type.FullName! );
                Assert.NotSame( type, otherType );
                Assert.True( type.IsEquivalentTo( otherType ), $"The type equivalence for '{type}' is broken." );
            }
        }
    }
}
#endif