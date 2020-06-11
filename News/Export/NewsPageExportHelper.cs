using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Corporate.Web.Cms.ContentTypes.Pages;
using EPiServer.Core;
using EPiServer.Find;
using EPiServer.Find.Cms;
using EPiServer.Find.Framework;
using Corporate.Eipa.Contracts;
using System.Linq;
using System.Threading.Tasks;
using Corporate.Core.Contracts.Helpers;
using Corporate.Eipa.Models;

namespace Corporate.Web.Cms.Schedulers.News.Export
{
    public class NewsPageExportHelper : INewsPageExportHelper
    {
        private INewsPageMapper NewsPageMapper { get; set; }
        private IEipaFileHelper EipaFileHelper { get; set; }
        private ILog Logger { get; set; }

        public NewsPageExportHelper(INewsPageMapper newsPageMapper, IEipaFileHelper eipaFileHelper, ILog logger)
        {
            NewsPageMapper = newsPageMapper;
            EipaFileHelper = eipaFileHelper;
            Logger = logger;
        }

        public void Export(ContentReference sourceDirectory, string destinationPath, CancellationTokenSource tokenSource, 
            Action<string> changeStatus, ConcurrentQueue<string> exportedPages, ConcurrentQueue<string> errors)
        {
            if (tokenSource.IsCancellationRequested)
            {
                return;
            }
            
            var newsPages = GetNewsPage(sourceDirectory);
            if (newsPages == null || newsPages.Count == 0)
            {
                return;
            }

            Parallel.ForEach(newsPages, i => Export(i, destinationPath,
                tokenSource.Token, exportedPages, errors, changeStatus));
        }

        #region Helper

        private List<NewsItemPage> GetNewsPage(ContentReference sourceDirectory)
        {
            var result = new List<NewsItemPage>();

            var skip = 0;
            var pageLength = 1000;
            IContentResult<NewsItemPage> searchResult;

            do
            {
                searchResult = SearchClient.Instance.Search<NewsItemPage>()
                    .Filter(x => x.Ancestors().Match(sourceDirectory.ID.ToString()))
                    .Filter(x => x.IsPendingPublish.Match(false))
                    .Skip(skip)
                    .Take(pageLength)
                    .GetContentResult();

                skip += pageLength;

                result.AddRange(searchResult);

            } while (searchResult != null && searchResult.Count() >= pageLength);

            return result;
        }

        private void Export(NewsItemPage newsItemPage, string destinationPath, CancellationToken cancellationToken, 
            ConcurrentQueue<string> exportedPages, ConcurrentQueue<string> errors, Action<string> changeStatus)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            NewsItem eipaNewsFileData;
            try
            {
                eipaNewsFileData = NewsPageMapper.Map(newsItemPage);
            }
            catch (Exception e)
            {
                var message = $"Page with id '{newsItemPage.PageId}' can't be converted to Eipa file. Exception: {e}";
                Logger.LogError(e, message);
                errors.Enqueue(message);
                return;
            }

            if (EipaFileHelper.Put(GetFilePath(newsItemPage, destinationPath), eipaNewsFileData))
            {
                exportedPages.Enqueue($"{newsItemPage.PageId}");
            }
            else
            {
                errors.Enqueue($"Page with id '{newsItemPage.PageId}' wasn't exported correctly");
            }

            changeStatus.Invoke($"Exported '{exportedPages.Count}' news pages. Errors: {errors.Count}");
        }
        
        private string GetFilePath(NewsItemPage newsItemPage, string destinationPath)
        {
            var date = $"{newsItemPage.StartPublish.ToString("yy")}{newsItemPage.StartPublish.ToString("MM")}{newsItemPage.StartPublish.ToString("dd")}";
            return $"{destinationPath}/{newsItemPage.PageId}-{date}-{newsItemPage.URLSegment}.{newsItemPage.LanguageID}.eipa";
        }

        #endregion
    }
}