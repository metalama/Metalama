// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code.SyntaxBuilders;

[CompileTime]
internal sealed record SwitchStatementSection( SwitchStatementLabel? Label, IExpression? When, IStatementList Statements, bool EndWithBreak );