// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Composition;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;

    using HtmlAgilityPack;
    using Newtonsoft.Json;

    [Export(nameof(ExtractSearchIndex), typeof(IPostProcessor))]
    public class ExtractSearchIndex : IPostProcessor
    {
        private static readonly Regex RegexWhiteSpace = new Regex(@"\s+", RegexOptions.Compiled);
        public const string IndexFileName = "index.json";

        public string Name => nameof(ExtractSearchIndex);

        public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
        {
            if (!metadata.ContainsKey("_enableSearch"))
            {
                metadata = metadata.Add("_enableSearch", true);
            }
            return metadata;
        }

        public Manifest Process(Manifest manifest, IFileAbstractLayer fal)
        {
            if (fal == null)
            {
                throw new ArgumentNullException("Base directory can not be null");
            }
            if (manifest?.Files == null)
            {
                return manifest;
            }
            var indexData = new Dictionary<string, SearchIndexItem>();
            var htmlFiles = (from item in manifest.Files
                             from output in item.OutputFiles
                             where output.Key.Equals(".html", StringComparison.OrdinalIgnoreCase)
                             select output.Value.RelativePath).ToList();
            if (htmlFiles.Count == 0)
            {
                return manifest;
            }

            Logger.LogInfo($"Extracting index data from {htmlFiles.Count} html files");
            foreach (var relativePath in htmlFiles)
            {
                var html = new HtmlDocument();
                Logger.LogVerbose($"Extracting index data from {relativePath}");

                if (fal.Exists(relativePath))
                {
                    try
                    {
                        html.Load(fal.OpenRead(relativePath), Encoding.UTF8);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Warning: Can't load content from {relativePath}: {ex.Message}");
                        continue;
                    }
                    var indexItem = ExtractItem(html, relativePath);
                    if (indexItem != null)
                    {
                        indexData[relativePath] = indexItem;
                    }
                }
            }
            using (var indexDataFileStream = fal.Create(IndexFileName))
            using (var sw = new StreamWriter(indexDataFileStream))
            {
                JsonUtility.Serialize(sw, indexData, Formatting.Indented);
            }

            // add index.json to mainfest as resource file
            var manifestItem = new ManifestItem
            {
                DocumentType = "Resource",
                Metadata = new Dictionary<string, object>(),
                OutputFiles = new Dictionary<string, OutputFileInfo>()
            };
            manifestItem.OutputFiles.Add("resource", new OutputFileInfo
            {
                RelativePath = IndexFileName,
            });

            manifest.Files.Add(manifestItem);
            return manifest;
        }

        internal SearchIndexItem ExtractItem(HtmlDocument html, string href)
        {
            var contentBuilder = new StringBuilder();

            // Select content between the data-searchable class tag
            var nodes = html.DocumentNode.SelectNodes("//*[contains(@class,'data-searchable')]") ?? Enumerable.Empty<HtmlNode>();
            // Select content between the article tag
            nodes = nodes.Union(html.DocumentNode.SelectNodes("//article") ?? Enumerable.Empty<HtmlNode>());
            foreach (var node in nodes)
            {
                ExtractTextFromNode(node, contentBuilder);
            }

            var content = NormalizeContent(contentBuilder.ToString());
            var title = ExtractTitleFromHtml(html);

            return new SearchIndexItem { Href = href, Title = title, Keywords = content };
        }

        private string ExtractTitleFromHtml(HtmlDocument html)
        {
            var titleNode = html.DocumentNode.SelectSingleNode("//head/title");
            var originalTitle = titleNode?.InnerText;
            return NormalizeContent(originalTitle);
        }

        private string NormalizeContent(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }
            str = StringHelper.HtmlDecode(str);
            return RegexWhiteSpace.Replace(str, " ").Trim();
        }

        private void ExtractTextFromNode(HtmlNode root, StringBuilder contentBuilder)
        {
            if (root == null)
            {
                return;
            }

            if (!root.HasChildNodes)
            {
                contentBuilder.Append(root.InnerText);
                contentBuilder.Append(" ");
            }
            else
            {
                foreach (var node in root.ChildNodes)
                {
                    ExtractTextFromNode(node, contentBuilder);
                }
            }
        }
    }
}
