// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @Include(../../_TrimAttribute.cs)
// @IgnoredDiagnostic(LAMA5206)
#endif

using Metalama.Patterns.Contracts;
using System.Windows;

namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.ContractsIntegration;

internal class WithValidateMethod : DependencyObject
{
    [DependencyProperty]
    [Trim]
    public string Name { get; set; }

    private void ValidateName( string name )
    {
        if ( name.Length > 3 )
        {
            throw new ArgumentOutOfRangeException();
        }
    }
}