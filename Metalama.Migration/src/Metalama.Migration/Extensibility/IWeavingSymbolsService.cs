// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using PostSharp.Constraints;
using System;

namespace PostSharp.Extensibility
{
    /// <summary>
    /// No equivalent in Metalama.
    /// </summary>
    [InternalImplement]
    public interface IWeavingSymbolsService : IService
    {
        void PushAnnotation(
            object targetDeclaration,
            Type annotationClass = null,
            string arguments = null,
            string description = null,
            int linesOfCodeAvoided = 0 );
    }
}