using Metalama.Framework.Aspects;
using System;

#pragma warning disable CS1717, CS0414

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Issue35XXX
{
    internal class MyAspect : TypeAspect
    {
        [Introduce]
        public void Method()
        {
        }
    }

    // <target>
    [MyAspect]
    internal class C
    {
        public void Foo()
        {
            CreateMap<CatalogSettings, CatalogSettingsModel>()
                .ForMember( model => model.AllowAnonymousUsersToEmailAFriend_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.AllowAnonymousUsersToReviewProduct_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.AllowProductSorting_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.AllowProductViewModeChanging_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.AllowViewUnpublishedProductPage_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.AvailableViewModes, options => options.Ignore() )
                .ForMember( model => model.CategoryBreadcrumbEnabled_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.CompareProductsEnabled_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.DefaultViewMode_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.DisplayDiscontinuedMessageForUnpublishedProducts_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.DisplayTaxShippingInfoFooter_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.DisplayTaxShippingInfoOrderDetailsPage_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.DisplayTaxShippingInfoProductBoxes_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.DisplayTaxShippingInfoProductDetailsPage_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.DisplayTaxShippingInfoShoppingCart_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.DisplayTaxShippingInfoWishlist_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.EmailAFriendEnabled_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ExportImportAllowDownloadImages_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ExportImportCategoriesUsingCategoryName_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ExportImportProductAttributes_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ExportImportProductCategoryBreadcrumb_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ExportImportProductSpecificationAttributes_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ExportImportRelatedEntitiesByName_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ExportImportProductUseLimitedToStores_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ExportImportSplitProductsFile_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.IncludeFullDescriptionInCompareProducts_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.IncludeShortDescriptionInCompareProducts_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ManufacturersBlockItemsToDisplay_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.NewProductsEnabled_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.NewProductsPageSize_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.NewProductsAllowCustomersToSelectPageSize_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.NewProductsPageSizeOptions_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.NotifyCustomerAboutProductReviewReply_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.NotifyStoreOwnerAboutNewProductReviews_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.NumberOfBestsellersOnHomepage_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.NumberOfProductTags_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.PageShareCode_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ProductReviewPossibleOnlyAfterPurchasing_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ProductReviewsMustBeApproved_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.OneReviewPerProductFromCustomer_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ProductReviewsPageSizeOnAccountPage_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ProductReviewsSortByCreatedDateAscending_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ProductsAlsoPurchasedEnabled_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ProductsAlsoPurchasedNumber_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ProductsByTagAllowCustomersToSelectPageSize_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ProductsByTagPageSizeOptions_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ProductsByTagPageSize_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ProductSearchAutoCompleteEnabled_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ProductSearchEnabled_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ProductSearchAutoCompleteNumberOfProducts_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ProductSearchTermMinimumLength_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.RecentlyViewedProductsEnabled_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.RecentlyViewedProductsNumber_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.RemoveRequiredProducts_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.SearchPageAllowCustomersToSelectPageSize_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.SearchPagePageSizeOptions_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.SearchPageProductsPerPage_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ShowBestsellersOnHomepage_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ShowCategoryProductNumberIncludingSubcategories_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ShowCategoryProductNumber_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ShowFreeShippingNotification_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ShowShortDescriptionOnCatalogPages_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ShowGtin_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ShowLinkToAllResultInSearchAutoComplete_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ShowManufacturerPartNumber_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ShowProductImagesInSearchAutoComplete_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ShowProductReviewsOnAccountPage_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ShowProductReviewsPerStore_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ShowProductsFromSubcategories_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ShowShareButton_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ShowSkuOnCatalogPages_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ShowSkuOnProductDetailsPage_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.DisplayDatePreOrderAvailability_OverrideForStore, mo => mo.Ignore() )
                .ForMember( model => model.UseAjaxCatalogProductsLoading_OverrideForStore, mo => mo.Ignore() )
                .ForMember( model => model.SearchPagePriceRangeFiltering_OverrideForStore, mo => mo.Ignore() )
                .ForMember( model => model.SearchPagePriceFrom_OverrideForStore, mo => mo.Ignore() )
                .ForMember( model => model.SearchPagePriceTo_OverrideForStore, mo => mo.Ignore() )
                .ForMember( model => model.SearchPageManuallyPriceRange_OverrideForStore, mo => mo.Ignore() )
                .ForMember( model => model.ProductsByTagPriceRangeFiltering_OverrideForStore, mo => mo.Ignore() )
                .ForMember( model => model.ProductsByTagPriceFrom_OverrideForStore, mo => mo.Ignore() )
                .ForMember( model => model.ProductsByTagPriceTo_OverrideForStore, mo => mo.Ignore() )
                .ForMember( model => model.ProductsByTagManuallyPriceRange_OverrideForStore, mo => mo.Ignore() )
                .ForMember( model => model.EnableManufacturerFiltering_OverrideForStore, mo => mo.Ignore() )
                .ForMember( model => model.EnablePriceRangeFiltering_OverrideForStore, mo => mo.Ignore() )
                .ForMember( model => model.EnableSpecificationAttributeFiltering_OverrideForStore, mo => mo.Ignore() )
                .ForMember( model => model.DisplayFromPrices_OverrideForStore, mo => mo.Ignore() )
                .ForMember( model => model.AttributeValueOutOfStockDisplayTypes, mo => mo.Ignore() )
                .ForMember( model => model.AttributeValueOutOfStockDisplayType_OverrideForStore, mo => mo.Ignore() )
                .ForMember( model => model.SortOptionSearchModel, options => options.Ignore() )
                .ForMember( model => model.ReviewTypeSearchModel, options => options.Ignore() )
                .ForMember( model => model.PrimaryStoreCurrencyCode, options => options.Ignore() )
                .ForMember( model => model.AllowCustomersToSearchWithManufacturerName_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.AllowCustomersToSearchWithCategoryName_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.DisplayAllPicturesOnCatalogPages_OverrideForStore, options => options.Ignore() )
                .ForMember( model => model.ProductUrlStructureTypeId_OverrideForStore, mo => mo.Ignore() )
                .ForMember( model => model.ProductUrlStructureTypes, mo => mo.Ignore() );
        }

        static MapConfiguration<TSource, TDestination> CreateMap<TSource, TDestination>() => null;
    }

    internal class MapConfiguration<TSource, TDestination>
    {
        public MapConfiguration<TSource, TDestination> ForMember<TResult>( Func<TDestination, TResult> member, Action<MappingOptions<TSource, TDestination>> options ) => null;
    }

    internal class MappingOptions<TSource, TDestination>
    {
        public MappingOptions<TSource, TDestination> Ignore() => null;
    }

    internal class CatalogSettings
    {
    }

    internal class CatalogSettingsModel
    {
        public object? AllowAnonymousUsersToEmailAFriend_OverrideForStore { get; set; }
        public object? AllowAnonymousUsersToReviewProduct_OverrideForStore { get; set; }
        public object? AllowProductSorting_OverrideForStore { get; set; }
        public object? AllowProductViewModeChanging_OverrideForStore { get; set; }
        public object? AllowViewUnpublishedProductPage_OverrideForStore { get; set; }
        public object? AvailableViewModes { get; set; }
        public object? CategoryBreadcrumbEnabled_OverrideForStore { get; set; }
        public object? CompareProductsEnabled_OverrideForStore { get; set; }
        public object? DefaultViewMode_OverrideForStore { get; set; }
        public object? DisplayDiscontinuedMessageForUnpublishedProducts_OverrideForStore { get; set; }
        public object? DisplayTaxShippingInfoFooter_OverrideForStore { get; set; }
        public object? DisplayTaxShippingInfoOrderDetailsPage_OverrideForStore { get; set; }
        public object? DisplayTaxShippingInfoProductBoxes_OverrideForStore { get; set; }
        public object? DisplayTaxShippingInfoProductDetailsPage_OverrideForStore { get; set; }
        public object? DisplayTaxShippingInfoShoppingCart_OverrideForStore { get; set; }
        public object? DisplayTaxShippingInfoWishlist_OverrideForStore { get; set; }
        public object? EmailAFriendEnabled_OverrideForStore { get; set; }
        public object? ExportImportAllowDownloadImages_OverrideForStore { get; set; }
        public object? ExportImportCategoriesUsingCategoryName_OverrideForStore { get; set; }
        public object? ExportImportProductAttributes_OverrideForStore { get; set; }
        public object? ExportImportProductCategoryBreadcrumb_OverrideForStore { get; set; }
        public object? ExportImportProductSpecificationAttributes_OverrideForStore { get; set; }
        public object? ExportImportRelatedEntitiesByName_OverrideForStore { get; set; }
        public object? ExportImportProductUseLimitedToStores_OverrideForStore { get; set; }
        public object? ExportImportSplitProductsFile_OverrideForStore { get; set; }
        public object? IncludeFullDescriptionInCompareProducts_OverrideForStore { get; set; }
        public object? IncludeShortDescriptionInCompareProducts_OverrideForStore { get; set; }
        public object? ManufacturersBlockItemsToDisplay_OverrideForStore { get; set; }
        public object? NewProductsEnabled_OverrideForStore { get; set; }
        public object? NewProductsPageSize_OverrideForStore { get; set; }
        public object? NewProductsAllowCustomersToSelectPageSize_OverrideForStore { get; set; }
        public object? NewProductsPageSizeOptions_OverrideForStore { get; set; }
        public object? NotifyCustomerAboutProductReviewReply_OverrideForStore { get; set; }
        public object? NotifyStoreOwnerAboutNewProductReviews_OverrideForStore { get; set; }
        public object? NumberOfBestsellersOnHomepage_OverrideForStore { get; set; }
        public object? NumberOfProductTags_OverrideForStore { get; set; }
        public object? PageShareCode_OverrideForStore { get; set; }
        public object? ProductReviewPossibleOnlyAfterPurchasing_OverrideForStore { get; set; }
        public object? ProductReviewsMustBeApproved_OverrideForStore { get; set; }
        public object? OneReviewPerProductFromCustomer_OverrideForStore { get; set; }
        public object? ProductReviewsPageSizeOnAccountPage_OverrideForStore { get; set; }
        public object? ProductReviewsSortByCreatedDateAscending_OverrideForStore { get; set; }
        public object? ProductsAlsoPurchasedEnabled_OverrideForStore { get; set; }
        public object? ProductsAlsoPurchasedNumber_OverrideForStore { get; set; }
        public object? ProductsByTagAllowCustomersToSelectPageSize_OverrideForStore { get; set; }
        public object? ProductsByTagPageSizeOptions_OverrideForStore { get; set; }
        public object? ProductsByTagPageSize_OverrideForStore { get; set; }
        public object? ProductSearchAutoCompleteEnabled_OverrideForStore { get; set; }
        public object? ProductSearchEnabled_OverrideForStore { get; set; }
        public object? ProductSearchAutoCompleteNumberOfProducts_OverrideForStore { get; set; }
        public object? ProductSearchTermMinimumLength_OverrideForStore { get; set; }
        public object? RecentlyViewedProductsEnabled_OverrideForStore { get; set; }
        public object? RecentlyViewedProductsNumber_OverrideForStore { get; set; }
        public object? RemoveRequiredProducts_OverrideForStore { get; set; }
        public object? SearchPageAllowCustomersToSelectPageSize_OverrideForStore { get; set; }
        public object? SearchPagePageSizeOptions_OverrideForStore { get; set; }
        public object? SearchPageProductsPerPage_OverrideForStore { get; set; }
        public object? ShowBestsellersOnHomepage_OverrideForStore { get; set; }
        public object? ShowCategoryProductNumberIncludingSubcategories_OverrideForStore { get; set; }
        public object? ShowCategoryProductNumber_OverrideForStore { get; set; }
        public object? ShowFreeShippingNotification_OverrideForStore { get; set; }
        public object? ShowShortDescriptionOnCatalogPages_OverrideForStore { get; set; }
        public object? ShowGtin_OverrideForStore { get; set; }
        public object? ShowLinkToAllResultInSearchAutoComplete_OverrideForStore { get; set; }
        public object? ShowManufacturerPartNumber_OverrideForStore { get; set; }
        public object? ShowProductImagesInSearchAutoComplete_OverrideForStore { get; set; }
        public object? ShowProductReviewsOnAccountPage_OverrideForStore { get; set; }
        public object? ShowProductReviewsPerStore_OverrideForStore { get; set; }
        public object? ShowProductsFromSubcategories_OverrideForStore { get; set; }
        public object? ShowShareButton_OverrideForStore { get; set; }
        public object? ShowSkuOnCatalogPages_OverrideForStore { get; set; }
        public object? ShowSkuOnProductDetailsPage_OverrideForStore { get; set; }
        public object? DisplayDatePreOrderAvailability_OverrideForStore { get; set; }
        public object? UseAjaxCatalogProductsLoading_OverrideForStore { get; set; }
        public object? SearchPagePriceRangeFiltering_OverrideForStore { get; set; }
        public object? SearchPagePriceFrom_OverrideForStore { get; set; }
        public object? SearchPagePriceTo_OverrideForStore { get; set; }
        public object? SearchPageManuallyPriceRange_OverrideForStore { get; set; }
        public object? ProductsByTagPriceRangeFiltering_OverrideForStore { get; set; }
        public object? ProductsByTagPriceFrom_OverrideForStore { get; set; }
        public object? ProductsByTagPriceTo_OverrideForStore { get; set; }
        public object? ProductsByTagManuallyPriceRange_OverrideForStore { get; set; }
        public object? EnableManufacturerFiltering_OverrideForStore { get; set; }
        public object? EnablePriceRangeFiltering_OverrideForStore { get; set; }
        public object? EnableSpecificationAttributeFiltering_OverrideForStore { get; set; }
        public object? DisplayFromPrices_OverrideForStore { get; set; }
        public object? AttributeValueOutOfStockDisplayTypes { get; set; }
        public object? AttributeValueOutOfStockDisplayType_OverrideForStore { get; set; }
        public object? SortOptionSearchModel { get; set; }
        public object? ReviewTypeSearchModel { get; set; }
        public object? PrimaryStoreCurrencyCode { get; set; }
        public object? AllowCustomersToSearchWithManufacturerName_OverrideForStore { get; set; }
        public object? AllowCustomersToSearchWithCategoryName_OverrideForStore { get; set; }
        public object? DisplayAllPicturesOnCatalogPages_OverrideForStore { get; set; }
        public object? ProductUrlStructureTypeId_OverrideForStore { get; set; }
        public object? ProductUrlStructureTypes { get; set; }
    }
}