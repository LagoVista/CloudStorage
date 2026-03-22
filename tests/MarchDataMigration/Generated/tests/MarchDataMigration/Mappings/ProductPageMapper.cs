using MarchDataMigration.Generated.ProductPage;
using System;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class ProductPageMapper
    {
        public static TargetProductPageRow Map(SourceProductPageRow source)
        {
            return new TargetProductPageRow
            {
                Id = source.Id,
                OrganizationId = "04FCB8DE0DEB4CE289487C6DE7C5611E",
                Name = source.Name,
                Key = source.Key,
                Icon = source.Icon,
                PageTitle = source.PageTitle,
                ShortSummaryHTML = source.ShortSummaryHTML,
                ThumbnailImageResourceId = source.ThumbnailImageResourceId,
                ThumbnailImageResourceName = source.ThumbnailImageResourceName,
                ImageResourceId = source.ImageResourceId,
                ImageResourceName = source.ImageResourceName,
                HeroImageResourceId = source.HeroImageResourceId,
                HeroImageResourceName = source.HeroImageResourceName,
                HeroTitle = source.HeroTitle,
                HeroTagLine1 = source.HeroTagLine1,
                HeroTagLine2 = source.HeroTagLine2,
                TopLeftMenuId = source.TopLeftMenuId,
                TopLeftMenuName = source.TopLeftMenuName,
                TopRightMenuId = source.TopRightMenuId,
                TopRightMenuName = source.TopRightMenuName,
                BottomMenuId = source.BottomMenuId,
                BottomMenuName = source.BottomMenuName,
                ColorPaletteId = source.ColorPaletteId,
                ColorPaletteName = source.ColorPaletteName,
                ProductPageLayoutId = source.ProductPageLayoutId,
                ProductPageLayoutName = source.ProductPageLayoutName,
                CreatedById = source.CreatedById,
                LastUpdatedById = source.LastUpdatedById,
                CreationDate = source.CreationDate,
                LastUpdatedDate = source.LastUpdateDate,
                IsPublic = source.IsPublic,
                DescriptionHtml = source.DescriptionHtml,
                VideoUrl = source.VideoUrl,
            };
        }
    }
}
