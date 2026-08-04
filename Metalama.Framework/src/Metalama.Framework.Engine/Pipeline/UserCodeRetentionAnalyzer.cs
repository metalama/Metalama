// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Infrastructure;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Engine.Utilities.ObjectGraph;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Metalama.Framework.Engine.Pipeline;

/// <summary>
/// Reports the references, held by compile-time code that the design-time pipeline keeps beyond the run that produced
/// it, that prevent a Roslyn <see cref="Compilation"/> from being released.
/// </summary>
/// <remarks>
/// <para>
/// Two families of objects are concerned. A fabric runs once per pipeline configuration, not once per compilation, and
/// everything it registers becomes an <see cref="IPipelineContributor"/> stored in that configuration, which is carried
/// forward at design time across every new compilation until compile-time code changes. Separately, the design-time
/// pipeline files the inheritable aspect instances, the transitive contributors and the annotations under the path of
/// their document, and carries them forward across every version in which that document did not change. In both cases a
/// field of the user's own object that holds an <see cref="INamedType"/> pins the version of the project in which it was
/// set, for as long as the solution stays open.
/// </para>
/// <para>
/// The analysis is a check on the shape of what that code left behind. Nothing leaks during the batch compilation that
/// reports it, because that process handles one compilation and exits. It is reported at compile time because the same
/// objects are built in both hosts, so a batch build reproduces what an editing session would retain, without paying the
/// cost of the walk on the hot path of the analysis process.
/// </para>
/// <para>
/// The walk runs after the pipeline has executed, not immediately after the fabrics have run, because a fabric captures
/// a declaration in two different moments: while <c>AmendProject</c> builds the query, and while the query is executed
/// against a compilation. A field filled during execution is the more damaging of the two, since it grows with every
/// version of the project, and it is empty at the moment the fabrics return. Running after the execution is also what
/// makes the aspect instances available.
/// </para>
/// <para>
/// This diagnostic does not duplicate the compile-time serializer, which already refuses a declaration in a field of an
/// externally inheritable aspect, with an error rather than a warning. What the serializer cannot see is a field marked
/// <c>[NonCompileTimeSerialized]</c>, which the design-time cache keeps all the same.
/// </para>
/// <para>
/// The walk is expensive, therefore this class runs only when
/// <see cref="IProjectOptions.DiagnoseMemoryLeaks"/> is set.
/// </para>
/// </remarks>
internal sealed class UserCodeRetentionAnalyzer
{
    /// <summary>
    /// The maximum number of user-code retentions reported as individual diagnostics. The remainder is reported by the
    /// summary diagnostic and written to the report file.
    /// </summary>
    private const int _maxReportedFindings = 20;

    private readonly CompileTimeProject _compileTimeProject;
    private readonly IProjectOptions? _projectOptions;
    private readonly IStandardDirectories? _standardDirectories;
    private readonly ILogger _logger;
    private readonly UserCodeRetentionPolicy _policy;

    /// <summary>
    /// Runs the analysis when the user has asked for it, and does nothing otherwise.
    /// </summary>
    /// <remarks>
    /// The design-time check is a safety net: this method is called from the compile-time pipeline, but that pipeline
    /// is also instantiated by hosts that set a design-time execution scenario, and the walk must never run in the
    /// process whose memory it exists to protect.
    /// </remarks>
    public static void AnalyzeIfEnabled( AspectPipelineResult result, IDiagnosticAdder diagnostics )
    {
        var configuration = result.Configuration;

        if ( configuration.ServiceProvider.GetService<IProjectOptions>() is not { DiagnoseMemoryLeaks: true } )
        {
            return;
        }

        if ( configuration.ServiceProvider.GetService<ExecutionScenario>() is { IsDesignTime: true } )
        {
            return;
        }

        if ( configuration.CompileTimeProject == null )
        {
            return;
        }

        new UserCodeRetentionAnalyzer( configuration.ServiceProvider, configuration.CompileTimeProject )
            .Analyze( result, diagnostics );
    }

    private UserCodeRetentionAnalyzer( in ProjectServiceProvider serviceProvider, CompileTimeProject compileTimeProject )
    {
        this._compileTimeProject = compileTimeProject;
        this._projectOptions = serviceProvider.GetService<IProjectOptions>();
        this._standardDirectories = serviceProvider.Global.GetBackstageService<IStandardDirectories>();
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( nameof(UserCodeRetentionAnalyzer) );
        this._policy = UserCodeRetentionPolicy.Create( compileTimeProject );
    }

