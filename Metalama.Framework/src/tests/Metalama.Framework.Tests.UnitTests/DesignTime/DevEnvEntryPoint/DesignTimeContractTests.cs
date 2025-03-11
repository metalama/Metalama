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

    [Fact]
    public void TypesAreEquivalent()
    {
        var mainAssembly = typeof(DesignTimeEntryPointManager).Assembly;

        Assert.NotSame( mainAssembly, _loadFileAssembly );

        foreach ( var type in _loadFileAssembly.GetTypes() )
        {
            if ( (type.IsInterface || type.IsValueType) && !type.Namespace!.StartsWith( "System", StringComparison.Ordinal )
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