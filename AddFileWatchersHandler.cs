﻿//
// AddFileWatchersHandler.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using System.Text;
using System.Threading;
using System.Net.Http;
using System.Net;

namespace TestManyFileWatchersAddin
{
	public class AddFileWatchersHandler : CommandHandler
	{
		protected override void Run ()
		{
			Task.Run (async () => await CreateFileWatchers ()).Ignore ();
		}

		public static List<FileSystemWatcher> Watchers = new List<FileSystemWatcher> ();

		async Task CreateFileWatchers ()
		{
			while (true) {
				bool result = await TestHttpConnection ();
				if (!result) {
					return;
				}

				var watcher = new FileSystemWatcher ();
				watcher.Path = "/";
				watcher.Filter = ".editorconfig";
				watcher.Changed += OnChanged;
				watcher.Deleted += OnDeleted;
				watcher.EnableRaisingEvents = true;
				Watchers.Add (watcher);
			}
		}

		static void OnChanged (object sender, FileSystemEventArgs e)
		{
		}

		static void OnDeleted (object sender, FileSystemEventArgs e)
		{
		}

		async Task<bool> TestHttpConnection ()
		{
			try {
				var url = "https://api.nuget.org/v3/index.json";
				using (HttpClient client = new HttpClient ()) {
					using (var response = await client.GetAsync (url)) {
						if (response.StatusCode == HttpStatusCode.OK) {
							return true;
						}
						throw new ApplicationException (string.Format ("Status code: {0}", response.StatusCode));
					}
				}
			} catch (Exception ex) {
				int workerThreadsMax, completionPortThreadsMax;
				ThreadPool.GetMaxThreads (out workerThreadsMax, out completionPortThreadsMax);

				int availableWorkerThreads, availableCompletionPortThreads;
				ThreadPool.GetAvailableThreads (out availableWorkerThreads, out availableCompletionPortThreads);
				int running = workerThreadsMax - availableWorkerThreads;

				await Runtime.RunInMainThread (() => {
					var builder = new StringBuilder ();
					builder.Append ("HttpClient request failed. ");
					builder.AppendLine (ex.Message);
					var innerEx = ex.InnerException;
					if (innerEx != null)
						builder.AppendLine (innerEx.Message);
					builder.AppendFormat ("Watchers created: {0}", Watchers.Count);
					builder.AppendLine ();

					builder.AppendFormat ("WorkerThreadsMax: {0} CompletionPortThreadsMax {1}", workerThreadsMax, completionPortThreadsMax);
					builder.AppendLine ();

					builder.AppendFormat ("WorkerThreadsAvailable: {0} CompletionPortThreadsAvailable {1}", availableWorkerThreads, availableCompletionPortThreads);
					builder.AppendLine ();
					MessageService.ShowMessage (builder.ToString ());
				});
				return false;
			}
		}
	}
}
