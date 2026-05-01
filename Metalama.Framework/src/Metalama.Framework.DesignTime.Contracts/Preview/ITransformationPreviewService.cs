// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using Microsoft.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.DesignTime.Contracts.Preview
{
    /// <summary>
    /// Defines a method that allows to transform a single syntax tree in a compilation. This service
    /// is used to produce the diff view between original code and transform code.
    /// </summary>
    [ComImport]
    [Guid( "7A873458-5842-4FA5-B67F-D056DB2F245C" )]
    public interface ITransformationPreviewService : ICompilerService
    {
        /// <summary>
        /// Transforms a single syntax tree in a compilation.
        /// </summary>
        Task PreviewTransformationAsync(
            Document document,
            IPreviewTransformationResult[] result,
            CancellationToken cancellationToken );
    }

    /// <summary>
    /// Defines a method that allows to see the generated code for a single file.
    /// This service is used to produce the generated code for introduced types.
    /// </summary>
    [ComImport]
    [Guid( "1053A716-1A04-4B5E-AC27-3C2D6F3BBC9C" )]
    public interface ITransformationPreviewService2 : ITransformationPreviewService
    {
        /// <summary>
        /// Transforms a single file.
        /// </summary>
        /// <param name="project">The project that contains the file that is being previewed.</param>
        /// <param name="filePath">Path to the generated file that is being previewed. This file shouldn't exist in the original project.</param>
        Task PreviewGeneratedFileAsync(
            Project project,
            string filePath,
            IPreviewTransformationResult[] result,
            CancellationToken cancellationToken );
    }
}