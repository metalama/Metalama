// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Extensibility.CallSites;
using Metalama.Framework.Engine.ReferenceGraph;
using Metalama.Framework.Tests.ExtensionPoints.Engine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

[assembly: ExportExtension( typeof(TestExtensionPointsPipelineExtension), ExtensionKinds.Default )]

namespace Metalama.Framework.Tests.ExtensionPoints.Engine;

/// <summary>
/// The pipeline extension of the proof of concept. It uses the public extension points of the engine only.
/// </summary>
public sealed class TestExtensionPointsPipelineExtension : PipelineExtension
{
    /// <summary>
    /// The warning that describes each registration, so that the expected output of an aspect test shows what the extension observed.
    /// </summary>
    internal static DiagnosticDefinition<(string Tag, string Channel, string Origin, string PredecessorKind, string TemplateProvider)>
        RegistrationObserved { get; } = new(
        "TEST0001",
        Severity.Warning,
        "Registration '{0}' through the {1}: origin '{2}', predecessor {3}, template provider {4}." );

    /// <summary>
    /// The warning that describes each reference that the extension reads from the shared index of source references.
    /// </summary>
    internal static DiagnosticDefinition<(string MethodName, string ReferencingSymbol, string ReferenceKinds, bool IsRestricted)> ReferenceObserved { get; } =
        new( "TEST0002", Severity.Warning, "Reference to '{0}' from '{1}' ({2}), index restricted to declaration roots: {3}." );

    /// <summary>
    /// The warning that lists the names of all the symbols that the shared index of source references contains, so that the expected output of
    /// an aspect test shows that the index contains nothing else than what the extensions requested.
    /// </summary>
    internal static DiagnosticDefinition<string> IndexContent { get; } =
        new( "TEST0005", Severity.Warning, "The shared index contains references to: {0}." );

    /// <summary>
    /// The warning that reports a redirection that the factory of transformations refused, with the message of the exception.
    /// </summary>
    internal static DiagnosticDefinition<(string Site, string ExceptionType, string Message)> RedirectionRefused { get; } =
        new( "TEST0003", Severity.Warning, "The redirection of '{0}' was refused with {1}: {2}" );

    /// <summary>
    /// The error that reports a declaration selected by a query that is not contained in the declaration of the owner of the query.
    /// </summary>
    internal static DiagnosticDefinition<(FormattableString Predecessor, IDeclaration Child, IDeclaration Parent)> ScopeNotContained { get; } =
        new( "TEST0004", Severity.Error, "{0} cannot redirect the calls in '{1}', because '{1}' is not contained in '{2}'." );

    public override bool Initialize( PipelineExtensionInitializationContext context )
    {
        context.ServiceBuilder.Add( _ => new TestExtensionPointsService() );
        context.AddDiagnosticDefinitions( [RegistrationObserved, ReferenceObserved, IndexContent, RedirectionRefused, ScopeNotContained] );

        return true;
    }

    /// <summary>
    /// Returns one requirement per requested method name for invocations and for the default reference kind, which includes method groups.
    /// </summary>
    public override SourceIndexRequirements GetSourceIndexRequirements( SourceIndexRequirementsContext context )
    {
        var consumers = context.Contributors.OfKind( TestContributorKinds.ReferenceReport )
            .Select( r => (r.MethodName, r.DeclarationRoots) )
            .Concat( context.Contributors.OfKind( TestContributorKinds.Redirection ).Select( r => (r.MethodName, r.DeclarationRoots) ) )
            .ToList();

        if ( consumers.Count == 0 )
        {
            return SourceIndexRequirements.None;
        }

        var requirements = consumers
            .Select( r => new ReferenceIndexerRequirements( ReferenceKinds.Invocation | ReferenceKinds.Default, false, DeclarationKind.Method, r.MethodName ) )
            .ToImmutableArray();

        var roots = consumers.All( r => !r.DeclarationRoots.IsDefault )
            ? consumers.SelectMany( r => r.DeclarationRoots ).ToImmutableArray()
            : default;

        return new SourceIndexRequirements( requirements ) { DeclarationRoots = roots };
    }

    public override async Task ExecuteTransformingContributorsAsync( ExtensionTransformationContext context, CancellationToken cancellationToken )
    {
        ReportRegistrations( context.Contributors.OfKind( TestContributorKinds.Registration ), context.FinalCompilation, context.Diagnostics );
        await ReportReferencesAsync( context, cancellationToken );
        await RedirectCallsAsync( context, cancellationToken );
    }

