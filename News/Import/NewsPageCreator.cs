using System;
using System.Globalization;
using System.Text;
using Corporate.Web.Cms.ContentTypes.Pages;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAccess;
using EPiServer.Security;
using Corporate.Eipa.Contracts;
using Corporate.Eipa.Models;

namespace Corporate.Web.Cms.Schedulers.News.Import
{
    public class NewsPageCreator : INewsPageCreator
    {
        private int _yearContainerPageTypeId;
        private int _newsItemPageTypeId;

        #region Properties

        private IContentRepository ContentRepository { get; set; }

        private PageTypeRepository PageTypeRepository { get; set; }

        private IEipaFileHelper EipaFileHelper { get; set; }

        private INewsPageMapper NewsPageMapper { get; set; }

        /// <summary>
        /// The page type id for the year container pages (if =0 all pages are placed directly under the container)
        /// </summary>
        private int YearContainerPageTypeId
        {
            get
            {
                if (_yearContainerPageTypeId != 0)
                {
                    return _yearContainerPageTypeId;
                }

                var contentType = PageTypeRepository.Load<NewsPlaceholderPage>();
                _yearContainerPageTypeId = contentType?.ID ?? 0;

                return _yearContainerPageTypeId;
            }
        }

        private int NewsItemPageTypeId
        {
            get
            {
                if (_newsItemPageTypeId != 0)
                {
                    return _newsItemPageTypeId;
                }

                var contentType = PageTypeRepository.Load<NewsItemPage>();
                _newsItemPageTypeId = contentType?.ID ?? 0;

                return _newsItemPageTypeId;
            }
        }
        
        #endregion

        public NewsPageCreator(PageTypeRepository pageTypeRepository, IContentRepository contentRepository, 
            IEipaFileHelper eipaFileHelper, INewsPageMapper newsPageMapper)
        {
            PageTypeRepository = pageTypeRepository;
            ContentRepository = contentRepository;
            EipaFileHelper = eipaFileHelper;
            NewsPageMapper = newsPageMapper;
        }
        
        public string ExecutePageCreation(string feedUrl, ContentReference parentContainer)
        {
            var returningMessage = new StringBuilder();
            // Make sure that there is a container given
            if (ContentReference.IsNullOrEmpty(parentContainer))
            {
                return $"Property {parentContainer} not set \n";
            }

            // Set the real name of the Container in the log file
            //returningMessage.AppendFormat("{0} \n", _contentRepository.Get<PageData>(Container).PageName);

            try
            {
                // Read in the XML into a NewsImportItem
                //var detailItem = new NewsImportItemReader(FeedUrl).Item;
                var newsData = EipaFileHelper.Get<NewsItem>(feedUrl);

                // Create page from the news item
                //CreateNewsPage(detailItem);
                CreateNewsPage(newsData, parentContainer, returningMessage);

                if (returningMessage.Capacity >= 0.8 * returningMessage.MaxCapacity)
                {
                    returningMessage = new StringBuilder();
                }
            }
            catch (Exception fe)
            {
                return returningMessage.AppendFormat("File: {0} \n Error message: {1} \n Stacktrace: {2} \n\n", feedUrl, fe.Message, fe.StackTrace).ToString();
            }

            return returningMessage.ToString();
        }

        #region Helpers
        
        private void CreateNewsPage(NewsItem item, ContentReference parentContainer, StringBuilder returningMessage)
        {
            try
            {
                var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                var publishDate = item.PublishDate != DateTime.MinValue
                    ? item.PublishDate
                    : dtDateTime.AddMilliseconds(item.MetaData.CreatedDateMilliseconds);
                
                var container = GetChildFromContainerAndName(parentContainer, publishDate.Year, item.Translation.LanguageId);
                container = GetChildFromContainerAndName(container, publishDate.Month, item.Translation.LanguageId);
                // Create MediaPage
                var newsPage = ContentRepository.GetDefault<NewsItemPage>(container, NewsItemPageTypeId, new CultureInfo(item.Translation.LanguageId));
                 
                if (newsPage == null)
                {
                    returningMessage.AppendFormat("Error: " + item.Title + " - _contentRepository.GetDefault<NewsItemPage> FAILED <br />");
                    return;
                }

                NewsPageMapper.Map(newsPage, item, publishDate);

                // Add a message about how many that has been imported
                //returningMessage.AppendFormat(
                //	"News imported: {0} with PageID: {1}\n",
                //	newsPage.PageName,
                //	newsPage.PageLink);
            }
            catch (Exception ex)
            {
                returningMessage.AppendFormat("Error: " + item.Title + " - " + ex.Message + "<br />");
            }
        }

