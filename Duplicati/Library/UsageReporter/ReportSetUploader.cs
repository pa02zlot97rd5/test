﻿//  Copyright (C) 2015, The Duplicati Team
//  http://www.duplicati.com, info@duplicati.com
//
//  This library is free software; you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 2.1 of the
//  License, or (at your option) any later version.
//
//  This library is distributed in the hope that it will be useful, but
//  WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//  Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;
using CoCoL;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Duplicati.Library.Utility;

namespace Duplicati.Library.UsageReporter
{
    public static class ReportSetUploader
    {
        /// <summary>
        /// The tag used for logging
        /// </summary>
        private static readonly string LOGTAG = Logging.Log.LogTagFromType(typeof(ReportSetUploader));

        /// <summary>
        /// The maximum number of pending uploads
        /// </summary>
        private const int MAX_PENDING_UPLOADS = 50;

        /// <summary>
        /// The target upload url
        /// </summary>
        private const string UPLOAD_URL = "https://usage-reporter.duplicati.com/api/v1/report";

        /// <summary>
        /// Runs the upload process
        /// </summary>
        /// <returns>A tuple with the completion task and the channel to use</returns>
        public static Tuple<Task, IWriteChannel<string>> Run()
        {
            var channel = ChannelManager.CreateChannel<string>(
                buffersize: MAX_PENDING_UPLOADS, 
                pendingWritersOverflowStrategy: QueueOverflowStrategy.LIFO
            );

            var task = AutomationExtensions.RunTask(
                channel.AsRead(),

                async (chan) =>
                {
                    using(var httpClient = new HttpClient())
                        while (true)
                        {
                            var f = await chan.ReadAsync();

                            try
                            {
                                if (File.Exists(f))
                                {
                                    int rc;
                                    using (var fs = File.OpenRead(f))
                                    {
                                        if (fs.Length > 0)
                                        {
                                            var content = new StreamContent(fs);
                                            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");
                                            content.Headers.ContentLength = fs.Length;

                                            using (var resp = httpClient.PostAsync(UPLOAD_URL, content).Await())
                                                rc = (int)resp.StatusCode;
                                        }
                                        else
                                            rc = 200;
                                    }

                                    if (rc >= 200 && rc <= 299)
                                        File.Delete(f);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logging.Log.WriteErrorMessage(LOGTAG, "UploadFailed", ex, "UsageReporter failed");
                            }
                        }
                }
            );

            return new Tuple<Task, IWriteChannel<string>>(task, channel);
        }

    }
}

