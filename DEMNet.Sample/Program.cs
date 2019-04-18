﻿//
// Program.cs
//
// Author:
//       Xavier Fischer
//
// Copyright (c) 2019 
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

using DEM.Net.Core;
using System.IO;
using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using DEM.Net.glTF;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.Extensions.Logging.Console;

namespace DEMNet.Sample
{

    class Program
    {
        private static ServiceProvider _serviceProvider;

        static void Main(string[] args)
        {
            RegisterServices();

            var app = _serviceProvider.GetService<SampleApplication>();
            app.Run(_serviceProvider);

            Console.Write("Press any key to exit...");
            Console.ReadLine();
        }

        private static void RegisterServices()
        {
            var services = new ServiceCollection();
            services.AddLogging(config =>
            {
                config.AddDebug(); // Log to debug (debug window in Visual Studio or any debugger attached)
                config.AddConsole(); // Log to console (colored !)
            })
           .Configure<LoggerFilterOptions>(options =>
           {
               options.AddFilter<DebugLoggerProvider>(null /* category*/ , LogLevel.Information /* min level */);
               options.AddFilter<ConsoleLoggerProvider>(null  /* category*/ , LogLevel.Information /* min level */);
           })
           .AddDemNetCore()
           .AddDemNetglTF()
           .AddTransient<SampleApplication>()
           .AddTransient<STLSamples>();

            _serviceProvider = services.BuildServiceProvider();
        }
    }

    public class SampleApplication
    {
        private readonly ILogger<SampleApplication> _logger;

        public SampleApplication(ILogger<SampleApplication> logger)
        {
            _logger = logger;
        }

        internal void Run(IServiceProvider serviceProvider)
        {
            //Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

            Stopwatch sw = Stopwatch.StartNew();
            _logger.LogInformation("Application started");

            STLSamples stlSamples = serviceProvider.GetRequiredService<STLSamples>();
            stlSamples.Run(DEMDataSet.AW3D30);

            //GpxSamples gpxSamples = new GpxSamples(_OutputDataDirectory, Path.Combine(_OutputDataDirectory, "GPX", "venturiers.gpx"));
            //gpxSamples.Run(_serviceProvider);

            //DatasetSamples.Run(_serviceProvider);

            //ElevationSamples.Run(_serviceProvider);

            //TextureSamples textureSamples = new TextureSamples(_OutputDataDirectory);
            //textureSamples.Run(_serviceProvider);



            //ReprojectionSamples reprojSamples = new ReprojectionSamples("POLYGON ((-69.647827 -33.767732, -69.647827 -32.953368, -70.751202 -32.953368, -70.751202 -33.767732, -69.647827 -33.767732))");
            //reprojSamples.Run(_serviceProvider);


            ////OldSamples oldSamples = new OldSamples( _OutputDataDirectory);
            ////oldSamples.Run();


            _logger.LogTrace($"Application ran in : {sw.Elapsed:g}");
        }
    }
}
