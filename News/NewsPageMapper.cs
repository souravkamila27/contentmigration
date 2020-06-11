using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Corporate.Web.Cms.ContentTypes.Media;
using Corporate.Web.Cms.ContentTypes.Pages;
using Corporate.Web.Models.ResponsiveImage;
using Corporate.Web.ResponsiveImages;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Framework.Blobs;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Corporate.Eipa.Contracts;
using Corporate.Eipa.Models;
using HtmlAgilityPack;

namespace Corporate.Web.Cms.Schedulers.News
{
    public class NewsPageMapper : INewsPageMapper
    {
        private IEipaReferenceMapper EipaReferenceMapper { get; set; }
        private IContentRepository ContentRepository { get; set; }
        private string CategoriesSeparator = ",";
        private UrlResolver UrlResolver { get; set; }

        public NewsPageMapper(IEipaReferenceMapper eipaReferenceMapper, 
            IContentRepository contentRepository, UrlResolver urlResolver)
        {
            EipaReferenceMapper = eipaReferenceMapper;
            ContentRepository = contentRepository;
            UrlResolver = urlResolver;
        }

        public void Map(NewsItemPage newsItemPage, NewsItem item, DateTime publishDate)
        {  


            newsItemPage.PageName = item.Title;
            newsItemPage.ListHeading = item.LinkTitle;
            newsItemPage.Headline = item.Title;
            newsItemPage.Preamble = item.Introduction;
            
            if (item.MetaData.CreatedDateMilliseconds > 0)
            {
                newsItemPage.Created = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                    .AddMilliseconds(item.MetaData.CreatedDateMilliseconds);
            }

            if (item.MetaData.ModifiedDateMilliseconds > 0)
            {
                newsItemPage.Saved = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                    .AddMilliseconds(item.MetaData.ModifiedDateMilliseconds);
            }

            /* DATES */
            newsItemPage.StartPublish = publishDate;

            if (item.ClosingDate != DateTime.MinValue)
            {
                newsItemPage.StopPublish = item.ClosingDate;
            }
            /* END DATES */
            
            // Need to save before setting MainBody (ContentFolder needs to be created)
            ContentRepository.Save(newsItemPage, SaveAction.Save, AccessLevel.NoAccess);

            // Prepend domain to relative urls
            var mainBody = item.Text.Replace("href=\"/", "href=\"//www.ericsson.com/");
            // Download, upload, save media to page, and replace markup in MainBody
            newsItemPage.MainBody = new XhtmlString(GetMainBodyWithMediaAndLinks(mainBody, newsItemPage));

            /* Media items */
            foreach (var media in item.MediaItems)
            {
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(media.Html);

                /* Images */
                var imageNodes = htmlDoc.DocumentNode.Descendants().Where(n => n.Name.StartsWith("macro:image"));
                if (imageNodes.Any())
                {
                    newsItemPage.Image3 = ImportExternalImage(imageNodes.First(), newsItemPage);
                }

                /* Videos */
                var videoNodes = htmlDoc.DocumentNode.Descendants()
                    .Where(n => n.Name.StartsWith("macro:video") || n.Name.StartsWith("macro:kaltura-video"));

                if (videoNodes.Any())
                {
                    var videoId = "not found";
                    if (videoNodes.First().Attributes["entry-id"] != null)
                    {
                        videoId = videoNodes.First().Attributes["entry-id"].Value;
                    }
                    else if (videoNodes.First().Attributes["video-path"] != null)
                    {
                        videoId = videoNodes.First().Attributes["video-path"].Value;
                    }

                    var missingText = "<h3 style=\"color: red;\">MISSING VIDEO. Id/path: " + videoId + "</h3>";

                    var currentMainBody = newsItemPage.MainBody != null ? newsItemPage.MainBody.ToHtmlString() : string.Empty;
                    newsItemPage.MainBody = new XhtmlString(missingText + currentMainBody);
                }
            }

            newsItemPage.ImportedTagList = item.TagListString;

            newsItemPage.ImportedCategories = item.Categories != null && item.Categories.Count > 0
                ? string.Join(CategoriesSeparator, item.Categories.Select(s => s.Reference))
                : string.Empty;

            //newsItemPage.DirectLink = item.DirectLink;
            newsItemPage.ImportedReferences = EipaReferenceMapper.ReferencesToString(item.References);
            //newsItemPage.Targeting = item.Targeting;

            /* Meta data */
			if (item.MetaData != null)
			{ 
				newsItemPage.MetaTitle = item.MetaData.Title;
				newsItemPage.MetaDescription = item.MetaData.Description;
				newsItemPage.MetaKeywords = item.MetaData.KeyWordsString;
			}
			if (item.ShareOptions != null)
			{ 
				newsItemPage.ShareDescription = item.ShareOptions.Description;
			}
			//newsPage.ShareImage = item.ShareOptions.Image.Reference; // images urls from XML does not resolve

			// Publish page
			ContentRepository.Save(newsItemPage, SaveAction.Publish, AccessLevel.NoAccess);
        }