    /// <summary>
    /// Reads the references of the requested methods from the shared index, keeps those that are inside the scope of each request, and passes
    /// them to the factory of transformations. A refused request is reported as a warning, so that the expected output of a test shows it.
    /// </summary>
    private static async Task RedirectCallsAsync( ExtensionTransformationContext context, CancellationToken cancellationToken )
    {
        var redirections = context.Contributors.OfKind( TestContributorKinds.Redirection ).ToList();

        if ( redirections.Count == 0 )
        {
            return;
        }

        var compilation = context.FinalCompilation;
        var index = await context.SourceReferenceIndex.GetIndexAsync( cancellationToken );
        var factory = context.TransformationFactory;

        foreach ( var redirection in redirections )
        {
            var roots = await GetScopeRootsAsync( redirection, context, cancellationToken );
            var replacement = GetReplacement( redirection, compilation );

            var sites = index.ReferencedSymbols
                .Where( s => s.ReferencedSymbol.Kind == SymbolKind.Method && s.ReferencedSymbol.Name == redirection.MethodName )
                .SelectMany( s => s.References.SelectMany( r => r.Nodes ) )
                .Select( n => n.Syntax.IsNode ? n.Syntax.AsNode()! : n.Syntax.Parent! )
                .Where( n => roots.Any( r => r.SyntaxTree == n.SyntaxTree && r.FullSpan.Contains( n.Span ) ) )
                .Distinct()
                .OrderBy( n => n.SyntaxTree.FilePath, StringComparer.Ordinal )
                .ThenBy( n => n.SpanStart )
                .ToList();

            foreach ( var site in sites )
            {
                var invocation = GetInvocation( site );

                try
                {
                    if ( invocation != null && !redirection.Options.MethodReferences )
                    {
                        factory.RedirectInvocation( redirection.Origin, CreateInvocationRequest( invocation, replacement, redirection.Options, compilation ) );
                    }
                    else if ( invocation == null && redirection.Options.MethodReferences )
                    {
                        factory.RedirectMethodReference(
                            redirection.Origin,
                            new MethodReferenceRedirectionRequest(
                                (ExpressionSyntax) site,
                                CallSiteRedirectionTarget.Existing( replacement ),
                                ParseReceiverMode( redirection.Options ) )
                            {
                                TypeArguments = redirection.Options.ExplicitTypeArguments ? GetTypeArguments( site, compilation ) : default
                            } );
                    }
                }
                catch ( Exception e ) when ( e is ArgumentException or InvalidOperationException )
                {
                    context.Diagnostics.Report(
                        RedirectionRefused.CreateRoslynDiagnostic(
                            site.GetLocation(),
                            ((invocation ?? GetMethodGroup( site )).ToString(), e.GetType().Name, GetMessage( e )) ) );
                }
            }
        }
    }

    /// <summary>
    /// Returns the message of an exception without the name of the parameter that <see cref="ArgumentException"/> appends, because its format
    /// differs between .NET Framework and .NET.
    /// </summary>
    private static string GetMessage( Exception exception )
    {
        var message = exception.Message;

        if ( exception is ArgumentException { ParamName: { } parameterName } )
        {
            foreach ( var suffix in new[] { $" (Parameter '{parameterName}')", $"{Environment.NewLine}Parameter name: {parameterName}" } )
            {
                if ( message.EndsWith( suffix, StringComparison.Ordinal ) )
                {
                    return message.Substring( 0, message.Length - suffix.Length );
                }
            }
        }

        return message;
    }

    /// <summary>
    /// Creates the request that redirects an invocation to a replacement method according to the options of a <see cref="TestRedirection"/>.
    /// </summary>
    private static InvocationRedirectionRequest CreateInvocationRequest(
        InvocationExpressionSyntax invocation,
        IMethod replacement,
        TestRedirectionOptions options,
        CompilationModel compilation )
    {
        var semanticModel = compilation.RoslynCompilation.GetSemanticModel( invocation.SyntaxTree );
        var sourceMethod = (IMethodSymbol) semanticModel.GetSymbolInfo( invocation ).Symbol!;

        return new InvocationRedirectionRequest( invocation, CallSiteRedirectionTarget.Existing( replacement ), ParseReceiverMode( options ) )
        {
            Arguments = options.Arguments == null ? default : ParseItems( options.Arguments ).Select( x => ParseArgument( x, compilation ) ).ToImmutableArray(),
            ExtraArguments = options.ExtraArguments == null ? default : ParseItems( options.ExtraArguments ).Select( ParseExtraArgument ).ToImmutableArray(),
            ResultCast = options.CastResult ? compilation.Factory.GetIType( sourceMethod.ReturnType ) : null,
            TypeArguments = options.ExplicitTypeArguments ? GetTypeArguments( invocation.Expression, compilation ) : default
        };
    }