        /// <summary>
        /// Method that returns a ContainerPage under a parent container based on a name
        /// if it doesn't exist, it will create it based on the name given
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="childName"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        protected ContentReference GetChildFromContainerAndName(ContentReference parent, int childName, string language)
        {
            //1.    Loop through all child pages under the container to see if anyone of the children is named as the childName - if so return the PageLink
            //2.    Otherwise create a new container page under the parent and return the pagelink

            foreach (PageData child in ContentRepository.GetChildren<PageData>(parent, new LanguageSelector(language)))
            {
                if (string.Compare(child.PageName, childName.ToString(), true) == 0)
                {
                    return child.PageLink;
                }
            }

            return CreateChildUnderContainer(parent, childName, language);
        }

        /// <summary>
        /// This method creates a container page under a parent, based on a given name
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="childName">Name of the child.</param>
        /// <param name="language"></param>
        /// <returns></returns>
        protected ContentReference CreateChildUnderContainer(ContentReference parent, int childName, string language)
        {
            //1. Create a new MediaContainer page with the name of the childName
            //2. Return the newly created PageReference

            try
            {
                if (YearContainerPageTypeId == 0)
                    return parent;
                ContentReference otherLanguage = null;
                foreach (PageData child in ContentRepository.GetChildren<PageData>(parent, LanguageSelector.MasterLanguage()))
                {
                    if (string.Compare(child.PageName, childName.ToString(), true) == 0)
                    {
                        otherLanguage = child.PageLink;
                        break;
                    }
                }

                if (otherLanguage == null)
                {
                    // first add the first language of the year container, then loop through other language of the start page.
                    //ContentReference yearContainer;
                    CultureInfo mainLanguage = new CultureInfo("en");
                    PageData yearPage = ContentRepository.GetDefault<PageData>(
                        parent,
                        YearContainerPageTypeId,
                        mainLanguage);

                    yearPage.PageName = childName.ToString();
                    otherLanguage = ContentRepository.Save(yearPage, SaveAction.Publish, AccessLevel.NoAccess);
                }
                if (language != "en")
                {
                    CultureInfo langSel = new CultureInfo(language);
                    var yearPage = ContentRepository.CreateLanguageBranch<PageData>(otherLanguage, langSel);
                    yearPage.PageName = childName.ToString();

                    ContentRepository.Save(yearPage, SaveAction.Publish, AccessLevel.NoAccess);
                }
                return otherLanguage;
            }
            catch
            {
                return ContentReference.EmptyReference;
            }
        }


        #endregion

        #region Obsolete


        //[Obsolete]
        //private void CreateNewsPage(NewsImportItem item)
        //{
        //    try
        //    {
        //        var contentType = PageTypeRepository.Load<NewsItemPage>();
        //        if (contentType == null)
        //        {
        //            return;
        //        }
        //        DateTime publishDate;
        //        DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        //        if (item.PublishDate != DateTime.MinValue)
        //        {
        //            publishDate = item.PublishDate;
        //        }
        //        else
        //        {
        //            publishDate = dtDateTime.AddMilliseconds(item.MetaCreatedAt);
        //        }

        //        var container = GetChildFromContainerAndName(Container, publishDate.Year, item.Language);
        //        container = GetChildFromContainerAndName(container, publishDate.Month, item.Language);
        //        // Create MediaPage
        //        var newsPage = ContentRepository.GetDefault<NewsItemPage>(container, contentType.ID, new CultureInfo(item.Language));