        public NewsItem Map(NewsItemPage newsItemPage)
        {
            var newsItem = new NewsItem
            {
                Title = newsItemPage.Headline,
                Introduction = newsItemPage.Preamble,
                LinkTitle = newsItemPage.ListHeading,
                PublishDate = newsItemPage.StartPublish,
                Translation = new Translation
                {
                    LanguageId = newsItemPage.LanguageID
                }
            };
            
            /* DATES */

            if (newsItemPage.StopPublish != DateTime.MaxValue)
            {
                newsItem.ClosingDate = newsItemPage.StopPublish;
            }

            if (newsItemPage.MainBody != null)
            {
                newsItem.Text = newsItemPage.MainBody.ToString();//.Replace("href=\"/", "href=\"//www.ericsson.com/");
            }

            string imageUrl;
            if (TryGetImageUrl(newsItemPage.Image1, "image-wide-100", out imageUrl))
            {
                newsItem.Images = new Images
                {
                    AlternativeText = newsItemPage.AltText1,
                    DesktopImageUrl = imageUrl
                };
            }

            var unixStartTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var createdTimeDiff = newsItemPage.Created - unixStartTime;
            var savedTimeDiff = newsItemPage.Saved - unixStartTime;

            /* Meta data */
            newsItem.MetaData = new Meta
            {
                Title = newsItemPage.MetaTitle,
                Description = newsItemPage.MetaDescription,
                KeyWordsString = newsItemPage.MetaKeywords,
                CreatedDateMilliseconds = createdTimeDiff.TotalMilliseconds,
                ModifiedDateMilliseconds = savedTimeDiff.TotalMilliseconds
            };

            newsItem.ShareOptions = new ShareOptions
            {
                Description = newsItemPage.ShareDescription
            };

            string shareImageUrl;
            if (TryGetImageUrl(newsItemPage.ShareImage, "image-normal-100", out shareImageUrl))
            {
                newsItem.ShareOptions.Image = new Link
                {
                    Reference = shareImageUrl
                };
            }

            newsItem.TagListString = newsItemPage.ImportedTagList;

            /* Categories */
            if (!string.IsNullOrWhiteSpace(newsItemPage.ImportedCategories))
            {
                var categoriesList = newsItemPage.ImportedCategories
                    .Split(new[] {CategoriesSeparator}, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                if (categoriesList.Count > 0)
                {
                    newsItem.Categories = new List<Link>();
                    foreach (var category in categoriesList)
                    {
                        newsItem.Categories.Add(new Link
                        {
                            Reference = category
                        });
                    }
                }
            }

            newsItem.References = EipaReferenceMapper.ReferencesFromString(newsItemPage.ImportedReferences);

            return newsItem;
        }

        #region Helpers

        private string GetMainBodyWithMediaAndLinks(string text, NewsItemPage newsPage)
        {
            // Return emtpy string if MainBody is empty
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(text);

            /* LINKS */
            // Get all <macro:link> in the text
            var linkNodes = htmlDoc.DocumentNode.Descendants().Where(n => n.Name.StartsWith("macro:link"));
            foreach (HtmlNode node in linkNodes.ToList())
            {
                // Get source url - add ericsson.com domain if relative url
                var url = node.Attributes["ref"].Value;
                if (url.StartsWith("/"))
                {
                    url = "//www.ericsson.com" + url;
                }

                var link = HtmlNode.CreateNode(string.Format("<a href=\"{0}\">{1}</a>", url, node.Attributes["label"].Value));

                // Replace <macro:link> with proper link tag
                node.ParentNode.ReplaceChild(link, node);
            }

            /* IMAGES */
            var urlResolver = ServiceLocator.Current.GetInstance<UrlResolver>();
            var contentRepo = ServiceLocator.Current.GetInstance<IContentRepository>();

            var imageNodes = htmlDoc.DocumentNode.Descendants().Where(n => n.Name.StartsWith("macro:image"));
            foreach (HtmlNode node in imageNodes.ToList())
            {
                var fileId = ImportExternalImage(node, newsPage);

                var file = contentRepo.Get<ImageFile>(fileId);
                var fileUrl = urlResolver.GetUrl(fileId);
                var image = HtmlNode.CreateNode(string.Format("<img src=\"{0}\" alt=\"{1}\" class=\"imported-image\">", fileUrl, file.AltText));

                // Replace <macro:image> with proper img tag with reference to EPi file
                node.ParentNode.ReplaceChild(image, node);
            }

            /* Videos */
            var videoNodes = htmlDoc.DocumentNode.Descendants().Where(n => n.Name.StartsWith("macro:video") || n.Name.StartsWith("macro:kaltura-video"));
            foreach (HtmlNode node in videoNodes.ToList())
            {
                var videoId = "not found";
                if (videoNodes.First().Attributes["entry-id"] != null)
                {
                    videoId = videoNodes.First().Attributes["entry-id"].Value;
                }
                else if (videoNodes.First().Attributes["video-path"] != null)
                {
                    videoId = videoNodes.First().Attributes["video-path"].Value;
                }

                var missingVideoNode = HtmlNode.CreateNode("<h3 style=\"color: red;\">MISSING VIDEO. Id/path: " + videoId + "</h3>");

                node.ParentNode.ReplaceChild(missingVideoNode, node);
            }

            return htmlDoc.DocumentNode.OuterHtml;
        }

        private ContentReference ImportExternalImage(HtmlNode imageNode, NewsItemPage newsPage)
        {
            var blobFactory = ServiceLocator.Current.GetInstance<BlobFactory>();
            var contentAssetHelper = ServiceLocator.Current.GetInstance<ContentAssetHelper>();
           
            // get an existing content asset folder or create a new one
            var assetsFolder = contentAssetHelper.GetOrCreateAssetFolder(newsPage.ContentLink);

            // Get source url - add ericsson.com domain if relative url
            var nodeUrl = imageNode.Attributes["src"].Value;
            if (nodeUrl.StartsWith("/"))
            {
                nodeUrl = "http://www.ericsson.com" + nodeUrl;
            }

            // Image name
            var imageName = nodeUrl.Substring(nodeUrl.LastIndexOf('/') + 1);

            // Get a new empty file data
            var file = ContentRepository.GetDefault<ImageFile>(assetsFolder.ContentLink);
            file.Name = imageName;

            // Create a blob in the binary container
            var blob = blobFactory.CreateBlob(file.BinaryDataContainer, Path.GetExtension(imageName));

            // Create a webrequest to read the file and write to EPi blob
            WebRequest req = WebRequest.Create(nodeUrl);
            using (WebResponse resp = req.GetResponse())
            {
                using (Stream stream = resp.GetResponseStream())
                {
                    using (var s = blob.OpenWrite())
                    {
                        stream.CopyTo(s);
                        s.Flush();
                    }
                }
            }

            // Set file data
            file.BinaryData = blob;
            file.AltText = imageNode.Attributes["alt"]?.Value;

            blob = null;

            // Save file to db
            var fileId = ContentRepository.Save(file, SaveAction.Publish);

            return fileId;
        }

        private bool TryGetImageUrl(ContentReference imageReference, string imageType, out string imageUrl)
        {
            if (ContentReference.IsNullOrEmpty(imageReference))
            {
                imageUrl = string.Empty;
                return false;
            }

            var imagePath = UrlResolver.GetUrl(imageReference);
            if (imagePath.Contains("epieditmode"))
            {
                imagePath = UrlResolver.GetUrl(imageReference, null, new VirtualPathArguments
                {
                    ContextMode = ContextMode.Default
                });
            }

            var imageData = new ResponsiveImageData
            {
                Path = imagePath,
                Image = imageReference,
                ImageType = imageType
            };

            var responsiveImage = new ResponsiveImage(imageData);
            imageUrl = responsiveImage.GetImageURL();
            return !string.IsNullOrWhiteSpace(imageUrl);
        }

        #endregion
    }
}