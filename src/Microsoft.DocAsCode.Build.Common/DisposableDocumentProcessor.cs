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
        private ImmutableArray<IDocumentBuildStep>? _buildSteps;

        public ImmutableArray<IDocumentBuildStep> BuildSteps
        {
            get
            {
                if (_buildSteps == null)
                {
                    _buildSteps = GetBuildSteps().ToImmutableArray();
                }
                return _buildSteps.Value;
            }
        }

        public abstract string Name { get; }

        protected virtual IEnumerable<IDocumentBuildStep> GetBuildSteps()
        {
            if (InternalBuildSteps == null)
            {
                return Enumerable.Empty<IDocumentBuildStep>();
            }
            return from step in InternalBuildSteps
                   where step != null
                   orderby step.BuildOrder
                   select step;
        }

        public virtual IEnumerable<IDocumentBuildStep> InternalBuildSteps { get; set; }

        public abstract ProcessingPriority GetProcessingPriority(IFileAbstractLayer fal, FileAndType file);

        public abstract FileModel Load(IFileAbstractLayer fal, FileAndType file, ImmutableDictionary<string, object> metadata);

        public abstract SaveResult Save(IFileAbstractLayer fal, FileModel model);

        public void Dispose()
        {
            if (_buildSteps != null)
            {
                foreach (var buildStep in _buildSteps.Value)
                {
                    Logger.LogVerbose($"Disposing build step {buildStep.Name} ...");
                    (buildStep as IDisposable)?.Dispose();
                }
                _buildSteps = null;
            }
        }

        // TODO: implement update href in each plugin
        public virtual void UpdateHref(FileModel model, IDocumentBuildContext context)
        {
        }
    }
}