    /// <summary>
    /// Walks the object graph reachable from everything that the design-time pipeline would keep beyond the run that
    /// produced it, and reports what pins the compilation.
    /// </summary>
    /// <param name="result">The result of the pipeline, from which the roots of the walk are taken.</param>
    /// <param name="diagnostics">The sink to which the findings are reported.</param>
    private void Analyze( AspectPipelineResult result, IDiagnosticAdder diagnostics )
    {
        var roots = new List<(string Name, object Root)>();
        var userCodeRoots = new HashSet<object>( ReferenceEqualityComparer<object>.Instance );

        this.AddFabricContributorRoots( result, roots );
        this.AddTransitiveOutputRoots( result, roots );
        this.AddStaticFieldRoots( roots, userCodeRoots );

        var walkResult = FindRetentions( roots, userCodeRoots, this._policy, out var findings );

        this.Report( findings, walkResult, diagnostics );
    }

    /// <summary>
    /// Walks the graph reachable from <paramref name="roots"/> and collects one finding per object that pins a
    /// compilation.
    /// </summary>
    /// <param name="roots">The objects from which the walk starts.</param>
    /// <param name="userCodeRoots">
    /// The roots that are themselves user code, such as a static field of a compile-time type. A finding whose chain
    /// contains no user type is attributed to such a root when it starts from one.
    /// </param>
    /// <param name="policy">The policy that decides what pins a compilation and what belongs to the user.</param>
    /// <param name="findings">The findings, one per distinct pinning object, each with the shortest chain reaching it.</param>
    internal static ObjectGraphWalkResult FindRetentions(
        IReadOnlyList<(string Name, object Root)> roots,
        HashSet<object> userCodeRoots,
        UserCodeRetentionPolicy policy,
        out IReadOnlyList<Finding> findings )
    {
        var collected = new List<Finding>();

        var walkResult = new ObjectGraphWalker().Walk(
            roots,
            node =>
            {
                if ( UserCodeRetentionPolicy.IsBoundary( node.Object ) )
                {
                    return ObjectGraphAction.Skip;
                }

                if ( UserCodeRetentionPolicy.IsPinning( node.Object ) )
                {
                    collected.Add( CreateFinding( node, userCodeRoots, policy ) );

                    // The internal graph of a compilation-bound object is large and belongs to another component. The
                    // walk therefore reports it and stops, which is also what bounds the cost of the analysis.
                    return ObjectGraphAction.Skip;
                }

                return ObjectGraphAction.Traverse;
            } );

        findings = collected;

        return walkResult;
    }

    /// <summary>
    /// Adds, as roots, everything the fabrics registered, which is what the pipeline configuration retains.
    /// </summary>
    /// <remarks>
    /// The fabric instances themselves are reachable from these objects, so they do not need to be roots of their own:
    /// a fabric that registered nothing is not retained at all and therefore cannot leak.
    /// </remarks>
    private void AddFabricContributorRoots( AspectPipelineResult result, List<(string Name, object Root)> roots )
    {
        var contributors = result.Configuration.FabricsContributors?.Contributors ?? ImmutableArray<IPipelineContributor>.Empty;

        for ( var i = 0; i < contributors.Length; i++ )
        {
            roots.Add( ($"fabric contributor #{i} ({ObjectGraphNode.FormatType( contributors[i].GetType() )})", contributors[i]) );
        }
    }

