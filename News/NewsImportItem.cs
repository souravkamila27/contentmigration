using System;
using System.Collections.Generic;

namespace Corporate.Web.Cms.Schedulers.NewsImport
{
    [Obsolete]
    public class NewsImportItem
	{
		private string
			language,
			linkTitle,
			title,
			introduction,
			text,
			tagList,
			directLink,
			references,
			targeting,
			shareImage,
			shareDescription,
			metaTitle,
			metaDescription,
			metaKeywords;

		private double metaCreatedAt, metaModifiedAt;
		private DateTime publishDate, closingDate;

		private Image itemImage = new Image();
		private List<Image> images = new List<Image>();

		private List<Media> mediaItems = new List<Media>();
		private List<string> categories = new List<string>();

		/// <summary>
		/// Gets or sets the language of the item
		/// </summary>
		public string Language
		{
			get	{ return language; }
			set	{ language = value; }
		}

		/// <summary>
		/// Gets or sets the link title of the item
		/// </summary>
		public string LinkTitle
		{
			get	{ return linkTitle;	}
			set	{ linkTitle = value; }
		}

		/// <summary>
		/// Gets or sets the link title of the item
		/// </summary>
		public string Title
		{
			get { return title;	}
			set	{ title = value; }
		}

		/// <summary>
		/// Gets or sets the introduction of the item
		/// </summary>
		public string Introduction
		{
			get	{ return introduction; }
			set	{ introduction = value; }
		}

		/// <summary>
		/// Gets or sets the title of the item
		/// </summary>
		public string Text
		{
			get	{ return text; }
			set	{ text = value;	}
		}

		/// <summary>
		/// Gets or sets the tag list of the item
		/// </summary>
		public string TagList
		{
			get	{ return tagList; }
			set	{ tagList = value; }
		}


		/// <summary>
		/// Gets or sets the direct link of the item
		/// </summary>
		public string DirectLink
		{
			get	{ return directLink; }
			set	{ directLink = value; }
		}

		/// <summary>
		/// Gets or sets the references of the item
		/// </summary>
		public string References
		{
			get { return references; }
			set { references = value; }
		}

		/// <summary>
		/// Gets or sets the targeting of the item
		/// </summary>
		public string Targeting
		{
			get	{ return targeting; }
			set	{ targeting = value; }
		}

		/// <summary>
		/// Gets or sets the share image of the item
		/// </summary>
		public string ShareImage
		{
			get	{ return shareImage; }
			set	{ shareImage = value; }
		}

		/// <summary>
		/// Gets or sets the share desc of the item
		/// </summary>
		public string ShareDescription
		{
			get	{ return shareDescription; }
			set	{ shareDescription = value; }
		}

		public string MetaTitle
		{
			get	{ return metaTitle; }
			set	{ metaTitle = value; }
		}

		public string MetaDescription
		{
			get { return metaDescription; }
			set	{ metaDescription = value; }
		}

		public string MetaKeywords
		{
			get	{ return metaKeywords; }
			set { metaKeywords = value; }
		}

		public double MetaCreatedAt
		{
			get { return metaCreatedAt; }
			set	{ metaCreatedAt = value; }
		}

		public double MetaModifiedAt
		{
			get	{ return metaModifiedAt; }
			set	{ metaModifiedAt = value; }
		}

		/// <summary>
		/// Gets or sets the publishing date of the item
		/// </summary>
		public DateTime PublishDate
		{
			get	{ return publishDate; }
			set	{ publishDate = value; }
		}

		/// <summary>
		/// Gets or sets the closing date of the item
		/// </summary>
		public DateTime ClosingDate
		{
			get	{ return closingDate; }
			set	{ closingDate = value; }
		}

		/// <summary>
		/// Gets or sets an image attached to the item
		/// </summary>
		public Image ItemImage
		{
			get { return itemImage; }
			set { itemImage = value; }
		}

		/// <summary>
		/// Gets or sets a list of categories for the item
		/// </summary>
		public List<Media> MediaItems
		{
			get { return mediaItems; }
			set { mediaItems = value; }
		}

		/// <summary>
		/// Gets or sets a list of categories for the item
		/// </summary>
		public List<string> Categories
		{
			get { return categories; }
			set { categories = value; }
		}

		/// <summary>
		/// Provides a string representation of the current item
		/// </summary>
		/// <returns>The title of the feed item</returns>
		public override string ToString()
		{
			return PublishDate.ToString().PadRight(20) + Title;
		}

		public struct Media
		{
			private string label,
				html,
				script;

			public string Label
			{
				get { return label; }
                set { label = value; }
			}

			public string Html
			{
				get { return html; }
				set { html = value; }
			}

			public string Script
			{
				get { return script; }
				set { script = value; }
			}
		}

		/// <summary>
		/// Represents an image attached to a detail item
		/// </summary>
		public struct Image
		{
			private string
				alt,
				dataDesktop,
                dataSmartphoneLandscape,
                dataSmartphonePortrait,
                dataTabletLandscape,
                dataTabletPortrait;

			public string Alt
			{
				get { return alt; }
				set { alt = value; }
			}

			public string DataDesktop
			{
				get { return dataDesktop; }
				set { dataDesktop = value; }
			}

			public string DataSmartphoneLandscape
			{
				get { return dataSmartphoneLandscape; }
				set { dataSmartphoneLandscape = value; }
			}

			public string DataSmartphonePortrait
			{
				get { return dataSmartphonePortrait; }
				set { dataSmartphonePortrait = value; }
			}

			public string DataTabletLandscape
			{
				get { return dataTabletLandscape; }
				set { dataTabletLandscape = value; }
			}

			public string DataTabletPortrait
			{
				get { return dataTabletPortrait; }
				set { dataTabletPortrait = value; }
			}
		}
	}
}