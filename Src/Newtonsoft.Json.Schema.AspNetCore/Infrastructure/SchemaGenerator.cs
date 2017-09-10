#region License
// Copyright (c) Newtonsoft. All Rights Reserved.
// License: https://raw.github.com/JamesNK/Newtonsoft.Json.Schema.AspNetCore/master/LICENSE.md
#endregion

using System;
using System.Collections.Concurrent;
using Newtonsoft.Json.Schema.Generation;

namespace Newtonsoft.Json.Schema.AspNetCore.Infrastructure
{
    internal class SchemaGenerator : ISchemaGenerator
    {
        private static readonly ConcurrentDictionary<Type, JSchema> SchemaCache = new ConcurrentDictionary<Type, JSchema>();

        private static JSchema CreateSchema(Type type)
        {
            JSchemaGenerator schemaGenerator = new JSchemaGenerator();
            return schemaGenerator.Generate(type);
        }

        public JSchema GetGenerateSchema(Type type)
        {
            return SchemaCache.GetOrAdd(type, CreateSchema);
        }
    }
}