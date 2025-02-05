// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
    [Guid( "982B62AD-5BB5-4B44-A7B2-2E3BEB19DE9E" )]
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
    [Guid( "2D800D48-3BF1-4EF8-98F5-62FA4417F3F7" )]
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