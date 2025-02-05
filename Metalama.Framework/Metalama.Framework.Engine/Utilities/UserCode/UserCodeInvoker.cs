// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Infrastructure;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Utilities.UserCode;

/// <summary>
/// Invokes some user code, handles exceptions, switches the <see cref="UserCodeExecutionContext"/>,
/// and optionally invokes an <see cref="IUserCodeInvokerHook"/> (this hook is used by Try Metalama).
/// </summary>
[PublicAPI]
public sealed class UserCodeInvoker : IProjectService, IGlobalService
{
    private readonly IUserCodeInvokerHook? _hook;
    private readonly ILogger _logger;

    public UserCodeInvoker( GlobalServiceProvider serviceProvider )
    {
        this._hook = serviceProvider.GetService<IUserCodeInvokerHook>();
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( "UserCode" );
    }

    /// <summary>
    /// Handles an exception and returns a value indicating whether the exception can be ignored.
    /// </summary>
    private bool OnException( Exception e, UserCodeExecutionContext context )
    {
        this._logger.Trace?.Log( $"Handling exception from {context.Description}: {e}" );

        var compileTimeProject = context.ServiceProvider.GetService<CompileTimeProject>();

        var userException = e switch
        {
            TargetInvocationException { InnerException: { } innerException } => innerException,
            _ => e
        };

        // We prefer to report the diagnostic on the target declaration of the aspect, and not in source code, because we must report the diagnostic
        // in the current project.
        var diagnosticLocation = context.TargetDeclaration?.GetDiagnosticLocation();

        // If we don't have a target declaration, try to report in source code.
        if ( diagnosticLocation == null && compileTimeProject != null )
        {
            var stackTrace = new StackTrace( userException, true );

            diagnosticLocation = GetSourceCodeLocation( stackTrace, compileTimeProject ) ?? Location.None;
        }

        if ( userException is DiagnosticException { InSourceCode: true } invalidUserCodeException )
        {
            foreach ( var diagnostic in invalidUserCodeException.Diagnostics )
            {
                if ( diagnosticLocation != null )
                {
                    // Report the original diagnostics, but with the fixed location.
                    context.Diagnostics.Report(
                        Diagnostic.Create(
                            diagnostic.Id,
                            diagnostic.Descriptor.Category,
                            new NonLocalizedString( diagnostic.GetLocalizedMessage() ),
                            diagnostic.Severity,
                            diagnostic.DefaultSeverity,
                            true,
                            diagnostic.WarningLevel,
                            diagnostic.Descriptor.Title,
                            diagnostic.Descriptor.Description,
                            diagnostic.Descriptor.HelpLinkUri,
                            diagnosticLocation,
                            diagnostic.AdditionalLocations,
                            properties: diagnostic.Properties ) );
                }
                else
                {
                    // If we don't have a better location, report the original diagnostic.
                    context.Diagnostics.Report( diagnostic );
                }
            }
        }
        else
        {
            var standardDirectories = context.ServiceProvider.Global.GetBackstageService<IStandardDirectories>();
            var applicationInfoProvider = context.ServiceProvider.Global.GetRequiredBackstageService<IApplicationInfoProvider>();
            string reportFile;

            if ( applicationInfoProvider.CurrentApplication.ShouldCreateLocalCrashReports && standardDirectories != null )
            {
                try
                {
                    reportFile = Path.Combine(
                        standardDirectories.CrashReportsDirectory,
                        $"exception-{Guid.NewGuid()}.txt" );

                    File.WriteAllText( reportFile, e.ToString() );
                }
                catch ( Exception reportException )
                {
                    reportFile = $"(cannot report: {reportException.Message})";
                }
            }
            else
            {
                reportFile = "(none)";
            }

            var exceptionMessage = userException.Message.TrimSuffix( "." );
            var exceptionType = userException.GetType().FullName ?? "<unknown>";

            context.Diagnostics.Report(
                GeneralDiagnosticDescriptors.ExceptionInUserCode.CreateRoslynDiagnostic(
                    diagnosticLocation,
                    (context.Description,
                     exceptionType,
                     exceptionMessage,
                     reportFile) ) );
        }

        return true;
    }

