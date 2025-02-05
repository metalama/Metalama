// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.Formatting;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Metalama.Testing.AspectTesting;

/// <summary>
/// A set of test options, which can be included in the source text of tests using special comments like <c>// @ReportOutputWarnings</c>.
/// This class is JSON-serializable. Another way to define options is to add a file named <c>metalamaTests.json</c> into the test directory or
/// any parent directory.
/// </summary>
[PublicAPI]
[JsonObject]
public class TestOptions
{
    private static readonly Regex _optionRegex = new( @"^\s*//\s*@(?<name>\w+)\s*(\((?<arg>[^\)]*)\))?", RegexOptions.Multiline );
    private readonly List<string> _invalidSourceOptions = new();
    private bool? _writeOutputHtml;

    /// <summary>
    /// Gets or sets the reason for which the test must be skipped, or <c>null</c> if the test must not be skipped.
    /// To skip a test, add this comment to your test file: <c>// @Skipped(reason)</c>. 
    /// </summary>
    public string? SkipReason { get; set; }

    /// <summary>
    /// Gets a value indicating whether the current test must be skipped.
    /// </summary>
    public bool IsSkipped => this.SkipReason != null;

    /// <summary>
    /// Gets or sets a value indicating whether the diagnostics of the compilation of the transformed target code should be included in the test result.
    /// This is useful when diagnostic suppression is being tested.
    /// To enable this option in a test, add this comment to your test file: <c>// @ReportOutputWarnings</c>. 
    /// </summary>
    public bool? ReportOutputWarnings { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the output file must be compiled into a binary (e.g. emitted).
    /// To enable this option in a test, add this comment to your test file: <c>// @OutputCompilationDisabled</c>.
    /// </summary>
    public bool? OutputCompilationDisabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether diagnostics of all severities should be included in the rest result. By default, only
    /// warnings and errors are included.
    /// To enable this option in a test, add this comment to your test file: <c>// @IncludeAllSeverities</c>.  
    /// </summary>
    public bool? IncludeAllSeverities { get; set; }

    /// <summary>
    /// Gets or sets the name of the test runner factory type (implementing <see cref="ITestRunnerFactory"/>).
    /// You can only define this option in the <c>metalamaTests.json</c> file of a directory. This setting is for Metalama internal use only.
    /// </summary>
    public string? TestRunnerFactoryType { get; set; }

    /// <summary>
    /// Gets the list of assembly names that should be included in the compilation.
    /// To add a named assembly reference, add this comment to your test file: <c>// @AssemblyReference(assemblyName)</c>.
    /// </summary>
    public List<TestAssemblyReference> References { get; } = new();

    /// <summary>
    /// Gets the list of source code files that should be included in the compilation.
    /// To enable this option in a test, add this comment to your test file: <c>// @Include(relativePath)</c>. 
    /// </summary>
    public List<string> IncludedFiles { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether adding system files to the test compilation should be skipped.
    /// Namely, there is one file that adds the <c>System.Runtime.CompilerServices.IsExternalInit</c> type on .Net Framework.
    /// To enable this option in a test, add this comment to your test file: <c>// @SkipAddingSystemFiles</c>. 
    /// </summary>
    public bool? SkipAddingSystemFiles { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether HTML of syntax-highlighted files should be produced for input files. If <c>true</c>, these files
    /// are created to the <c>obj/html</c> directory.
    /// To enable this option in a test, add this comment to your test file: <c>// @WriteInputHtml</c>. 
    /// </summary>
    public bool? WriteInputHtml { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether HTML of syntax-highlighted files should be produced for the consolidated output file. If <c>true</c>, this file
    /// is created to the <c>obj/html</c> directory. Setting this property to <c>true</c> automatically sets the <see cref="FormatOutput"/> property to <c>true</c>. 
    /// To enable this option in a test, add this comment to your test file: <c>// @WriteOutputHtml</c>. 
    /// </summary>
    public bool? WriteOutputHtml
    {
        get => this._writeOutputHtml;

        set
        {
            this._writeOutputHtml = value;

            if ( value.GetValueOrDefault() )
            {
                this.FormatOutput = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether titles (tooltips) should be added to HTML files.
    /// To enable this option in a test, add this comment to your test file: <c>// @AddHtmlTitles</c>. 
    /// </summary>
    public bool? AddHtmlTitles { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the <see cref="TestResult.ErrorMessage"/> should be added to
    /// the test output.
    /// You can only define this option in the <c>metalamaTests.json</c> file of a directory. 
    /// </summary>
    public bool? ReportErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the output <c>t.cs</c> file should be formatted, which includes simplifying the code and adding <c>using</c> directives. The default behavior is <c>true</c>.
    /// To enable this option in a test, add this comment to your test file: <c>// @FormatOutput</c>.
    /// </summary>
    public bool? FormatOutput { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether whitespace are taken into account while comparing the the expected output <c>t.cs</c> file with the actual output. The default behavior is <c>false</c>.
    /// To enable this option in a test, add this comment to your test file: <c>// @PreserveWhitespace</c>.
    /// </summary>
    public bool? CompareWhitespace { get; set; }

    [Obsolete( "Use CompareWhitespace" )]
    public bool? PreserveWhitespace
    {
        get => this.CompareWhitespace;
        set => this.CompareWhitespace = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether C# nullability is disabled for the compilation.
    /// To enable this option in a test, add this comment to your test file: <c>// @NullabilityDisabled</c>.
    /// </summary>
    public bool? NullabilityDisabled { get; set; }

    /// <summary>
    /// Gets a list of warnings that are not reported even if <see cref="ReportOutputWarnings"/> is set to <c>true</c>.
    /// To add an item into this collection from a test, add this comment to your test file: <c>// @IgnoredDiagnostic(id)</c>.
    /// </summary>
    public List<string> IgnoredDiagnostics { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the list of <see cref="IgnoredDiagnostics"/> inherited from the parent level (directory or base directory)
    /// must be cleared before new diagnostics are added to this list. This option is not inherited from the base level.
    /// To enable this option in a test, add this comment to your test file: <c>// @ClearIgnoredDiagnostics</c>.
    /// </summary>
    public bool? ClearIgnoredDiagnostics { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the test is allowed to have compile-time code that has dynamic calls.
    /// To enable this option in a test, add this comment to your test file: <c>// @AllowCompileTimeDynamicCode</c>.
    /// </summary>
    public bool? AllowCompileTimeDynamicCode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the `Program.Main` method should be executed if it exists. The default value is <c>true</c>.
    /// To disable this option in a test, add this comment to your test file: <c>// @DisableExecuteProgram</c>.
    /// </summary>
    public bool? ExecuteProgram { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the program output should be compared to its expected value. The default value is <c>true</c>.
    /// To disable this option in a test, add this comment to your test file: <c>// @DisableCompareProgramOutput</c>. 
    /// </summary>
    public bool? CompareProgramOutput { get; set; }

    /// <summary>
    /// Gets or sets a value indicating which type of the output assembly should be used for the test. Currently valid values are <c>Dll</c> <c>Exe</c> (default).
    /// </summary>
    public string? OutputAssemblyType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the test should be executed even if the input compilation has errors.
    /// To enable this option in a test, add this comment to your test file: <c>// @AcceptInvalidInput</c>.
    /// </summary>
    public bool? AcceptInvalidInput { get; set; }

    /// <summary>
    /// Gets the set of preprocessor symbols that are required for this test, otherwise the test would be skipped.
    /// To add an item into this collection from a test, add this comment to your test file: <c>// @RequiredConstant(constant)</c>.
    /// </summary>
    public List<string> RequiredConstants { get; } = new();

    /// <summary>
    /// Gets the set of preprocessor symbols that are forbidden for this test, test would be skipped if any is present.
    /// To add an item into this collection from a test, add this comment to your test file: <c>// @ForbiddenConstant(constant)</c>.
    /// </summary>
    public List<string> ForbiddenConstants { get; } = new();

    /// <summary>
    /// Gets the set of preprocessor symbols that are defined for this test.
    /// To add an item into this collection from a test, add this comment to your test file: <c>// @DefinedConstant(constant)</c>.
    /// All constants of the test project and TESTRUNNER and METALAMA are defined by default.
    /// Constants added via <see cref="DependencyDefinedConstants"/> option are not added.
    /// </summary>
    public List<string> DefinedConstants { get; } = new();

    /// <summary>
    /// Gets the set of preprocessor symbols that are defined for this test dependency.
    /// To add an item into this collection from a test, add this comment to your test file: <c>// @DependencyDefinedConstant(constant)</c>.
    /// All constants of the test project and TESTRUNNER and METALAMA are defined by default.
    /// Constants added via <see cref="DefinedConstants"/> option are not added.
    /// </summary>
    public List<string> DependencyDefinedConstants { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating the test scenario.
    /// See <see cref="TestScenario"/> enum values for details.
    /// </summary>
    public TestScenario? TestScenario { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the target syntax tree suffix. This allows the selection of a different syntax tree for the test scenario. This is currently valid only for <see cref="TestScenario.Preview"/> scenario.
    /// </summary>
    public string? TargetSyntaxTreeSuffix { get; set; }

    /// <summary>
    /// Gets or sets the zero-based index of the code fix to be applied
    /// when <see cref="TestScenario"/> is set to <see cref="AspectTesting.TestScenario.CodeFix"/>.
    /// To set this option in a test, add this comment to your test file: <c>// @AppliedCodeFixIndex(id)</c>.
    /// </summary>
    public int? AppliedCodeFixIndex { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether disabled code should be kept as trivia.
    /// To set this option in a test, add this comment to your test file: <c>// @KeepDisabledCode</c>.
    /// </summary>
    public bool? KeepDisabledCode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating which end-of-line sequence is expected.
    /// To set this option in a test, add this comment to your test file: <c>// @ExpectedEndOfLine(eol)</c> where EOL is <c>CR</c>, <c>LF</c> or <c>CRLF</c>.
    /// </summary>
    public string? ExpectedEndOfLine { get; set; }

    /// <summary>
    /// Gets or sets the version of the C# language that the test should be compiled with.
    /// To set this option in a test, add this comment to your test file: <c>// @LanguageVersion(version)</c>.
    /// </summary>
    public LanguageVersion? LanguageVersion { get; set; }

    /// <summary>
    /// Gets or sets the version of the C# language that the dependencies of the test should be compiled with.
    /// To set this option in a test, add this comment to your test file: <c>// @DependencyLanguageVersion(version)</c>.
    /// </summary>
    public LanguageVersion? DependencyLanguageVersion { get; set; }

    /// <summary>
    /// Gets or sets the list of C# language features that the test should be compiled with.
    /// To set this option in a test, add this comment to your test file: <c>// @LanguageFeature(feature)</c> or <c>// @LanguageFeature(feature=value)</c>.
    /// </summary>
    public ImmutableDictionary<string, string> LanguageFeatures { get; set; } = ImmutableDictionary<string, string>.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether an error should be reported if the compilation uses aspects that
    /// are not explicitly ordered.
    /// To enable this option in a test, add this comment to your test file: <c>// @RequireOrderedAspects</c>. 
    /// </summary>
    public bool? RequireOrderedAspects { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Metalama should produce logs and output them to the Xunit test log.
    /// To enable this option in a test, add this comment to your test file: <c>// @EnableLogging</c>. 
    /// </summary>
    public bool? EnableLogging { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified name of expected exception type to be thrown.
    /// To set this option in a test, add this comment to your test file: <c>// @ExpectedException(fully qualified exception type name)</c>.
    /// </summary>
    public string? ExpectedException { get; set; }

    /// <summary>
    /// Gets or sets the name of the <c>Main</c> method. The default value of this property is <c>Main</c>. This option
    /// is useful to work around the <c>CS0017</c> error.
    /// To set this option in a test, add this comment to your test file: <c>// @MainMethod(name)</c>. 
    /// </summary>
    public string? MainMethod { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether memory leaks should be detected. This features is supported from .NET 6. Leaks are detected
    /// by trying to unload the <c>AssemblyLoadContext</c>. If it fails to unload in due time, it means that Metalama or the user code has
    /// a static reference to compile-time assemblies. To enable this option in a test, add this comment to your test file: <c>// @CheckMemoryLeaks</c>.
    /// </summary>
    public bool? CheckMemoryLeaks { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the line number should be included in reports of warnings or errors in the consolidated test output.
    /// The default value is <c>false</c>. To enable this option in a test, add this comment to your test file: <c>// @IncludeLineNumberInDiagnosticReport</c>.
    /// </summary>
    public bool? IncludeLineNumberInDiagnosticReport { get; set; }
    
    public bool? IncludeDeclarationInDiagnosticReport { get; set; }

    /// <summary>
    /// Gets or sets a value indicating that the test output should not include the transformed code, but only the diagnostics.
    /// The default value is <c>false</c>. To enable this option in a test, add this comment to your test file: <c>// @RemoveOutputCode</c>.
    /// </summary>
    public bool? RemoveOutputCode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating that the test output should not include the diagnostic message, but only the diagnostic ID.
    /// The default value is <c>false</c>. To enable this option in a test, add this comment to your test file: <c>// @RemoveDiagnosticMessage</c>. 
    /// </summary>
    public bool? RemoveDiagnosticMessage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating that assembly-wide attributes should not be added to the test output.
    /// The default value is <c>false</c>. To enable this option in a test, add this comment to your test file: <c>// @ExcludeAssemblyAttributes</c>.
    /// </summary>
    public bool? ExcludeAssemblyAttributes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the JIT debugger should be launched before executing the test.
    /// The default value is <c>false</c>. To enable this option in a test, add this comment to your test file: <c>// @LaunchDebugger</c>.
    /// </summary>
    public bool? LaunchDebugger { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether code of the compile-time project should be formatted.
    /// The default value is <c>false</c> in the default test runner, but <c>true</c> in Aspect Workbench.
    /// To set this option in a test, add this comment to your test file: <c>// @FormatCompileTimeCode(value)</c> where <c>value</c>
    /// is <c>true</c> or <c>false</c>.
    /// </summary>
    public bool? FormatCompileTimeCode { get; set; }

    /// <summary>
    /// Gets or sets the project of the test. By default, the test file name without extension is used.
    /// </summary>
    public string? ProjectName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the licenses registered in the user profile should be ignored.
    /// The default value is <c>false</c>. When this property is set to <c>true</c>, user-profile licenses are not loaded for this test.
    /// </summary>
    public bool? IgnoreUserProfileLicenses { get; set; }

    public bool? TestUnformattedOutput { get; set; }

    /// <summary>
    /// Gets or sets the number of times the test must be repeated. This can be used to reproduce random issues.
    /// The default value is <c>1</c>. To change this option in a test, add this comment to your test file: <c>// @Repeat(int)</c>. 
    /// </summary>
    public int? Repeat { get; set; }

    /// <summary>
    /// Gets or sets the seed for the random number generator. The default value is random when <see cref="Repeat"/> is <c>1</c>, or <c>0</c>
    /// otherwise. To change this option in a test, add this comment to your test file: <c>// @RandomSeed(int)</c>. 
    /// </summary>
    public int? RandomSeed { get; set; }

    internal void SetFullPaths( string directory )
    {
        for ( var i = 0; i < this.IncludedFiles.Count; i++ )
        {
            this.IncludedFiles[i] = Path.GetFullPath( Path.Combine( directory, this.IncludedFiles[i] ) );
        }
    }

    /// <summary>
    /// Applies <see cref="TestDirectoryOptions"/> to the current object by overriding any property
    /// that is not defined in the current object but defined in the argument.
    /// </summary>
    internal virtual void ApplyBaseOptions( TestDirectoryOptions baseOptions )
    {
        this.SkipReason ??= baseOptions.SkipReason;

        this.ReportOutputWarnings ??= baseOptions.ReportOutputWarnings;

        this.OutputCompilationDisabled ??= baseOptions.OutputCompilationDisabled;

        this.IncludeAllSeverities ??= baseOptions.IncludeAllSeverities;

        this.TestRunnerFactoryType ??= baseOptions.TestRunnerFactoryType;

        this.WriteInputHtml ??= baseOptions.WriteInputHtml;

        this.WriteOutputHtml ??= baseOptions.WriteOutputHtml;

        this.AddHtmlTitles ??= baseOptions.AddHtmlTitles;

        this.ReportErrorMessage ??= baseOptions.ReportErrorMessage;

        this.FormatOutput ??= baseOptions.FormatOutput;

        this.CompareWhitespace ??= baseOptions.CompareWhitespace;

        this.IncludedFiles.AddRange( baseOptions.IncludedFiles );

        this.References.AddRange( baseOptions.References );

        this.AllowCompileTimeDynamicCode ??= baseOptions.AllowCompileTimeDynamicCode;

        this.ExecuteProgram ??= baseOptions.ExecuteProgram;

        this.OutputAssemblyType ??= baseOptions.OutputAssemblyType;

        this.AcceptInvalidInput ??= baseOptions.AcceptInvalidInput;

        this.TestScenario ??= baseOptions.TestScenario;

        this.TargetSyntaxTreeSuffix ??= baseOptions.TargetSyntaxTreeSuffix;

        this.KeepDisabledCode ??= baseOptions.KeepDisabledCode;

        this.AppliedCodeFixIndex ??= baseOptions.AppliedCodeFixIndex;

        if ( !this.ClearIgnoredDiagnostics.GetValueOrDefault() )
        {
            this.IgnoredDiagnostics.AddRange( baseOptions.IgnoredDiagnostics );
        }

        this.RequiredConstants.AddRange( baseOptions.RequiredConstants );

        this.ForbiddenConstants.AddRange( baseOptions.ForbiddenConstants );

        this.DefinedConstants.AddRange( baseOptions.DefinedConstants );

        this.DependencyDefinedConstants.AddRange( baseOptions.DependencyDefinedConstants );

        this.RequireOrderedAspects ??= baseOptions.RequireOrderedAspects;

        this.EnableLogging ??= baseOptions.EnableLogging;

        this.ExpectedException ??= baseOptions.ExpectedException;

        this.MainMethod ??= baseOptions.MainMethod;

        this.CheckMemoryLeaks ??= baseOptions.CheckMemoryLeaks;

        this.IncludeLineNumberInDiagnosticReport ??= baseOptions.IncludeLineNumberInDiagnosticReport;

        this.IncludeDeclarationInDiagnosticReport ??= baseOptions.IncludeDeclarationInDiagnosticReport;

        this.RemoveOutputCode ??= baseOptions.RemoveOutputCode;

        this.RemoveDiagnosticMessage ??= baseOptions.RemoveDiagnosticMessage;

        this.ExcludeAssemblyAttributes ??= baseOptions.ExcludeAssemblyAttributes;

        this.LaunchDebugger ??= baseOptions.LaunchDebugger;

        this.FormatCompileTimeCode ??= baseOptions.FormatCompileTimeCode;

        this.CompareProgramOutput ??= baseOptions.CompareProgramOutput;

        this.IgnoreUserProfileLicenses ??= baseOptions.IgnoreUserProfileLicenses;

        this.TestUnformattedOutput ??= baseOptions.TestUnformattedOutput;

        this.Repeat ??= baseOptions.Repeat;

        this.RandomSeed ??= baseOptions.RandomSeed;
    }

    public IReadOnlyList<string> InvalidSourceOptions => this._invalidSourceOptions;

    /// <summary>
    /// Parses <c>// @</c> directives from source code and apply them to the current object. 
    /// </summary>
    internal void ApplySourceDirectives( string sourceCode, string? path )
    {
        var options = _optionRegex.Matches( sourceCode );
        var ifDirectiveIndex = sourceCode.IndexOf( "#if", StringComparison.InvariantCulture );

        foreach ( Match? option in options )
        {
            if ( option == null )
            {
                continue;
            }

            var optionName = option.Groups["name"].Value;
            var optionArg = option.Groups["arg"].Value;

            if ( ifDirectiveIndex < 0 || option.Index < ifDirectiveIndex )
            {
                throw new InvalidTestOptionException( $"The '@{optionName}' option must be in an #if block in '{path}'." );
            }

            switch ( optionName )
            {
                case "ReportOutputWarnings":
                    this.ReportOutputWarnings = true;

                    break;

                case "OutputCompilationDisabled":
                    this.OutputCompilationDisabled = true;

                    break;

                case "IncludeAllSeverities":
                    this.IncludeAllSeverities = true;

                    break;

                case "Skipped":
                    this.SkipReason = string.IsNullOrEmpty( optionArg ) ? "Skipped by directive in source code." : optionArg;

                    break;

                case "Include":
                    this.IncludedFiles.Add( optionArg );

                    break;

                case "SkipAddingSystemFiles":
                    this.SkipAddingSystemFiles = true;

                    break;

                case "TestScenario":
                    if ( Enum.TryParse<TestScenario>( optionArg, out var testScenario ) )
                    {
                        this.TestScenario = testScenario;
                    }
                    else
                    {
                        throw new InvalidTestOptionException(
                            $"'{optionArg} is not a TestScenario value in '{path}'. Use one of following: {Enum.GetValues( typeof(TestScenario) )}." );
                    }

                    break;

                case "TargetSyntaxTreeSuffix":
                    this.TargetSyntaxTreeSuffix = optionArg;

                    break;

                case "WriteInputHtml":
                    this.WriteInputHtml = true;

                    break;

                case "WriteOutputHtml":
                    this.WriteOutputHtml = true;

                    break;

                case "AddHtmlTitles":
                    this.AddHtmlTitles = true;

                    break;

                case "FormatOutput":
                    this.FormatOutput = true;

                    break;

                case "PreserveWhitespace":
                case "CompareWhitespace":
                    this.CompareWhitespace = true;

                    break;

                case "NullabilityDisabled":
                    this.NullabilityDisabled = true;

                    break;

                case "IgnoredDiagnostic":
                    this.IgnoredDiagnostics.Add( optionArg );

                    break;

                case "RequiredConstant":
                    this.RequiredConstants.Add( optionArg );

                    break;

                case "ForbiddenConstant":
                    this.ForbiddenConstants.Add( optionArg );

                    break;

                case "DefinedConstant":
                    this.DefinedConstants.Add( optionArg );

                    break;

                case "DependencyDefinedConstant":
                    this.DependencyDefinedConstants.Add( optionArg );

                    break;

                case "ClearIgnoredDiagnostics":
                    this.ClearIgnoredDiagnostics = true;

                    break;

                case "AllowCompileTimeDynamicCode":
                    this.AllowCompileTimeDynamicCode = true;

                    break;

                case "AcceptInvalidInput":
                    this.AcceptInvalidInput = true;

                    break;

                case "AppliedCodeFixIndex":
                    if ( int.TryParse( optionArg, out var index ) )
                    {
                        this.AppliedCodeFixIndex = index;
                    }
                    else
                    {
                        throw new InvalidTestOptionException( $"'{optionArg} is not a valid code fix index number in '{path}'." );
                    }

                    break;

                case "KeepDisabledCode":
                    this.KeepDisabledCode = true;

                    break;

                case "DisableExecuteProgram":
                    this.ExecuteProgram = false;

                    break;

                case "DisableCompareProgramOutput":
                    this.CompareProgramOutput = false;

                    break;

                case "OutputAssemblyType":
                    this.OutputAssemblyType = optionArg;

                    break;

                case "ExpectedEndOfLine":
                    this.ExpectedEndOfLine = optionArg;

                    break;

                case "AssemblyReference":
                    this.References.Add( new TestAssemblyReference { Name = optionArg } );

                    break;

                case "LanguageVersion":
                    if ( LanguageVersionFacts.TryParse( optionArg, out var languageVersion ) )
                    {
                        this.LanguageVersion = languageVersion;
                    }
                    else
                    {
                        // The version may be a valid number but still not recognized by the current version of Roslyn.
                        if ( double.TryParse( optionArg, out var n ) && n >= 10 && Math.Abs( n - Math.Floor( n ) ) <= double.Epsilon )
                        {
                            this.SkipReason = $"@LanguageVersion '{optionArg}' is not recognized by the current version of Roslyn.";
                        }
                        else
                        {
                            // Throwing here may kill test discovery. 
                            throw new InvalidTestOptionException( $"@LanguageVersion '{optionArg}' is not a valid language version in '{path}'." );
                        }
                    }

                    break;

                case "DependencyLanguageVersion":
                    if ( LanguageVersionFacts.TryParse( optionArg, out var dependencyLanguageVersion ) )
                    {
                        this.DependencyLanguageVersion = dependencyLanguageVersion;
                    }
                    else
                    {
                        // The version may be a valid number but still not recognized by the current version of Roslyn.
                        if ( double.TryParse( optionArg, out var n ) && n >= 10 && Math.Abs( n - Math.Floor( n ) ) <= double.Epsilon )
                        {
                            this.SkipReason = $"@DependencyLanguageVersion '{optionArg}' is not recognized by the current version of Roslyn.";
                        }
                        else
                        {
                            // Throwing here may kill test discovery. 
                            throw new InvalidTestOptionException( $"@DependencyLanguageVersion '{optionArg}' is not a valid language version in '{path}'." );
                        }
                    }

                    break;

                case "LanguageFeature":
                    {
                        var parts = optionArg.Split( '=' );

                        if ( parts.Length == 1 )
                        {
                            this.LanguageFeatures = this.LanguageFeatures.SetItem( parts[0], "" );
                        }
                        else
                        {
                            this.LanguageFeatures = this.LanguageFeatures.SetItem( parts[0], parts[1] );
                        }
                    }

                    break;

                case "ExpectedException":

                    this.ExpectedException = optionArg;

                    break;

                case "RequireOrderedAspects":
                    this.RequireOrderedAspects = true;

                    break;

                case "EnableLogging":
                    this.EnableLogging = true;

                    break;

                case "MainMethod":
                    this.MainMethod = optionArg;

                    break;

                case "CheckMemoryLeaks":
                    this.CheckMemoryLeaks = true;

                    break;

                case "IncludeLineNumberInDiagnosticReport":
                    this.IncludeLineNumberInDiagnosticReport = true;

                    break;
                
                case "IncludeDeclarationInDiagnosticReport":
                    this.IncludeDeclarationInDiagnosticReport = true;

                    break;

                case "RemoveOutputCode":
                    this.RemoveOutputCode = true;

                    break;

                case "RemoveDiagnosticMessage":
                    this.RemoveDiagnosticMessage = true;

                    break;

                case "ExcludeAssemblyAttributes":
                    this.ExcludeAssemblyAttributes = string.IsNullOrEmpty( optionArg ) || bool.Parse( optionArg );

                    break;

                case "LaunchDebugger":
                    this.LaunchDebugger = true;

                    break;

                case "FormatCompileTimeCode":
                    this.FormatCompileTimeCode = bool.Parse( optionArg );

                    break;

                case "ProjectName":
                    this.ProjectName = optionArg;

                    break;

                case "TestUnformattedOutput":
                    this.TestUnformattedOutput = true;

                    break;

                case "Repeat":
                    this.Repeat = int.Parse( optionArg, CultureInfo.InvariantCulture );

                    break;

                case "RandomSeed":
                    this.RandomSeed = int.Parse( optionArg, CultureInfo.InvariantCulture );

                    break;

                default:
                    this._invalidSourceOptions.Add( "@" + optionName );

                    break;
            }
        }
    }

    /// <summary>
    /// Apply all relevant options for a test, both from the source code and from the <c>metalamaTests.config</c> file. 
    /// </summary>
    internal void ApplyOptions( string sourceCode, string path, TestDirectoryOptionsReader optionsReader )
    {
        this.ApplySourceDirectives( sourceCode, path );
        this.ApplyBaseOptions( optionsReader.GetDirectoryOptions( Path.GetDirectoryName( path )! ) );
    }

    internal TestContextOptions ApplyToTestContextOptions( TestContextOptions testContextOptions )
        => testContextOptions with
        {
            RequireOrderedAspects = this.RequireOrderedAspects ?? testContextOptions.RequireOrderedAspects,
            FormatCompileTimeCode = this.FormatCompileTimeCode ?? testContextOptions.FormatCompileTimeCode,
            IgnoreUserProfileLicenses = this.IgnoreUserProfileLicenses ?? testContextOptions.IgnoreUserProfileLicenses,
            CodeFormattingOptions =
            this.FormatOutput switch
            {
                true => CodeFormattingOptions.Formatted,
                false => CodeFormattingOptions.Default,
                null => (CodeFormattingOptions?) null
            } ?? testContextOptions.CodeFormattingOptions
        };
}