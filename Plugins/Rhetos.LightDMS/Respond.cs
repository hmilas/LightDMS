﻿/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Newtonsoft.Json;
using Rhetos.Logging;
using System;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Web;

namespace Rhetos.LightDMS
{
    public static class Respond
    {
        /// <summary>
        /// Logs the error details, and returns a generic error response with HTTP status code 500.
        /// </summary>
        public static void InternalError(HttpContext context, Exception exception, [CallerFilePath] string sourceFilePath = null)
        {
            string userMessage = $"Internal server error occurred. See RhetosServer.log for more information. ({exception.GetType().Name}, {DateTime.Now.ToString("s")})";
            LogAndReturnError(context, exception.ToString(), userMessage, sourceFilePath, HttpStatusCode.InternalServerError, EventType.Error);
        }

        /// <summary>
        /// Logs the error details (if trace log enabled), and returns the provided error message with HTTP status code 400.
        /// </summary>
        public static void BadRequest(HttpContext context, string error, [CallerFilePath] string sourceFilePath = null)
        {
            LogAndReturnError(context, error, error, sourceFilePath, HttpStatusCode.BadRequest, EventType.Info);
        }

        private static void LogAndReturnError(HttpContext context, string logMessage, string userMessage, string sourceFilePath, HttpStatusCode statusCode, EventType logEventType)
        {
            string loggerName = !string.IsNullOrEmpty(sourceFilePath)
                ? nameof(LightDMS) + "." + Path.GetFileNameWithoutExtension(sourceFilePath)
                : nameof(LightDMS);

            var logger = new NLogProvider().GetLogger(loggerName);
            logger.Write(logEventType, logMessage);

            if (!logMessage.Contains(DownloadHelper.ResponseBlockedMessage))
            {
                context.Response.Clear();
                try
                {
                    context.Response.ContentType = "application/json;";
                    context.Response.StatusCode = (int)statusCode;
                }
                catch (HttpException ex) when (ex.Message.StartsWith("Server cannot set content type after HTTP headers have been sent."))
                {
                    logger.Error(ex.ToString());
                }
                context.Response.Write(JsonConvert.SerializeObject(new { error = userMessage }));
            }
        }

        public static void Ok<T>(HttpContext context, T response)
        {
            context.Response.ContentType = "application/json;";
            context.Response.Write(JsonConvert.SerializeObject(response));
            context.Response.StatusCode = 200;
        }
    }
}
