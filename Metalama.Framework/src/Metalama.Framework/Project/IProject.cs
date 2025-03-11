// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Services;
using Metalama.Framework.Utilities;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Project
{
    /// <summary>
    /// Exposes the properties of the current C# project, such as <see cref="Path"/>, <see cref="AssemblyReferences"/>, <see cref="PreprocessorSymbols"/>,
    /// <see cref="Configuration"/> or <see cref="TargetFramework"/>. To access a custom MSBuild property, use <see cref="TryGetProperty"/>. You can extend
    /// this interface with your own framework-specific by using the <see cref="Extension{T}"/> method.
    /// </summary>
    [InternalImplement]
    [CompileTime]
    public interface IProject
    {
        /// <summary>
        /// Gets the project name, i.e. the <see cref="Path"/> without the directory and without the extension.
        /// </summary>
        string? Name { get; }

        /// <summary>
        /// Gets the name of the assembly produced by the project.
        /// </summary>
        string AssemblyName { get; }

        /// <summary>
        /// Gets the path to the <c>csproj</c> file.
        /// </summary>
        string? Path { get; }

        /// <summary>
        /// Gets the list of assembly references of the current project.
        /// </summary>
        ImmutableArray<IAssemblyIdentity> AssemblyReferences { get; }

        /// <summary>
        /// Gets the list of defined preprocessor symbols like <c>DEBUG</c>, <c>TRACE</c>, <c>NET5_0_OR_GREATER</c> and so on.
        /// </summary>
        ImmutableHashSet<string> PreprocessorSymbols { get; }

        /// <summary>
        /// Gets the name of the build configuration, for instance <c>Debug</c> or <c>Release</c>.
        /// </summary>
        string? Configuration { get; }

        /// <summary>
        /// Gets the identifier of the target framework, for instance <c>netstandard2.0</c>.
        /// </summary>
        string? TargetFramework { get; }

        /// <summary>
        /// Gets the set of properties passed from MSBuild. To expose an MSBuild property to this collection,
        /// define the <c>CompilerVisibleProperty</c> item. 
        /// </summary>
        /// <seealso href="@reading-msbuild-properties"/>
        bool TryGetProperty( string name, [NotNullWhen( true )] out string? value );

        /// <summary>
        /// Gets a project extension object or creates a new instance if none has been created before. The type must derive from <see cref="ProjectExtension"/>
        /// and have a default constructor. New instances will be initialized using <see cref="ProjectExtension.Initialize"/>.
        /// </summary>
        /// <typeparam name="T">The extension type, which must derive from <see cref="ProjectExtension"/> and have a default constructor.</typeparam>
        [Obsolete( "Use IDeclaration.Enhancements().GetOptions<T> to get or amender.Configure<T>(...) to set an option." )]
        T Extension<T>()
            where T : ProjectExtension, new();

        /// <summary>
        /// Gets an <see cref="IServiceProvider{TBase}"/> that gives access to the compiler services exposed using the <c>[CompileTimePlugIn]</c> facility.
        /// Only interfaces that derive from <see cref="IProjectService"/> are accessible from this property.
        /// </summary>
        IServiceProvider<IProjectService> ServiceProvider { get; }
    }
}