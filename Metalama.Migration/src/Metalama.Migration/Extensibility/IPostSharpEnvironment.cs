// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Project;
using System.Reflection;

namespace PostSharp.Extensibility
{
    /// <summary>
    /// In Metalama, use <see cref="MetalamaExecutionContext"/>.
    /// </summary>
    public interface IPostSharpEnvironment
    {
        IProject CurrentProject { get; }

        Assembly LoadAssemblyFromFile( string fileName );
    }
}