// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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