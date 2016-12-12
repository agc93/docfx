// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public abstract class DisposableDocumentProcessor : IDocumentProcessor, IDisposable
    {
        private List<IDocumentBuildStep> BuildSteps;

        public abstract string Name { get; }

        public IEnumerable<IDocumentBuildStep> GetBuildSteps()
        {
            if (BuildSteps == null)
            {
                BuildSteps = GetBuildStepsCore().ToList();
            }
            return BuildSteps;
        }

        protected abstract IEnumerable<IDocumentBuildStep> GetBuildStepsCore();

        public abstract ProcessingPriority GetProcessingPriority(IFileAbstractLayer fal, FileAndType file);

        public abstract FileModel Load(IFileAbstractLayer fal, FileAndType file, ImmutableDictionary<string, object> metadata);

        public abstract SaveResult Save(IFileAbstractLayer fal, FileModel model);

        public void Dispose()
        {
            if (BuildSteps != null)
            {
                foreach (var buildStep in BuildSteps)
                {
                    Logger.LogVerbose($"Disposing build step {buildStep.Name} ...");
                    (buildStep as IDisposable)?.Dispose();
                }
                BuildSteps = null;
            }
        }

        // TODO: implement update href in each plugin
        public virtual void UpdateHref(FileModel model, IDocumentBuildContext context)
        {
        }
    }
}
