// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Metalama.Testing.AspectTesting;
// This whole file is temporary before we have FormatOutput in TestOptions properly controlling FormatOutput in ProjectOptions.
// Additionally rewriting the test input before running the aspect pipeline test runner does not seem to be necessary.

internal sealed class OutputFormatterAspectTestRunner : AspectTestRunner
{
    public OutputFormatterAspectTestRunner(
        GlobalServiceProvider serviceProvider,
        string? projectDirectory,
        TestProjectReferences references,
        ITestOutputHelper? logger )
        : base(
            serviceProvider,
            projectDirectory,
            references,
            logger ) { }

    protected override TestContextOptions GetContextOptions( TestContextOptions options )
        => options with { CodeFormattingOptions = CodeFormattingOptions.Formatted };

    protected override async Task RunAsync(
        TestInput testInput,
        TestResult testResult,
        TestContext projectOptions )
    {
        var expectedEol =
            testInput.Options.ExpectedEndOfLine switch
            {
                null => null,
                "CR" => "\r",
                "LF" => "\n",
                "CRLF" => "\r\n",
                _ => throw new AssertionFailedException( $"Unexpected value for the ExpectedEndOfLine test option: '{testInput.Options.ExpectedEndOfLine}'." )
            };

        // If we have an expected EOL, change the EOLs of the input.
        if ( expectedEol != null )
        {
            var sb = new StringBuilder();

            for ( var i = 0; i < testInput.SourceCode.Length; i++ )
            {
                var current = testInput.SourceCode[i];
                var next = i < testInput.SourceCode.Length - 1 ? testInput.SourceCode[i + 1] : (char?) null;

                switch (current, next)
                {
                    case ('\r', '\n'):
                        sb.Append( expectedEol );
                        i++;

                        break;

                    case ('\r', _):
                    case ('\n', _):
                        sb.Append( expectedEol );

                        break;

                    default:
                        sb.Append( current );

                        break;
                }
            }

            testInput = testInput.WithSource( sb.ToString() );
        }

        // Run the sample.
        await base.RunAsync( testInput, testResult, projectOptions );

        // If we have an expected EOL, verify that EOLs are preserved in the output document.
        if ( expectedEol != null && testResult.OutputProject != null )
        {
            foreach ( var sourceDocument in testResult.OutputProject.Documents )
            {
                var outputSource = (await sourceDocument.GetTextAsync()).ToString();

                var sourceSoFar = new StringBuilder();

                for ( var i = 0; i < outputSource.Length; i++ )
                {
                    var current = outputSource[i];
                    sourceSoFar.Append( current );

                    var next = i < outputSource.Length - 1 ? outputSource[i + 1] : (char?) null;

                    static string MapEolToString( string value )
                    {
                        return value switch
                        {
                            "\r\n" => "\\r\\n",
                            "\r" => "\\r",
                            "\n" => "\\n",
                            _ => throw new AssertionFailedException( $"Unexpected EOL value: '{string.Join( " ", value.Select( x => (int) x ) )}'" )
                        };
                    }

                    var error = false;

                    switch (current, next)
                    {
                        case ('\r', '\n'):
                            if ( expectedEol != "\r\n" )
                            {
                                error = true;

                                testResult.SetFailed(
                                    $"ERROR: Expected \"{MapEolToString( expectedEol )}\" end of lines, but got \"\\r\\n\" at position {i}." );
                            }

                            i++;

                            break;

                        case ('\r', _):
                        case ('\n', _):
                            if ( expectedEol.Length > 1 || expectedEol[0] != current )
                            {
                                error = true;

                                testResult.SetFailed(
                                    $"ERROR: Expected \"{MapEolToString( expectedEol )}\" end of lines, but got \"{MapEolToString( $"{current}" )}\" at position {i}." );
                            }

                            break;
                    }

                    if ( error )
                    {
                        break;
                    }
                }
            }
        }
    }
}