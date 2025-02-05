// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Engine.Diagnostics;

internal interface IUserDiagnosticSink : IDiagnosticSink, IDiagnosticAdder;