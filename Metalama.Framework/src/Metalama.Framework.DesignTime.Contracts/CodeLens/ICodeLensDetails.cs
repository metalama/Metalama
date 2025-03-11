// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.CodeLens;

/// <summary>
/// The base interface for the result of the <see cref="ICodeLensService.GetCodeLensDetailsAsync"/> method.
/// </summary>
/// <seealso cref="ICodeLensDetailsTable"/>
[ComImport]
[Guid( "FFFB9B14-7D4A-4BC4-AD83-2495A9DC5AC0" )]
public interface ICodeLensDetails;