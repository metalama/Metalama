// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Testing.AspectTesting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Metalama.AspectWorkbench.Model
{
    internal static class TestSerializer
    {
        private static string GetExpectedTransformedCodeFilePath( string testFilePath ) => Path.ChangeExtension( testFilePath, FileExtensions.TransformedCode );

        private static string GetExpectedProgramOutputFilePath( string testFilePath ) => Path.ChangeExtension( testFilePath, FileExtensions.ProgramOutput );

        public static async Task<TemplateTest> LoadFromFileAsync( TestProjectProperties projectProperties, string filePath )
        {
            var testSource = await File.ReadAllTextAsync( filePath );

            var expectedTransformedCodeFilePath = GetExpectedTransformedCodeFilePath( filePath );
            string? expectedTransformedCode = null;

            if ( File.Exists( expectedTransformedCodeFilePath ) )
            {
                expectedTransformedCode = await File.ReadAllTextAsync( expectedTransformedCodeFilePath );
            }

            var expectedProgramOutputFilePath = GetExpectedProgramOutputFilePath( filePath );
            string? expectedProgramOutput = null;

            if ( File.Exists( expectedProgramOutputFilePath ) )
            {
                expectedProgramOutput = await File.ReadAllTextAsync( expectedProgramOutputFilePath );
            }

            return new TemplateTest
            {
                Input = TestInput.Factory.Default.FromSource( projectProperties, testSource, filePath ),
                ExpectedTransformedCode = expectedTransformedCode,
                ExpectedProgramOutput = expectedProgramOutput
            };
        }

        public static async Task SaveToFileAsync( TemplateTest test, string filePath )
        {
            if ( test.Input == null )
            {
                throw new InvalidOperationException( "Test input not set." );
            }

            await File.WriteAllTextAsync( filePath, test.Input.SourceCode );

            var expectedTransformedCodeFilePath = GetExpectedTransformedCodeFilePath( filePath );
            await File.WriteAllTextAsync( expectedTransformedCodeFilePath, test.ExpectedTransformedCode );

            var expectedProgramOutputFilePath = GetExpectedProgramOutputFilePath( filePath );

            if ( !string.IsNullOrWhiteSpace( test.ExpectedProgramOutput ) )
            {
                await File.WriteAllTextAsync( expectedProgramOutputFilePath, test.ExpectedProgramOutput );
            }
            else if ( File.Exists( expectedProgramOutputFilePath ) )
            {
                File.Delete( expectedProgramOutputFilePath );
            }
        }
    }
}