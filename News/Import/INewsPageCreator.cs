using EPiServer.Core;

namespace Corporate.Web.Cms.Schedulers.News.Import
{
    public interface INewsPageCreator
    {
        /// <summary>
        /// Execute page creation
        /// </summary>
        /// <param name="feedUrl">Xml url</param>
        /// <param name="parentContainer">PageReference to where the page should be created under</param>
        /// <returns></returns>
        string ExecutePageCreation(string feedUrl, ContentReference parentContainer);
    }
}