    public bool TryInvoke<T>( Func<T> func, UserCodeExecutionContext context, [NotNullWhen( true )] out T? result )
    {
        var adapter = new UserCodeFuncAdapter<T>( func );

        return this.TryInvoke( adapter.UserCodeFunc, ref adapter, context, out result );
    }

    public bool TryInvoke<TResult, TPayload>(
        UserCodeFunc<TResult, TPayload> func,
        ref TPayload payload,
        UserCodeExecutionContext context,
        [NotNullWhen( true )] out TResult? result )
    {
        try
        {
            result = this.Invoke( func, ref payload, context, false );

#pragma warning disable CS8762 // Parameter must have a non-null value when exiting in some condition.
            return true;
#pragma warning restore CS8762 // Parameter must have a non-null value when exiting in some condition.
        }
        catch ( Exception e )
        {
            // We cannot use OnException in a `when` clause because exceptions in the OnException method will be ignored
            // and it will be weird.
            if ( this.OnException( e, context ) )
            {
                result = default;

                return false;
            }
            else
            {
                throw;
            }
        }
    }

    public bool TryInvokeEnumerable<T>( Func<IEnumerable<T>> func, UserCodeExecutionContext context, [NotNullWhen( true )] out List<T>? result )
    {
        try
        {
            result = new List<T>();

            try
            {
                var enumerable = this.Invoke( func, context, false );
                var enumerator = this.Invoke( enumerable.GetEnumerator, context, false );

                while ( this.Invoke( enumerator.MoveNext, context, false ) )
                {
                    result.Add( this.Invoke( () => enumerator.Current, context, false ) );
                }
            }
            catch ( Exception e )
            {
                // We cannot use OnException in a `when` clause because exceptions in the OnException method will be ignored
                // and it will be weird.
                if ( this.OnException( e, context ) )
                {
                    result = default;

                    return false;
                }
                else
                {
                    throw;
                }
            }

            return true;
        }
        catch ( Exception e )
        {
            // We cannot use OnException in a `when` clause because exceptions in the OnException method will be ignored
            // and it will be weird.
            if ( this.OnException( e, context ) )
            {
                result = default;

                return false;
            }
            else
            {
                throw;
            }
        }
    }

    public bool TryInvoke( Action action, UserCodeExecutionContext context )
        => this.TryInvoke(
            () =>
            {
                action();

                return true;
            },
            context,
            out _ );

    public void Invoke( Action action, UserCodeExecutionContext context, bool wrapExceptions = true )
    {
        _ = this.Invoke(
            () =>
            {
                action();

                return true;
            },
            context,
            wrapExceptions );
    }

    public T Invoke<T>( Func<T> func, UserCodeExecutionContext context, bool wrapExceptions = true )
    {
        var adapter = new UserCodeFuncAdapter<T>( func );

        return this.Invoke( adapter.UserCodeFunc, ref adapter, context, wrapExceptions );
    }

    public TResult Invoke<TResult, TPayload>(
        UserCodeFunc<TResult, TPayload> func,
        ref TPayload payload,
        UserCodeExecutionContext context,
        bool wrapException = true )
    {
        using ( UserCodeExecutionContext.WithContext( context ) )
        {
            try
            {
                if ( this._hook != null )
                {
                    return this._hook.Invoke( func, ref payload );
                }
                else
                {
                    return func( ref payload );
                }
            }
            catch ( Exception e ) when ( wrapException )
            {
                throw new UserCodeException( context, e );
            }
        }
    }

    public Task InvokeAsync( Func<Task> func, UserCodeExecutionContext context )
    {
        async Task<bool> Wrapper()
        {
            await func();

            return true;
        }

        return this.InvokeAsync( Wrapper, context );
    }

