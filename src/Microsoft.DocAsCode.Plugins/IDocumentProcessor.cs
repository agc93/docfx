// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Immutable;

    public interface IDocumentProcessor
    {
        string Name { get; }

        ImmutableArray<IDocumentBuildStep> BuildSteps { get; }

        ProcessingPriority GetProcessingPriority(IFileAbstractLayer fal, FileAndType file);

        FileModel Load(IFileAbstractLayer fal, FileAndType file, ImmutableDictionary<string, object> metadata);

        // TODO: rename
        SaveResult Save(IFileAbstractLayer fal, FileModel model);

        void UpdateHref(FileModel model, IDocumentBuildContext context);
    }
}
