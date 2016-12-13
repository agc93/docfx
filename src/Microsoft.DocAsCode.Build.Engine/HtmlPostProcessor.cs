// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Text;

    using HtmlAgilityPack;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Plugins;

    internal sealed class HtmlPostProcessor : IPostProcessor
    {
        public List<HtmlDocumentHandler> Handlers { get; } = new List<HtmlDocumentHandler>();

        public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
        {
            return metadata;
        }

        public Manifest Process(Manifest manifest, IFileAbstractLayer fal)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }
            if (fal == null)
            {
                throw new ArgumentNullException(nameof(fal));
            }
            foreach (var handler in Handlers)
            {
                manifest = handler.PreHandleWithScopeWrapper(manifest);
            }
            foreach (var tuple in from item in manifest.Files ?? Enumerable.Empty<ManifestItem>()
                                  from output in item.OutputFiles
                                  where output.Key.Equals(".html", StringComparison.OrdinalIgnoreCase)
                                  select new
                                  {
                                      Item = item,
                                      InputFile = item.SourceRelativePath,
                                      OutputFile = output.Value.RelativePath,
                                  })
            {
                if (!fal.Exists(tuple.OutputFile))
                {
                    continue;
                }
                var document = new HtmlDocument();
                try
                {
                    using (var fs = fal.OpenRead(tuple.OutputFile))
                    {
                        document.Load(fs, Encoding.UTF8);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Warning: Can't load content from {tuple.OutputFile}: {ex.Message}");
                    continue;
                }
                foreach (var handler in Handlers)
                {
                    handler.HandleWithScopeWrapper(document, tuple.Item, tuple.InputFile, tuple.OutputFile);
                }
                using (var fs = fal.Create(tuple.OutputFile))
                {
                    document.Save(fs, Encoding.UTF8);
                }
            }
            foreach (var handler in Handlers)
            {
                manifest = handler.PostHandleWithScopeWrapper(manifest);
            }
            return manifest;
        }
    }
}
