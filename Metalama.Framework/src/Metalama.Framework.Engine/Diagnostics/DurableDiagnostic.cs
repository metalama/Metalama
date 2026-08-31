// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Utilities.ObjectGraph;
using Metalama.Framework.Utilities;
using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics;
using System.Globalization;

namespace Metalama.Framework.Engine.Diagnostics;

/// <summary>
/// A <see cref="Diagnostic"/> whose location is not bound to a syntax tree, so that it can be held across
/// compilations. Call <see cref="ToDiagnostic"/> to obtain a diagnostic bound to a tree again.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="Location"/> of a diagnostic holds the syntax tree it points into. The design-time pipeline keeps the
/// diagnostics of a file in its <c>SyntaxTreePipelineResult</c> and carries that result forward to every later
/// version of the project in which the file is not analysed again, so the tree of the run that reported the
/// diagnostic stayed alive for the whole editing session.
/// </para>
/// <para>
/// The location is therefore replaced by an external one, which records a file path, a text span and a line and
/// column span, and holds no tree. Reporting the diagnostic requires the tree back, and the caller has it: the
/// diagnostics of a result all belong to the document named by its <c>SyntaxTreePath</c>.
/// </para>
/// <para>
/// The message is self-contained after the conversion. A diagnostic of Metalama already holds its arguments in the
/// <see cref="NonLocalizedString"/> of its descriptor, and those arguments are materialized where they were bound to
/// a compilation. A diagnostic from another source holds them where they cannot be read, so its message is formatted
/// during the conversion.
/// </para>
/// </remarks>
// Public because the design-time pipeline, which stores these, is in another assembly.
[Durable]
public readonly struct DurableDiagnostic
{
    /// <remarks>
    /// The analyzer cannot verify this member, because whether a <see cref="Diagnostic"/> reaches a compilation
    /// depends on the location and the arguments it was given rather than on its type. The invariant is established
    /// by <see cref="Create"/> instead, which is the only way to obtain an instance, and checked by
    /// <see cref="AssertArgumentsAreDurable"/> in a debug build.
    /// </remarks>
#pragma warning disable LAMA0870
    private readonly Diagnostic _diagnostic;
#pragma warning restore LAMA0870

    private DurableDiagnostic( Diagnostic diagnostic )
    {
        this._diagnostic = diagnostic;
    }

    /// <summary>
    /// Converts a diagnostic into a form that holds no syntax tree.
    /// </summary>
    public static DurableDiagnostic Create( Diagnostic diagnostic )
    {
        var detached = Detach( diagnostic );

        AssertArgumentsAreDurable( detached );

        return new DurableDiagnostic( detached );
    }

    /// <summary>
    /// Gets the identifier of the diagnostic.
    /// </summary>
    public string Id => this._diagnostic.Id;

    /// <summary>
    /// Gets the location of the diagnostic, which names a file but no syntax tree.
    /// </summary>
    public Location Location => this._diagnostic.Location;

    public string GetMessage( IFormatProvider? formatProvider = null ) => this._diagnostic.GetMessage( formatProvider );

    /// <summary>
    /// Returns the diagnostic, with its location bound to a syntax tree again.
    /// </summary>
    /// <param name="syntaxTree">
    /// The tree of the document the diagnostic belongs to, or <c>null</c> to leave the location as it is.
    /// </param>
    /// <remarks>
    /// The span is checked against the length of the tree, because a caller may pass a tree of a later version of the
    /// file. The location is then left as it is rather than pointing into a text that has changed under it.
    /// </remarks>
    public Diagnostic ToDiagnostic( SyntaxTree? syntaxTree )
    {
        if ( syntaxTree == null || this._diagnostic.Location.Kind != LocationKind.ExternalFile )
        {
            return this._diagnostic;
        }

        var span = this._diagnostic.Location.SourceSpan;

        if ( span.End > syntaxTree.Length )
        {
            return this._diagnostic;
        }

        return Diagnostic.Create(
            this._diagnostic.Descriptor,
            Location.Create( syntaxTree, span ),
            this._diagnostic.Severity,
            this._diagnostic.AdditionalLocations,
            this._diagnostic.Properties );
    }

    public override string ToString() => this._diagnostic.ToString();

    private static Diagnostic Detach( Diagnostic diagnostic )
    {
        if ( diagnostic.Location.SourceTree == null )
        {
            // The location is already free of a tree, which is the case of a diagnostic that has no location and of
            // one reported on an external file.
            return diagnostic;
        }

        var lineSpan = diagnostic.Location.GetLineSpan();
        var detachedLocation = Location.Create( lineSpan.Path, diagnostic.Location.SourceSpan, lineSpan.Span );

        if ( diagnostic.Descriptor.MessageFormat is NonLocalizedString )
        {
            // A diagnostic of Metalama. Its arguments are held by the descriptor, and those that were bound to a
            // compilation have been replaced by the string they format to, so the descriptor is reused as it is.
            return Diagnostic.Create(
                diagnostic.Descriptor,
                detachedLocation,
                diagnostic.Severity,
                diagnostic.AdditionalLocations,
                diagnostic.Properties );
        }

        // A diagnostic of the C# compiler or of another analyzer. Its arguments are held by the diagnostic itself,
        // where no public member exposes them, so the message is formatted now and the arguments are dropped with the
        // original.
        var descriptor = new DiagnosticDescriptor(
            diagnostic.Id,
            new NonLocalizedString( diagnostic.Descriptor.Title.ToString( CultureInfo.InvariantCulture ) ),
            new NonLocalizedString( diagnostic.GetMessage( CultureInfo.InvariantCulture ) ),
            diagnostic.Descriptor.Category,
            diagnostic.Descriptor.DefaultSeverity,
            diagnostic.Descriptor.IsEnabledByDefault,
            diagnostic.Descriptor.Description.ToString( CultureInfo.InvariantCulture ),
            diagnostic.Descriptor.HelpLinkUri,
            [..diagnostic.Descriptor.CustomTags] );

        return Diagnostic.Create(
            descriptor,
            detachedLocation,
            diagnostic.Severity,
            diagnostic.AdditionalLocations,
            diagnostic.Properties );
    }

    /// <summary>
    /// Verifies, in a debug build, that no argument of the message reaches a compilation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The arguments are the part of a diagnostic that no rule can check. <c>DiagnosticDescriptorExtensions</c>
    /// replaces every argument it recognizes as bound to a compilation with the string it formats to, and that
    /// recognition is a list of types. A type nobody added to the list is materialized by its default, but a type
    /// wrongly added to the safe side of it is not, and the result is a retained compilation that appears only as a
    /// memory measurement much later.
    /// </para>
    /// <para>
    /// The walk is therefore a check on that list rather than on this class. It runs in a debug build only, since it
    /// visits an object graph on a path that runs for every diagnostic of every design-time run.
    /// </para>
    /// </remarks>
    [Conditional( "DEBUG" )]
    private static void AssertArgumentsAreDurable( Diagnostic diagnostic )
    {
        if ( diagnostic.Descriptor.MessageFormat is not NonLocalizedString { Arguments: { Length: > 0 } arguments } )
        {
            return;
        }

        var walker = new ObjectGraphWalker(
            new ObjectGraphWalkerOptions { MaxObjects = 10_000, Timeout = TimeSpan.FromSeconds( 5 ) } );

        string? retentionPath = null;

        walker.Walk(
            [("arguments", arguments)],
            node =>
            {
                switch ( node.Object )
                {
                    case Compilation:
                    case SyntaxTree:
                    case SemanticModel:
                    case ISymbol:
                    case ICompilationElement:
                        retentionPath = node.FormatPath();

                        return ObjectGraphAction.Stop;

                    // Not traversed, for the same reason as in the memory leak tests: these are not what a chain
                    // reported here would be acted upon, and following them multiplies the cost of the walk.
                    case string:
                    case Type:
                    case System.Reflection.MemberInfo:
                    case System.Reflection.ParameterInfo:
                        return ObjectGraphAction.Skip;

                    default:
                        return ObjectGraphAction.Traverse;
                }
            } );

        if ( retentionPath != null )
        {
            throw new AssertionFailedException(
                $"An argument of the diagnostic '{diagnostic.Id}' reaches a compilation, so the diagnostic cannot be "
                + "held across compilations. Add the type of the argument to "
                + "DiagnosticDescriptorExtensions.IsCompilationBound so that it is formatted when the diagnostic is "
                + "created." + Environment.NewLine + retentionPath );
        }
    }
}
