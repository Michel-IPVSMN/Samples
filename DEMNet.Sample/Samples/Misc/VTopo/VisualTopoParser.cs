﻿//
// VisualTopoSample.cs
//
// Author:
//       Xavier Fischer 2020-6
//
// Copyright (c) 2020 Xavier Fischer
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
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SampleApp
{
    public static class VisualTopoParser
    {
        internal static void ParseEntryHeader(VisualTopoModel model, string entry)
        {
            var data = entry.Split(',');
            model.Name = data[0];
            model.EntryPointProjectionCode = data[4];
            double factor = model.EntryPointProjectionCode == "WGS84" ? 1d : 1000d;
            model.EntryPoint = new GeoPoint(
                double.Parse(data[2], CultureInfo.InvariantCulture) * factor
                , double.Parse(data[1], CultureInfo.InvariantCulture) * factor
                , double.Parse(data[3], CultureInfo.InvariantCulture));
            int srid = 0;
            switch (model.EntryPointProjectionCode)
            {
                case "UTM31": srid = 32631; break;
                case "LT3": srid = 27573; break;
                case "WGS84": srid = 4326; break;
                default: throw new NotImplementedException($"Projection not {model.EntryPointProjectionCode} not implemented");
            };
            model.SRID = srid;
            model.EntryPoint = model.EntryPoint;
        }

        internal static VisualTopoModel ParseHeader(VisualTopoModel model, StreamReader sr)
        {

            sr.ReadUntil(string.IsNullOrWhiteSpace);
            var headerLines = sr.ReadUntil(string.IsNullOrWhiteSpace)
                                .Where(s => !string.IsNullOrWhiteSpace(s))
                                .Select(s => s.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries))
                                .ToDictionary(s => s[0], s => s[1]);
            if (headerLines.TryGetValue("Trou", out string trou))
            {
                ParseEntryHeader(model, trou);
            }
            if (headerLines.TryGetValue("Club", out string club))
            {
                model.Author = club;
            }
            if (headerLines.TryGetValue("Entree", out string entree))
            {
                model.Entree = entree;
            }
            if (headerLines.TryGetValue("Toporobot", out string toporobot))
            {
                model.TopoRobot = toporobot == "1";
            }
            if (headerLines.TryGetValue("Couleur", out string couleur))
            {
                model.DefaultColor = ParseColor(couleur);
            }
            return model;
        }

        internal static VisualTopoModel ParseSet(VisualTopoModel model, StreamReader sr, bool decimalDegrees, bool ignoreStars)
        {
            VisualTopoSet set = new VisualTopoSet();

            string setHeader = sr.ReadLine();

            if (setHeader.StartsWith("[Configuration "))
            {
                sr.ReadToEnd(); // skip until end of stream
                return model;
            }

            // Set header
            var data = setHeader.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var headerSlots = data[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            set.Color = VisualTopoParser.ParseColor(headerSlots[headerSlots.Length-3]);
            set.Name = data.Length > 1 ? data[1].Trim() : string.Empty;

            sr.Skip(1);
            var dataLine = sr.ReadLine();
            do
            {
                VisualTopoData topoData = new VisualTopoData();

                var parts = dataLine.Split(';');
                if (parts.Length > 1) topoData.Comment = parts[1].Trim();
                var slots = parts[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);

                Debug.Assert(slots.Length == 13);

                // Parse data line
                topoData = VisualTopoParser.ParseData(topoData, slots, decimalDegrees, ignoreStars);
                if (topoData != null)
                {
                    set.Data.Add(topoData);
                }
                dataLine = sr.ReadLine();
            }
            while (dataLine != string.Empty);

            model.Sets.Add(set);

            return model;
        }

        private static VisualTopoData ParseData(VisualTopoData topoData, string[] slots, bool decimalDegrees, bool ignoreStars)
        {
            const string DefaultSize = "2";

            topoData.Entree = slots[0];
            topoData.Sortie = slots[1];

            if (topoData.Sortie == "*" && ignoreStars)
                return null;

            topoData.Longueur = float.Parse(slots[2], CultureInfo.InvariantCulture);
            topoData.Cap = ParseAngle(float.Parse(slots[3], CultureInfo.InvariantCulture), decimalDegrees);
            topoData.Pente = ParseAngle(float.Parse(slots[4], CultureInfo.InvariantCulture), decimalDegrees);
            topoData.Section = new BoundingBox(
                                    float.Parse(slots[5] == "*" ? DefaultSize : slots[5], CultureInfo.InvariantCulture),
                                    float.Parse(slots[6] == "*" ? DefaultSize : slots[6], CultureInfo.InvariantCulture),
                                    float.Parse(slots[8] == "*" ? DefaultSize : slots[8], CultureInfo.InvariantCulture),
                                    float.Parse(slots[7] == "*" ? DefaultSize : slots[7], CultureInfo.InvariantCulture)
                                    );

            return topoData;
        }

        private static float ParseAngle(float degMinSec, bool decimalDegrees)
        {
            // 125 deg 30min is not 125,5 BUT 125,3
            if (decimalDegrees)
            {
                return degMinSec;
            }
            else
            {
                // sexagecimal

                float intPart = (float)Math.Truncate(degMinSec);
                float decPart = degMinSec - intPart;

                return intPart + MathHelper.Map(0f, 0.6f, 0f, 1f, Math.Abs(decPart), false) * Math.Sign(degMinSec);

            }
        }

        private static Vector4 ParseColor(string rgbCommaSeparated)
        {
            if (rgbCommaSeparated == "Std")
                return VectorsExtensions.CreateColor(255, 255, 255);

            var slots = rgbCommaSeparated.Split(',')
                        .Select(s => byte.Parse(s))
                        .ToArray();
            return VectorsExtensions.CreateColor(slots[0], slots[1], slots[2]);
        }

        internal static VisualTopoModel ParseFile(string vtopoFile, Encoding encoding, bool decimalDegrees, bool ignoreStars)
        {
            VisualTopoModel model = new VisualTopoModel();
            using (StreamReader sr = new StreamReader(vtopoFile, encoding))
            {
                model = VisualTopoParser.ParseHeader(model, sr);

                while (!sr.EndOfStream)
                {
                    model = VisualTopoParser.ParseSet(model, sr, decimalDegrees, ignoreStars);
                }
            }

            return model;
        }
    }



}
