using System;
using System.Collections.Concurrent;
using System.Threading;
using EPiServer.Core;

namespace Corporate.Web.Cms.Schedulers.News.Export
{
    public interface INewsPageExportHelper
    {
        void Export(ContentReference sourceDirectory, string destinationPath, CancellationTokenSource tokenSource, 
            Action<string> changeStatus, ConcurrentQueue<string> exportedPages, ConcurrentQueue<string> errors);
    }
}