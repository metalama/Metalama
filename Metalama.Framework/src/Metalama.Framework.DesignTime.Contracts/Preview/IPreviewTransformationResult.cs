// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.Preview
{
    [ComImport]
    [Guid( "56DF8D75-6AA9-4669-976A-1BB79D5D783C" )]
    public interface IPreviewTransformationResult
    {
        bool IsSuccessful { get; set; }

        SyntaxTree? TransformedSyntaxTree { get; set; }

        string[]? ErrorMessages { get; set; }
    }
}