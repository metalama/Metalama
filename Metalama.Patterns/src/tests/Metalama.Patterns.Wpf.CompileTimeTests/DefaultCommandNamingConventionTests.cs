// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using FluentAssertions;
using Metalama.Patterns.Wpf.Implementation.CommandNamingConvention;
using Xunit;

namespace Metalama.Patterns.Wpf.CompileTimeTests;

public sealed class DefaultCommandNamingConventionTests
{
    [InlineData( "execute", "execute" )]
    [InlineData( "m_execute", "execute" )]
    [InlineData( "m_", "m_" )]
    [InlineData( "_", "_" )]
    [InlineData( "Command", "Command" )]
    [InlineData( "_command", "command" )]
    [InlineData( "executeFoo", "Foo" )]
    [InlineData( "ExecuteFoo", "Foo" )]
    [InlineData( "m_foo", "Foo" )]
    [InlineData( "m_executeFoo", "Foo" )]
    [InlineData( "Foo", "Foo" )]
    [InlineData( "foo", "Foo" )]
    [InlineData( "FooCommand", "Foo" )]
    [InlineData( "Foo_Command", "Foo" )]
    [InlineData( "foo_command", "Foo" )]
    [InlineData( "m_execute_foo_command", "Foo" )]
    [Theory]
    public void GetCommandNameFromExecuteMethodName( string methodName, string expectedCommandName )
    {
        DefaultCommandNamingConvention.GetCommandNameFromExecuteMethodName( methodName ).Should().Be( expectedCommandName );
    }

    [Fact]
    public void GetCommandPropertyNameFromCommandName()
    {
        DefaultCommandNamingConvention.GetCommandPropertyNameFromCommandName( "Foo" ).Should().Be( "FooCommand" );
    }

    [Fact]
    public void GetCanExecuteNameFromCommandName()
    {
        DefaultCommandNamingConvention.GetCanExecuteNameFromCommandName( "Foo" ).Should().Contain( "CanExecuteFoo" );
    }
}