    /// <summary>
    /// Returns the type arguments of the method to which a node binds, or of its single candidate when the binding fails.
    /// </summary>
    private static ImmutableArray<IType> GetTypeArguments( SyntaxNode node, CompilationModel compilation )
    {
        var semanticModel = compilation.RoslynCompilation.GetSemanticModel( node.SyntaxTree );
        var symbolInfo = semanticModel.GetSymbolInfo( node );
        var method = (IMethodSymbol) (symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.Single());

        return method.TypeArguments.Select( t => compilation.Factory.GetIType( t ) ).ToImmutableArray();
    }

    /// <summary>
    /// Parses <see cref="TestRedirectionOptions.ReceiverMode"/> as a <see cref="CallSiteReceiverMode"/>.
    /// </summary>
    private static CallSiteReceiverMode ParseReceiverMode( TestRedirectionOptions options )
        => (CallSiteReceiverMode) Enum.Parse( typeof(CallSiteReceiverMode), options.ReceiverMode );

    /// <summary>
    /// Splits a list of items separated by semicolons, trims the items and removes the empty ones.
    /// </summary>
    private static IEnumerable<string> ParseItems( string list ) => list.Split( ';' ).Select( x => x.Trim() ).Where( x => x.Length > 0 );

    /// <summary>
    /// Parses an item of <see cref="TestRedirectionOptions.Arguments"/>.
    /// </summary>
    private static RedirectedArgument ParseArgument( string item, CompilationModel compilation )
    {
        // The suffix " as <reflection name>" casts the argument to the named type.
        var asIndex = item.IndexOf( " as ", StringComparison.Ordinal );

        if ( asIndex > 0 )
        {
            var castType = compilation.Factory.GetTypeByReflectionName( item.Substring( asIndex + 4 ).Trim() );

            return ParseArgument( item.Substring( 0, asIndex ), compilation ).WithCast( castType );
        }

        string? name = null;
        var equals = item.IndexOf( '=' );
        var colon = item.IndexOf( ':' );

        if ( equals > 0 && (colon < 0 || equals < colon) )
        {
            name = item.Substring( 0, equals ).Trim();
            item = item.Substring( equals + 1 ).Trim();
            colon = item.IndexOf( ':' );
        }

        var kind = colon < 0 ? item : item.Substring( 0, colon );
        var value = colon < 0 ? "" : item.Substring( colon + 1 );

        var argument = kind switch
        {
            "receiver" => RedirectedArgument.SourceReceiver,
            "argument" => RedirectedArgument.SourceArgument( int.Parse( value, CultureInfo.InvariantCulture ) ),
            "value" => RedirectedArgument.Value( SyntaxFactory.ParseExpression( value ) ),
            _ => throw new InvalidOperationException( $"Unknown argument item: '{item}'." )
        };

        return name == null ? argument : argument.WithName( name );
    }

    /// <summary>
    /// Parses an item of <see cref="TestRedirectionOptions.ExtraArguments"/>, which has the form <c>name=E</c>.
    /// </summary>
    private static CallSiteExtraArgument ParseExtraArgument( string item )
    {
        var equals = item.IndexOf( '=' );

        return new CallSiteExtraArgument( item.Substring( 0, equals ).Trim(), SyntaxFactory.ParseExpression( item.Substring( equals + 1 ) ) );
    }

    /// <summary>
    /// Returns the invocation whose invoked expression is the given method name, or <c>null</c> when the name is a method group.
    /// </summary>
    private static InvocationExpressionSyntax? GetInvocation( SyntaxNode name )
    {
        var expression = name;

        if ( name.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == name )
        {
            expression = memberAccess;
        }
        else if ( name.Parent is MemberBindingExpressionSyntax memberBinding && memberBinding.Name == name )
        {
            expression = memberBinding;
        }

        return expression.Parent is InvocationExpressionSyntax invocation && invocation.Expression == expression ? invocation : null;
    }

    /// <summary>
    /// Returns the method group that contains the given method name, which is the member access whose name it is, as the factory of
    /// transformations normalizes it.
    /// </summary>
    private static SyntaxNode GetMethodGroup( SyntaxNode name )
        => name.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == name ? memberAccess : name;

