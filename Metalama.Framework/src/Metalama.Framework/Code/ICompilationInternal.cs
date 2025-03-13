// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Options;
using System.Collections.Generic;

namespace Metalama.Framework.Code
{
    internal interface ICompilationInternal : ICompilation
    {
        ICompilationHelpers Helpers { get; }

        IAspectRepository AspectRepository { get; }

        IHierarchicalOptionsManager HierarchicalOptionsManager { get; }

        IEnumerable<T> GetAnnotations<T>( IDeclaration declaration )
            where T : class, IAnnotation;
    }
}