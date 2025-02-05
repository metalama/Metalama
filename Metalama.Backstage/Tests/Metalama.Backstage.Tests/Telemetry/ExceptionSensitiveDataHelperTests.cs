// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Telemetry;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace Metalama.Backstage.Tests.Telemetry
{
    public class ExceptionSensitiveDataHelperTests
    {
        [Theory]
        [InlineData( "   at MyNamespace.MyType..ctor(MyNamespace.MyType param) in :line 16707565", "   at #user(#user param) in :line 16707565" )]
        [InlineData(
            "   at MyNamespace.MyType.<>c__DisplayClass11_0(MyNamespace.MyType param) in :line 16707565",
            "   at #user(#user param) in :line 16707565" )]
        [InlineData(
            "   at MyNamespace.MyType.<MethodName>d__25.MoveNext(MyNamespace.MyType param) in :line 16707565",
            "   at #user(#user param) in :line 16707565" )]
        [InlineData( "   at MyNamespace.MyType.MyMethod(MyNamespace.MyType param) in :line 16707565", "   at #user(#user param) in :line 16707565" )]
        [InlineData( "   at MyNamespace.MyType.MyMethod(String param) in :line 16707565", "   at #user(String param) in :line 16707565" )]
        [InlineData( "   at MyNamespace.MyType.MyMethod(PostSharp.Type param) in :line 16707565", "   at #user(PostSharp.Type param) in :line 16707565" )]
        [InlineData( "   at MyNamespace.PostSharp.MyMethod(MyNamespace.MyType param) in :line 16707565", "   at #user(#user param) in :line 16707565" )]
        [InlineData(
            "   at PostSharpMyNamespace.MyType.MyMethod(PostSharpMyNamespace.MyType param) in :line 16707565",
            "   at #user(#user param) in :line 16707565" )]
        [InlineData(
            "   at MetalamaMyNamespace.MyType.MyMethod(MetalamaMyNamespace.MyType param) in :line 16707565",
            "   at #user(#user param) in :line 16707565" )]
        [InlineData(
            "   at MicrosoftMyNamespace.MyType.MyMethod(MicrosoftMyNamespace.MyType param) in :line 16707565",
            "   at #user(#user param) in :line 16707565" )]
        [InlineData( "   at MSMyNamespace.MyType.MyMethod(MSMyNamespace.MyType param) in :line 16707565", "   at #user(#user param) in :line 16707565" )]
        [InlineData(
            "   at SystemMyNamespace.MyType.MyMethod(SystemMyNamespace.MyType param) in :line 16707565",
            "   at #user(#user param) in :line 16707565" )]
        public void SensitiveDataIsRemoved( string input, string expectedOutput )
        {
            var actualOutput = ExceptionSensitiveDataHelper.Instance.RemoveSensitiveData( input );

            Assert.Equal( expectedOutput, actualOutput );
        }

        [Theory]
        [InlineData( @"C:\Windows path with spaces\file.extension", "#path" )]
        [InlineData( @"foo C:\Windows path with spaces\file.extension", "foo #path" )]
        [InlineData( @"C:\Windows path with spaces\file.extension bar", "#path" )]
        [InlineData( @"foo C:\Windows path with spaces\file.extension bar", "foo #path" )]
        [InlineData( @"C:\WindowsPathWithoutSpaces\file.extension", "#path" )]
        [InlineData( @"foo C:\WindowsPathWithoutSpaces\file.extension", "foo #path" )]
        [InlineData( @"C:\WindowsPathWithoutSpaces\file.extension bar", "#path" )]
        [InlineData( @"foo C:\WindowsPathWithoutSpaces\file.extension bar", "foo #path" )]
        public void SensitiveDataIsRemovedOnWindows( string input, string expectedOutput )
        {
            var actualOutput = new ExceptionSensitiveDataHelper( isWindows: true ).RemoveSensitiveData( input );

            Assert.Equal( expectedOutput, actualOutput );
        }

        [Theory]
        [InlineData( @"/var/unix\ path\ with\ spaces/file.extension", "#path" )]
        [InlineData( @"foo /var/unix\ path\ with\ spaces/file.extension", "foo #path" )]
        [InlineData( @"/var/unix\ path\ with\ spaces/file.extension bar", "#path bar" )]
        [InlineData( @"foo /var/unix\ path\ with\ spaces/file.extension bar", "foo #path bar" )]
        [InlineData( @"/var/UnixPathWithoutSpaces/file.extension", "#path" )]
        [InlineData( @"foo /var/UnixPathWithoutSpaces/file.extension", "foo #path" )]
        [InlineData( @"/var/UnixPathWithoutSpaces/file.extension bar", "#path bar" )]
        [InlineData( @"foo /var/UnixPathWithoutSpaces/file.extension bar", "foo #path bar" )]
        public void SensitiveDataIsRemovedOnUnix( string input, string expectedOutput )
        {
            var actualOutput = new ExceptionSensitiveDataHelper( isWindows: false ).RemoveSensitiveData( input );

            Assert.Equal( expectedOutput, actualOutput );
        }

        [Theory]
        [InlineData( "   at PostSharp.Type..ctor(String param) in :line 16707565" )]
        [InlineData( "   at PostSharp.Type.<>c__DisplayClass11_0(String param) in :line 16707565" )]
        [InlineData( "   at PostSharp.Type.<MethodName>d__25.MoveNext(String param) in :line 16707565" )]
        [InlineData( "   at PostSharp.Type.Method(String message) in :line 16707565" )]
        [InlineData( "   at Metalama.Type.Method(String message) in :line 16707565" )]
        [InlineData( "   at Microsoft.Type.Method(String message) in :line 16707565" )]
        [InlineData( "   at MS.Type.Method(String message) in :line 16707565" )]
        [InlineData( "   at System.Type.Method(String message) in :line 16707565" )]
        [InlineData( "   at Presentation.Type.Method(String message) in :line 16707565" )]
        [InlineData( "   at PresentationFramework.Type.Method(String message) in :line 16707565" )]
        [InlineData( "   at EnvDTE.Type.Method(String message) in :line 16707565" )]
        [InlineData( "   at EnvDTENamespace.Type.Method(String message) in :line 16707565" )]
        [InlineData( "   at Windows.Type.Method(String message) in :line 16707565" )]
        [InlineData( "   at WindowsNamespace.Type.Method(String message) in :line 16707565" )]
        public void NonSensitiveDataIsNotRemoved( string input )
        {
            var actualOutput = ExceptionSensitiveDataHelper.Instance.RemoveSensitiveData( input );

            Assert.Equal( input, actualOutput );
        }

        // Always solve failures of the other tests before solving failures of this one.
        [Fact]
        public void SensitiveDataIsRemovedFromMultilineInput()
        {
            IEnumerable<string[]> GetInlineData( string theoryMethodName )
            {
                var theoryMethod = this.GetType().GetMethod( theoryMethodName )!;

                return theoryMethod.GetCustomAttributes<InlineDataAttribute>()
                    .Select( a => a.GetData( theoryMethod ).Single().Cast<string>().ToArray() );
            }

            StringBuilder inputBuilder = new();
            StringBuilder expectedResultBuilder = new();

            foreach ( var data in GetInlineData( nameof(this.SensitiveDataIsRemoved) ) )
            {
                inputBuilder.AppendLine( data[0] );
                expectedResultBuilder.AppendLine( data[1] );
            }

            foreach ( var data in GetInlineData( nameof(this.NonSensitiveDataIsNotRemoved) ) )
            {
                inputBuilder.AppendLine( data[0] );
                expectedResultBuilder.AppendLine( data[0] );
            }

            StringBuilder windowsInputBuilder = new( inputBuilder.ToString() );
            StringBuilder windowsExpectedResultBuilder = new( expectedResultBuilder.ToString() );

            foreach ( var data in GetInlineData( nameof(this.SensitiveDataIsRemovedOnWindows) ) )
            {
                windowsInputBuilder.AppendLine( data[0] );
                windowsExpectedResultBuilder.AppendLine( data[1] );
            }

            StringBuilder unixInputBuilder = new( inputBuilder.ToString() );
            StringBuilder unixExpectedResultBuilder = new( expectedResultBuilder.ToString() );

            foreach ( var data in GetInlineData( nameof(this.SensitiveDataIsRemovedOnUnix) ) )
            {
                unixInputBuilder.AppendLine( data[0] );
                unixExpectedResultBuilder.AppendLine( data[1] );
            }

            var actualWindowsOutput = new ExceptionSensitiveDataHelper( isWindows: true ).RemoveSensitiveData( windowsInputBuilder.ToString() );
            Assert.Equal( actualWindowsOutput, windowsExpectedResultBuilder.ToString() );

            var actualUnixOutput = new ExceptionSensitiveDataHelper( isWindows: false ).RemoveSensitiveData( unixExpectedResultBuilder.ToString() );
            Assert.Equal( actualUnixOutput, unixExpectedResultBuilder.ToString() );
        }
    }
}