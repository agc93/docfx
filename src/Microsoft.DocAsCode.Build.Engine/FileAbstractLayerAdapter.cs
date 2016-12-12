// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Collections.Immutable;
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public class FileAbstractLayerAdapter : IFileAbstractLayer
    {
        public FileAbstractLayer Fal { get; }

        public FileAbstractLayerAdapter(FileAbstractLayer fal)
        {
            Fal = fal;
        }

        public bool CanRead => Fal.CanRead;

        public bool CanWrite => Fal.CanWrite;

        public void Copy(string sourceFileName, string destFileName) => Fal.Copy(sourceFileName, destFileName);

        public Stream Create(string file) => Fal.Create(file);

        public bool Exists(string file) => Fal.Exists(file);

        public IEnumerable<string> GetAllInputFiles() =>
            from r in Fal.GetAllInputFiles()
            select (string)r.RemoveWorkingFolder();

        public IEnumerable<string> GetAllOutputFiles() =>
            from r in Fal.GetAllOutputFiles()
            select (string)r.RemoveWorkingFolder();

        public ImmutableDictionary<string, string> GetProperties(string file) => Fal.GetProperties(file);

        public Stream OpenRead(string file) => Fal.OpenRead(file);
    }
}
