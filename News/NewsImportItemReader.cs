using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace Corporate.Web.Cms.Schedulers.NewsImport
{
    [Obsolete]
    public class NewsImportItemReader
	{
		//URL used to access the item detail feed
		private string feedUrl = string.Empty;

		//Detail item to populate
		private NewsImportItem item = new NewsImportItem();

		/// <summary>
		/// Creates a new instance based on the specified feed URI and item ID
		/// </summary>
		/// <param name="companyInfoFeedItem"></param>
		public NewsImportItemReader(string feedUrl)
		{
			string detailUrl = feedUrl;

			//Verify that the feedUrl string isn't empty
			if (string.IsNullOrEmpty(detailUrl))
				throw new Exception("The detail url for the pressrelease is empty");
			
			XDocument doc = doc = XDocument.Load(detailUrl);

			var newsItem = doc.Root;
			item.Language = (string)newsItem.Element("translation").Element("language-code");
			
			XElement xMedia = newsItem.Element("media");
			if (xMedia != null)
			{
				foreach (XElement xMediaItem in xMedia.Elements("item"))
				{
					var mediaItem = new NewsImportItem.Media();
					mediaItem.Label = (string)xMediaItem.Element("label");
					mediaItem.Html = (string)xMediaItem.Element("html");
					mediaItem.Script = (string)xMediaItem.Element("script");
					item.MediaItems.Add(mediaItem);
				}
			}

			item.LinkTitle = (string)newsItem.Element("link-title");
			item.Title = (string)newsItem.Element("title");
			item.Introduction = (string)newsItem.Element("introduction");
			item.Text = (string)newsItem.Element("text");

			string publishDate = (string)newsItem.Element("publish-date");
			item.PublishDate = GetValidDate(publishDate);
			string closingDate = (string)newsItem.Element("closing-date");
			item.ClosingDate = GetValidDate(closingDate);

			item.TagList = (string)newsItem.Element("taglist");

			/* Image */
			//XElement xImage = newsItem.Element("images");
			//if (xImage != null)
			//{
			//	var image = item.ItemImage;
			//	image.Alt = (string)xImage.Attribute("alt");
			//	image.DataDesktop = (string)xImage.Attribute("data-desktop");
			//	image.DataSmartphoneLandscape = (string)xImage.Attribute("data-smartphonelandscape");
			//	image.DataSmartphonePortrait = (string)xImage.Attribute("data-smartphoneportrait");
			//	image.DataTabletLandscape = (string)xImage.Attribute("data-tabletlandscape");
			//	image.DataTabletPortrait = (string)xImage.Attribute("data-tabletportrait");
			//}

			// Categories
			var xCategories = newsItem.Element("categories");
			if (xCategories != null)
			{
				foreach (XElement xCategory in xCategories.Elements("cms"))
				{
					var category = (string)xCategory.Attribute("ref");
                    item.Categories.Add(category);
				}
			}

			//item.DirectLink = (string)newsItem.Element("direct-link");

			/* References */
			XElement xReferences = newsItem.Element("references");
			if (xReferences != null)
			{
				foreach (XElement xReference in xReferences.Elements("reference"))
				{
					item.References += xReference.Element("label").Value;
					var xRef = xReference.Elements().Count() == 2 ? xReference.Elements().Last() : null;
					item.References += xRef != null ? ": " + xRef.Attribute("ref").Value + "\r\n" : string.Empty;
				}
			}

			//item.Targeting = (string)newsItem.Element("targeting");
			//item.ShareImage = (string)newsItem.Element("share-options").Element("image").Attribute("ref");
			item.ShareDescription = (string)newsItem.Element("share-options").Element("description");
			XElement metaElement = newsItem.Element("meta");
			item.MetaTitle = (string)metaElement.Element("title");
			item.MetaDescription = (string)metaElement.Element("description");
			item.MetaKeywords = (string)metaElement.Element("keywords");
			item.MetaCreatedAt = (double)metaElement.Element("created-at");
			item.MetaModifiedAt = (double)metaElement.Element("modified-at");
		}
		
		private DateTime GetValidDate(string date)
		{
			DateTime retval = new DateTime();
			if (!string.IsNullOrEmpty(date))
			{
				retval = DateTime.Parse(date, CultureInfo.InvariantCulture);
			}
			
			return retval;
		}

		/// <summary>
		/// Gets the feed detail item parsed by the reader
		/// </summary>
		public NewsImportItem Item
		{
			get { return item; }
		}
	}
}