    public async Task<TResult> InvokeAsync<TResult>( Func<Task<TResult>> func, UserCodeExecutionContext context )
    {
        using ( UserCodeExecutionContext.WithContext( context ) )
        {
            if ( this._hook != null )
            {
                return await this._hook.InvokeAsync( func );
            }
            else
            {
                return await func();
            }
        }
    }

    public async Task<bool> TryInvokeAsync( Func<Task> func, UserCodeExecutionContext context )
    {
        async Task<bool> Wrapper()
        {
            await func();

            return true;
        }

        var result = await this.TryInvokeAsync( Wrapper, context );

        return result.IsSuccessful;
    }

    public async Task<FallibleResult<TResult>> TryInvokeAsync<TResult>( Func<Task<TResult>> func, UserCodeExecutionContext context )
    {
        using ( UserCodeExecutionContext.WithContext( context ) )
        {
            try
            {
                if ( this._hook != null )
                {
                    return await this._hook.InvokeAsync( func );
                }
                else
                {
                    return await func();
                }
            }
            catch ( Exception e )
            {
                // We cannot use OnException in a `when` clause because exceptions in the OnException method will be ignored
                // and it will be weird.
                if ( this.OnException( e, context ) )
                {
                    return default;
                }
                else
                {
                    throw;
                }
            }
        }
    }

    private static Location? GetSourceCodeLocation( StackTrace stackTrace, CompileTimeProject compileTimeProject )
    {
        // TODO: This method needs to be rewritten. Ideally, the PDB would be mapped to the source file, it would not be necessary
        // to perform the mapping here.

        // Get the syntax tree where the exception happened.
        // ReSharper disable once RedundantSuppressNullableWarningExpression
        var stackFrames = stackTrace.GetFrames()!;

        var frame =
            stackFrames
                .Where( f => f.GetFileName() != null )
                .Select( f => (Frame: f, File: compileTimeProject.FindCodeFileFromTransformedPath( f.GetFileName()! )) )
                .FirstOrDefault( i => i.File.File != null );

        if ( frame.File.File == null )
        {
            return null;
        }

        // Check if we have a location map for this file anyway.
        var frameProject = frame.File.Project.AssertNotNull();
        var textMap = frameProject.GetTextMap( frame.Frame.GetFileName()! );

        if ( textMap == null )
        {
            return null;
        }

        var transformedFileFullPath = Path.Combine( frameProject.Directory!, frame.File.File.TransformedPath );

        var transformedText = SourceText.From( File.ReadAllText( transformedFileFullPath ) );

        // Find the node in the syntax tree.
        var textLines = transformedText.Lines;
        var lineNumber = frame.Frame.GetFileLineNumber();

        if ( lineNumber == 0 )
        {
            return null;
        }

        var columnNumber = frame.Frame.GetFileColumnNumber();

        if ( lineNumber > textLines.Count )
        {
            return null;
        }

        var textLine = textLines[lineNumber - 1];

        if ( columnNumber > textLine.End )
        {
            return null;
        }

        var position = textLine.Start + columnNumber - 1;

        var transformedTree = CSharpSyntaxTree.ParseText( transformedText, SupportedCSharpVersions.DefaultParseOptions );

        var node = transformedTree.GetRoot().FindNode( TextSpan.FromBounds( position, position ) );
        node = FindPotentialExceptionSource( node );

        if ( node != null )
        {
            var targetLocation = node.GetLocation();
            var sourceLocation = textMap.GetSourceLocation( targetLocation.SourceSpan );

            return sourceLocation ?? targetLocation;
        }
        else
        {
            // TODO: We could report the whole line here.
            return Location.None;
        }

        // Finds a parent node that is a potential source of exception. We take the farthest ancestor that has the same span start.
        static SyntaxNode? FindPotentialExceptionSource( SyntaxNode? node )
            => node switch
            {
                null => null,
                { Parent: { } parent } when parent.SpanStart == node.SpanStart => FindPotentialExceptionSource( node.Parent ),
                _ => node
            };
    }
}