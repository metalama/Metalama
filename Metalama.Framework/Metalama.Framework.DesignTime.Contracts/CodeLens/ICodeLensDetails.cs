// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.CodeLens;

/// <summary>
/// The base interface for the result of the <see cref="ICodeLensService.GetCodeLensDetailsAsync"/> method.
/// </summary>
/// <seealso cref="ICodeLensDetailsTable"/>
[ComImport]
[Guid( "FFFB9B14-7D4A-4BC4-AD83-2495A9DC5AC0" )]
public interface ICodeLensDetails;