    /// <summary>
    /// Returns the replacement method of a redirection in a compilation: the method given by reference, or else the single method that has the
    /// given type name and method name.
    /// </summary>
    private static IMethod GetReplacement( TestRedirection redirection, CompilationModel compilation )
    {
        if ( redirection.Replacement != null )
        {
            return redirection.Replacement.GetTarget( compilation );
        }

        var (typeName, methodName) = redirection.ReplacementName!.Value;

        return compilation.AllTypes.Single( t => t.FullName == typeName ).Methods.OfName( methodName ).Single();
    }

    /// <summary>
    /// Returns the syntax of the declarations that are the scope of a redirection. For a query, the query is evaluated on the final compilation.
    /// </summary>
    private static async Task<IReadOnlyList<SyntaxNode>> GetScopeRootsAsync(
        TestRedirection redirection,
        ExtensionTransformationContext context,
        CancellationToken cancellationToken )
    {
        if ( !redirection.DeclarationRoots.IsDefault )
        {
            return redirection.DeclarationRoots;
        }

        var roots = new List<SyntaxNode>();

        await redirection.ScopeQuery!.InvokeAsync(
            context.FinalCompilation,
            context.Diagnostics,
            ScopeNotContained,
            ( declaration, _, _ ) =>
            {
                lock ( roots )
                {
                    roots.AddRange( declaration.Sources.Select( s => s.SyntaxNodeOrToken().AsNode() ).OfType<SyntaxNode>() );
                }

                return Task.CompletedTask;
            },
            cancellationToken );

        return roots;
    }

    /// <summary>
    /// Reports a diagnostic that lists the names of the symbols in the shared index, and a diagnostic for each reference to a
    /// method named by a <see cref="TestReferenceReport"/>. The references are sorted by file path and position.
    /// </summary>
    private static async Task ReportReferencesAsync( ExtensionTransformationContext context, CancellationToken cancellationToken )
    {
        var methodNames = new HashSet<string>( context.Contributors.OfKind( TestContributorKinds.ReferenceReport ).Select( r => r.MethodName ) );

        if ( methodNames.Count == 0 )
        {
            return;
        }

        var index = await context.SourceReferenceIndex.GetIndexAsync( cancellationToken );

        var indexedNames = index.ReferencedSymbols.Select( s => s.ReferencedSymbol.Name ).Distinct().OrderBy( n => n, StringComparer.Ordinal );
        context.Diagnostics.Report( IndexContent.CreateRoslynDiagnostic( Location.None, string.Join( ", ", indexedNames ) ) );

        var references = index.ReferencedSymbols
            .Where( s => s.ReferencedSymbol.Kind == SymbolKind.Method && methodNames.Contains( s.ReferencedSymbol.Name ) )
            .SelectMany( s => s.References.SelectMany( r => r.Nodes.Select( n => (Symbol: s.ReferencedSymbol, Reference: r, Node: n) ) ) )
            .OrderBy( x => x.Node.Syntax.SyntaxTree?.FilePath, StringComparer.Ordinal )
            .ThenBy( x => x.Node.Syntax.SpanStart );

        foreach ( var (symbol, reference, node) in references )
        {
            context.Diagnostics.Report(
                ReferenceObserved.CreateRoslynDiagnostic(
                    node.Syntax.GetLocation(),
                    (symbol.Name, reference.ReferencingSymbol.ToDisplayString(), node.ReferenceKind.ToString(),
                     context.SourceReferenceIndex.IsRestrictedToDeclarationRoots) ) );
        }
    }

    /// <summary>
    /// Reports a diagnostic for each <see cref="TestExtensionPipelineContributor"/>, in the order of the tags, so that a test can verify what the extension
    /// received.
    /// </summary>
    private static void ReportRegistrations( IEnumerable<TestExtensionPipelineContributor> registrations, CompilationModel compilation, UserDiagnosticSink diagnostics )
    {
        foreach ( var registration in registrations.OrderBy( r => r.Tag ) )
        {
            var scope = registration.Scope?.GetTargetOrNull( compilation );

            var templateProvider = registration.TemplateProviderMatches switch
            {
                null => "not checked",
                true => "as expected",
                false => "not as expected"
            };

            diagnostics.Report(
                RegistrationObserved.CreateRoslynDiagnostic(
                    scope.GetDiagnosticLocation(),
                    (registration.Tag, registration.Channel, registration.Origin.DiagnosticSourceDescription,
                     registration.Origin.Predecessor.Kind.ToString(), templateProvider) ) );
        }
    }
}