        //        if (newsPage == null)
        //        {
        //            ReturningMessage.AppendFormat("Error: " + item.Title + " - _contentRepository.GetDefault<NewsItemPage> FAILED <br />");
        //            return;
        //        }

        //        newsPage.PageName = item.Title;
        //        newsPage.ListHeading = item.LinkTitle;
        //        newsPage.Headline = item.Title;
        //        newsPage.Preamble = item.Introduction;

        //        /* DATES */
        //        newsPage.StartPublish = publishDate;

        //        if (item.ClosingDate != DateTime.MinValue)
        //        {
        //            newsPage.StopPublish = item.ClosingDate;
        //        }
        //        /* END DATES */

        //        // Need to save before setting MainBody (ContentFolder needs to be created)
        //        ContentRepository.Save(newsPage, SaveAction.Save, AccessLevel.NoAccess);

        //        // Prepend domain to relative urls
        //        var mainBody = item.Text.Replace("href=\"/", "href=\"//www.ericsson.com/");
        //        // Download, upload, save media to page, and replace markup in MainBody
        //        newsPage.MainBody = new XhtmlString(GetMainBodyWithMediaAndLinks(mainBody, newsPage));

        //        /* Media items */
        //        foreach (NewsImportItem.Media media in item.MediaItems)
        //        {
        //            HtmlDocument htmlDoc = new HtmlDocument();
        //            htmlDoc.LoadHtml(media.Html);

        //            /* Images */
        //            var imageNodes = htmlDoc.DocumentNode.Descendants().Where(n => n.Name.StartsWith("macro:image"));
        //            if (imageNodes.Any())
        //            {
        //                newsPage.Image3 = ImportExternalImage(imageNodes.First(), newsPage);
        //            }

        //            /* Videos */
        //            var videoNodes = htmlDoc.DocumentNode.Descendants().Where(n => n.Name.StartsWith("macro:video") || n.Name.StartsWith("macro:kaltura-video"));
        //            if (videoNodes.Any())
        //            {
        //                var videoId = "not found";
        //                if (videoNodes.First().Attributes["entry-id"] != null)
        //                {
        //                    videoId = videoNodes.First().Attributes["entry-id"].Value;
        //                }
        //                else if (videoNodes.First().Attributes["video-path"] != null)
        //                {
        //                    videoId = videoNodes.First().Attributes["video-path"].Value;
        //                }

        //                var missingText = "<h3 style=\"color: red;\">MISSING VIDEO. Id/path: " + videoId + "</h3>";

        //                var currentMainBody = newsPage.MainBody != null ? newsPage.MainBody.ToHtmlString() : string.Empty;
        //                newsPage.MainBody = new XhtmlString(missingText + currentMainBody);
        //            }
        //        }

        //        newsPage.ImportedTagList = item.TagList;

        //        string importedCategories = string.Empty;
        //        foreach (string category in item.Categories)
        //        {
        //            importedCategories += category + ",";
        //        }
        //        newsPage.ImportedCategories = importedCategories;

        //        //newsPage.DirectLink = item.DirectLink;
        //        newsPage.ImportedReferences = item.References;
        //        //newsPage.Targeting = item.Targeting;

        //        /* Meta data */
        //        newsPage.MetaTitle = item.MetaTitle;
        //        newsPage.MetaDescription = item.MetaDescription;
        //        newsPage.MetaKeywords = item.MetaKeywords;
        //        newsPage.ShareDescription = item.ShareDescription;
        //        //newsPage.ShareImage = item.ShareImage; // images urls from XML does not resolve

        //        // Publish page
        //        ContentRepository.Save(newsPage, SaveAction.Publish, AccessLevel.NoAccess);

        //        // Add a message about how many that has been imported
        //        //returningMessage.AppendFormat(
        //        //	"News imported: {0} with PageID: {1}\n",
        //        //	newsPage.PageName,
        //        //	newsPage.PageLink);
        //    }
        //    catch (Exception ex)
        //    {
        //        ReturningMessage.AppendFormat("Error: " + item.Title + " - " + ex.Message + "<br />");
        //    }
        //}

        #endregion
    }
}