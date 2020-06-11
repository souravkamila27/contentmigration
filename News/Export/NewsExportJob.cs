using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Corporate.Web.Configuration;
using EPiServer.Core;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;

namespace Corporate.Web.Cms.Schedulers.News.Export
{
    [ScheduledPlugIn(DisplayName = "News export", Description = "Export News Items pages to XML files", SortIndex = 6)]
    public class NewsExportJob : ScheduledJobBase
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private ConcurrentQueue<string> Errors { get; set; } 
        private ConcurrentQueue<string> ExportedPages { get; set; } 
        private INewsPageExportHelper NewsPageExportHelper { get; set; }

        public NewsExportJob()
        {
            IsStoppable = true;
            _cancellationTokenSource = new CancellationTokenSource();
            NewsPageExportHelper = ServiceLocator.Current.GetInstance<INewsPageExportHelper>();
            Errors = new ConcurrentQueue<string>();
            ExportedPages = new ConcurrentQueue<string>();
        }

        public override void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        public override string Execute()
        {
            var containerId = AppSettings.NewsExportContainer;
            OnStatusChanged($"Starting news export execution from '{containerId}' container");
            NewsPageExportHelper.Export(new ContentReference(containerId), 
                AppSettings.NewsExportDirectory, _cancellationTokenSource, OnStatusChanged, 
                ExportedPages, Errors);

            OnStatusChanged($"Export news was completed from '{containerId}' container");
            return GetJobResultMessage();
        }

        #region Helpers

        private string GetJobResultMessage()
        {
            var commonMessage = $"{System.Environment.MachineName} - Exported pages: {ExportedPages.Count}";
            if (Errors.Count == 0)
            {
                return commonMessage;
            }

            var formattedMessages = Errors.Select(s => $"<pre>{s}</pre>").ToList();
            return $"{commonMessage} - Errors: {formattedMessages.Count} {string.Join(" ", formattedMessages)}";
        }

        #endregion
    }
}