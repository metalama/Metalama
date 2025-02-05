// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Engine.Templating;

internal sealed record LocalFunctionInfo( IType ReturnType, bool IsAsync );