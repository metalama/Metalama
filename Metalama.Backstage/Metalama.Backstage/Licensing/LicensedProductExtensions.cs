// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Backstage.Licensing;

internal static class LicensedProductExtensions
{
    public static bool IsSupportedBeforeMetalama20251( this LicensedProduct product ) => product is not LicensedProduct.MetalamaCommunity;
}