    /// <summary>
    /// Adds, as roots, the objects that the design-time pipeline files under a document path and carries forward from
    /// one version of the project to the next.
    /// </summary>
    /// <remarks>
    /// <para>
    /// `SyntaxTreePipelineResult` describes itself as compilation-independent and cacheable, and the pipeline
    /// re-analyses only the files that changed, so anything it holds outlives every version of the project in which the
    /// file was not touched. Its contents are not all serialized: serialization happens when a result crosses a project
    /// boundary, whereas within a project the objects are kept as they are. An `InheritableAspectInstance` converts its
    /// target declaration to a durable reference, but its `Aspect` and `AspectState` are the user's own objects, held
    /// live, and a field of one of them that holds a declaration pins the compilation exactly as a fabric field does.
    /// </para>
    /// <para>
    /// What is walked here is therefore the design-time form of each output, not the form the compile-time pipeline
    /// happens to hold: an inheritable aspect instance is constructed as the design-time pipeline constructs it, and a
    /// transitive contributor is converted with <see cref="ITransitivePipelineContributor.ToDesignTime"/>. Walking the
    /// raw objects instead would report the conversions that those two steps exist to perform.
    /// </para>
    /// </remarks>
    private void AddTransitiveOutputRoots( AspectPipelineResult result, List<(string Name, object Root)> roots )
    {
        foreach ( var aspectInstance in result.ExternallyInheritableAspects )
        {
            try
            {
                roots.Add( ($"inheritable aspect '{aspectInstance.AspectClass.ShortName}'", new InheritableAspectInstance( aspectInstance )) );
            }
            catch ( Exception e )
            {
                this._logger.Warning?.Log( $"Cannot build the design-time form of an inheritable aspect instance: {e.Message}" );
            }
        }

        foreach ( var contributor in result.TransitiveContributors )
        {
            try
            {
                if ( contributor.ToDesignTime() is { } designTimeContributor )
                {
                    roots.Add(
                        ($"transitive contributor ({ObjectGraphNode.FormatType( designTimeContributor.GetType() )})", designTimeContributor) );
                }
            }
            catch ( Exception e )
            {
                this._logger.Warning?.Log( $"Cannot build the design-time form of a transitive contributor: {e.Message}" );
            }
        }

        foreach ( var annotationsOnDeclaration in result.Annotations )
        {
            foreach ( var annotation in annotationsOnDeclaration )
            {
                // Only the annotation is stored at design time; its target declaration is stored as a serializable
                // identifier and is therefore not a root.
                roots.Add( ($"annotation ({ObjectGraphNode.FormatType( annotation.Annotation.GetType() )})", annotation.Annotation) );
            }
        }
    }

    /// <summary>
    /// Adds, as roots, the value of every static field declared by the compile-time assemblies of the project.
    /// </summary>
    /// <remarks>
    /// A static field of a fabric outlives every configuration, so it is a stronger retention than an instance field,
    /// and it is invisible to a walk that starts from the fabric instances alone. Reading a static field runs the type
    /// initializer of its declaring type, therefore every read is guarded individually and a failure only makes the
    /// analysis less complete.
    /// </remarks>
    private void AddStaticFieldRoots( List<(string Name, object Root)> roots, HashSet<object> userCodeRoots )
    {
        foreach ( var project in this._compileTimeProject.ClosureProjects )
        {
            Type[] types;

            try
            {
                types = project.GetDeclaredTypes();
            }
            catch ( Exception e )
            {
                this._logger.Warning?.Log( $"Cannot enumerate the types of the compile-time project '{project.CompileTimeIdentity.Name}': {e.Message}" );

                continue;
            }

            foreach ( var type in types )
            {
                FieldInfo[] fields;

                try
                {
                    fields = type.GetFields( BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly );
                }
                catch ( Exception )
                {
                    continue;
                }

                foreach ( var field in fields )
                {
                    if ( field.FieldType.IsPrimitive || field.FieldType.IsEnum || field.FieldType.IsPointer )
                    {
                        continue;
                    }

                    object? value;

                    try
                    {
                        value = field.GetValue( null );
                    }
                    catch ( Exception )
                    {
                        continue;
                    }

                    if ( value != null )
                    {
                        roots.Add( ($"static field '{type.FullName}.{field.Name}'", value) );
                        userCodeRoots.Add( value );
                    }
                }
            }
        }
    }

    private static Finding CreateFinding( ObjectGraphNode node, HashSet<object> userCodeRoots, UserCodeRetentionPolicy policy )
    {
        var path = node.GetPath();
        var userType = policy.FindUserCodeTypeName( path );

        if ( userType == null && userCodeRoots.Contains( path[0].Object ) )
        {
            // The root itself is user code, even though the object it holds is not: this is the case of a static field
            // of a compile-time type whose value is a code-model object.
            userType = path[0].Label;
        }

        return new Finding( node, userType );
    }

