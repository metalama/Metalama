// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Commands;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Testing;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Tools.Config.Tests.Commands
{
    public abstract class CommandsTestsBase : TestsBase, ICommandServiceProviderProvider
    {
        private readonly ILogger _logger;

        protected CommandsTestsBase( ITestOutputHelper logger )
            : base( logger )
        {
            this._logger = this.ServiceProvider.GetLoggerFactory().GetLogger( "Console" );
        }

        protected Task TestCommandAsync(
            string commandLine,
            string? expectedOutput = null,
            int expectedExitCode = 0 )
            => this.TestCommandAsync( commandLine.Split( ' ' ), expectedOutput, expectedExitCode );

        protected async Task TestCommandAsync(
            string[] commandLine,
            string? expectedOutput = null,
            int expectedExitCode = 0 )
        {
            var output = new StringWriter();

            this.Log.MessageReported += output.WriteLine;

            this._logger.Trace?.Log( $">> {string.Join( " ", commandLine )}" );

            var commandApp = new CommandApp();
            BackstageCommandFactory.ConfigureCommandApp( commandApp, new BackstageCommandOptions( this, output, output, AnsiSupport.No ) );
            var exitCode = await commandApp.RunAsync( commandLine );

            if ( expectedOutput != null )
            {
                Assert.Contains( expectedOutput, output.ToString(), StringComparison.OrdinalIgnoreCase );
            }

            Assert.Equal( expectedExitCode, exitCode );
            this.Log.Clear();
        }

        IServiceProvider ICommandServiceProviderProvider.GetServiceProvider( CommandServiceProviderArgs args ) => this.ServiceProvider;
    }
}