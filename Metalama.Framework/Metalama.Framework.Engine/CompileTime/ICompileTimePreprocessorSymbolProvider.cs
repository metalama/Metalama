// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Services;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CompileTime;

public interface ICompileTimePreprocessorSymbolProvider : IProjectService
{
    public IReadOnlyList<string> PreprocessorSymbols { get; }
}