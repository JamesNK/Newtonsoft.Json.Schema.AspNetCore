#region License
// Copyright (c) Newtonsoft. All Rights Reserved.
// License: https://raw.github.com/JamesNK/Newtonsoft.Json.Schema.AspNetCore/master/LICENSE.md
#endregion

using System;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Newtonsoft.Json.Schema.AspNetCore.Infrastructure
{
    internal class SchemaLoader : ISchemaLoader
    {
        private static readonly ConcurrentDictionary<string, JSchema> LoadedSchemas = new ConcurrentDictionary<string, JSchema>();

        public JSchema GetLoadedSchema(IHostingEnvironment hostingEnvironment, string path)
        {
            return LoadedSchemas.GetOrAdd(path, p =>
            {
                IFileInfo fileInfo = hostingEnvironment.ContentRootFileProvider.GetFileInfo(p);
                if (!fileInfo.Exists)
                {
                    throw new InvalidOperationException($"Could not find '{p}'.");
                }

                using (Stream stream = fileInfo.CreateReadStream())
                using (StreamReader sr = new StreamReader(stream))
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    return JSchema.Load(reader);
                }
            });
        }
    }
}