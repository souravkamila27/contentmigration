using System;
using Corporate.Web.Cms.ContentTypes.Pages;
using Corporate.Eipa.Models;

namespace Corporate.Web.Cms.Schedulers.News
{
    public interface INewsPageMapper
    {
        void Map(NewsItemPage newsItemPage, NewsItem item, DateTime publishDate);

        NewsItem Map(NewsItemPage newsItemPage);
    }
}