using Corporate.Web.Configuration;
using EPiServer.Core;
using EPiServer.PlugIn;
using System;
using System.IO;
using System.Text;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using Corporate.Eipa.Contracts;

namespace Corporate.Web.Cms.Schedulers.News.Import
{
	[ScheduledPlugIn(DisplayName = "News import", Description = "Imports News Items from XML files", SortIndex = 5)]
	public class NewsImportJob : ScheduledJobBase
	{
	    private INewsPageCreator PageCreator { get; set; }
	    private IEipaFileHelper EipaFileHelper { get; set; }

	    public NewsImportJob()
	    {
	        PageCreator = ServiceLocator.Current.GetInstance<INewsPageCreator>();
	        EipaFileHelper = ServiceLocator.Current.GetInstance<IEipaFileHelper>();
	    }


        /// <summary>
        /// Method that is invoked by time-schedule or manual invokation from admin gui
        /// </summary>
        /// <returns></returns>
        public override string Execute()
		{
            try
            {
                // Create the logg messages
                var log = new StringBuilder();

                // Get the urls to the feed
                var sourceDirectory = AppSettings.NewsImportSourceDirectory;
                var files = Directory.GetFiles(EipaFileHelper.GetFilePath(sourceDirectory), "*", SearchOption.AllDirectories);

                // Get the container to import the pages to
                var newsContainer = new ContentReference(AppSettings.NewsImportContainer);

                foreach (var file in files)
                {
                    // Execute the PageCreator and append to the log file the result
                    var message = PageCreator.ExecutePageCreation(file, newsContainer);

                    // Set the message to the log
                    log.Append(message);
                }

                //PageCreator = null;
                return $"{Environment.MachineName} - Total files: {files.Length} - Errors: <pre>{log}</pre>";
            }
            catch (Exception x)
            {
                return Environment.MachineName + ": " + x.Message + " " + x.StackTrace;
            }
        }
    }
}