// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if TEST_OPTIONS
// @Include(../../_TrimAttribute.cs)
// @IgnoredDiagnostic(LAMA5206)
#endif

using Metalama.Patterns.Contracts;
using System.Windows;

namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.ContractsIntegration;

internal class WithoutValidateMethod : DependencyObject
{
    [DependencyProperty]
    [Trim]
    public string Name { get; set; }
}