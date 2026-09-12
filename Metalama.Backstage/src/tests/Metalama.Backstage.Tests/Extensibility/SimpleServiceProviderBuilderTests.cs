// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using System;
using System.Collections.Generic;
using Xunit;

namespace Metalama.Backstage.Tests.Extensibility;

/// <summary>
/// Tests of the ownership rules of <see cref="SimpleServiceProviderBuilder"/>: the provider disposes the services it
/// created, in the reverse order of their creation, and leaves the supplied instances alone.
/// </summary>
public sealed class SimpleServiceProviderBuilderTests
{
    private sealed class DisposableService : IBackstageService, IDisposable
    {
        private readonly List<string> _log;
        private readonly string _name;

        public DisposableService( List<string> log, string name )
        {
            this._log = log;
            this._name = name;
        }

        public void Dispose() => this._log.Add( this._name );
    }

    private sealed class FirstService : IBackstageService;

    private sealed class SecondService : IBackstageService;

    [Fact]
    public void CreatedServicesAreDisposedInReverseOrderAndSuppliedInstancesAreNot()
    {
        var log = new List<string>();
        var builder = new SimpleServiceProviderBuilder();

        builder.AddService( typeof(FirstService), _ => new DisposableService( log, "first" ) );
        builder.AddService( typeof(SecondService), _ => new DisposableService( log, "second" ) );
        builder.AddService( typeof(DisposableService), new DisposableService( log, "supplied" ) );

        var serviceProvider = builder.ServiceProvider;

        // The services are created in the order of resolution, not of registration.
        Assert.NotNull( serviceProvider.GetService( typeof(SecondService) ) );
        Assert.NotNull( serviceProvider.GetService( typeof(FirstService) ) );
        Assert.NotNull( serviceProvider.GetService( typeof(DisposableService) ) );

        ((IDisposable) serviceProvider).Dispose();

        Assert.Equal( ["first", "second"], log );

        // Disposing twice is harmless.
        ((IDisposable) serviceProvider).Dispose();
        Assert.Equal( 2, log.Count );
    }

    [Fact]
    public void ServiceIsCreatedOnce()
    {
        var builder = new SimpleServiceProviderBuilder();
        var creations = 0;
        builder.AddService( typeof(FirstService), _ => { creations++; return new FirstService(); } );

        var serviceProvider = builder.ServiceProvider;

        Assert.Same( serviceProvider.GetService( typeof(FirstService) ), serviceProvider.GetService( typeof(FirstService) ) );
        Assert.Equal( 1, creations );
        Assert.Null( serviceProvider.GetService( typeof(SecondService) ) );
    }
}