    private void Report( IReadOnlyList<Finding> findings, ObjectGraphWalkResult walkResult, IDiagnosticAdder diagnostics )
    {
        var userCodeFindings = findings.Where( f => f.UserType != null ).ToReadOnlyList();
        var frameworkFindingCount = findings.Count - userCodeFindings.Count;

        foreach ( var finding in userCodeFindings.Take( _maxReportedFindings ) )
        {
            diagnostics.Report(
                GeneralDiagnosticDescriptors.UserCodePinsCompilation.CreateRoslynDiagnostic(
                    null,
                    (finding.UserType!, ObjectGraphNode.FormatType( finding.Node.Object.GetType() ), FormatCompactPath( finding.Node )),
                    description: finding.Node.FormatPath() ) );
        }

        var completeness = walkResult.IsExhausted
            ? "The walk of the object graph was interrupted before it completed, therefore this result is a lower bound."
            : "The whole object graph reachable from the retained objects was analysed.";

        var report = this.FormatReport( findings, walkResult, completeness );
        var reportPath = this.TryWriteReport( report ) ?? "(the report file could not be written)";

        this._logger.Info?.Log( report );

        diagnostics.Report(
            GeneralDiagnosticDescriptors.UserCodeRetentionAnalysisCompleted.CreateRoslynDiagnostic(
                null,
                (userCodeFindings.Count, frameworkFindingCount, completeness, reportPath) ) );
    }

    /// <summary>
    /// Formats the chain of references on a single line, so that it fits in a diagnostic message.
    /// </summary>
    private static string FormatCompactPath( ObjectGraphNode node ) => string.Join( " -> ", node.GetPath().SelectAsArray( n => n.Label ) );

    private string FormatReport( IReadOnlyList<Finding> findings, ObjectGraphWalkResult walkResult, string completeness )
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine( "Analysis of the references retained by compile-time code" );
        stringBuilder.AppendLine( "============================================" );
        stringBuilder.AppendLine();
        stringBuilder.AppendLine( $"Project: {this._projectOptions?.AssemblyName} ({this._projectOptions?.TargetFramework})" );
        stringBuilder.AppendLine( $"Objects visited: {walkResult.VisitedObjectCount}" );
        stringBuilder.AppendLine( completeness );
        stringBuilder.AppendLine();

        AppendSection( "Retentions in compile-time user code", findings.Where( f => f.UserType != null ) );
        AppendSection( "Retentions in Metalama itself", findings.Where( f => f.UserType == null ) );

        return stringBuilder.ToString();

        void AppendSection( string title, IEnumerable<Finding> section )
        {
            var sectionFindings = section.ToReadOnlyList();

            stringBuilder.AppendLine( title );
            stringBuilder.AppendLine( new string( '-', title.Length ) );
            stringBuilder.AppendLine();

            if ( sectionFindings.Count == 0 )
            {
                stringBuilder.AppendLine( "None." );
                stringBuilder.AppendLine();

                return;
            }

            foreach ( var finding in sectionFindings )
            {
                stringBuilder.AppendLine( $"{ObjectGraphNode.FormatType( finding.Node.Object.GetType() )} is retained{FormatOrigin( finding )}:" );
                stringBuilder.AppendLine( finding.Node.FormatPath() );
                stringBuilder.AppendLine();
            }
        }

        static string FormatOrigin( Finding finding ) => finding.UserType == null ? "" : $" by '{finding.UserType}'";
    }

    /// <summary>
    /// Writes the report next to the other Metalama diagnostic files, and returns its path, or <c>null</c> when it
    /// could not be written.
    /// </summary>
    /// <remarks>
    /// The name is derived from the project rather than being unique, so that repeated builds replace the report of the
    /// previous one instead of accumulating files that the user would have to sort by date.
    /// </remarks>
    private string? TryWriteReport( string report )
    {
        if ( this._standardDirectories == null )
        {
            return null;
        }

        var directory = Path.Combine( this._standardDirectories.TempDirectory, "FabricRetentionReports" );
        var name = $"{this._projectOptions?.AssemblyName ?? "project"}-{this._projectOptions?.TargetFramework ?? "unknown"}.txt";
        var file = Path.Combine( directory, name );

        try
        {
            Directory.CreateDirectory( directory );
            File.WriteAllText( file, report );

            return file;
        }
        catch ( Exception e )
        {
            this._logger.Warning?.Log( $"Cannot write the fabric retention report to '{file}': {e.Message}" );

            return null;
        }
    }

    /// <summary>
    /// One object that pins a compilation, together with the chain of references that reaches it.
    /// </summary>
    /// <param name="Node">The pinning object and its incoming chain of references.</param>
    /// <param name="UserType">
    /// The compile-time type of the user that holds the reference, or <c>null</c> when the whole chain belongs to
    /// Metalama.
    /// </param>
    internal sealed record Finding( ObjectGraphNode Node, string? UserType );
}
