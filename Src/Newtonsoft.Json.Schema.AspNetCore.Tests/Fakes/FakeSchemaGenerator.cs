#region License
// Copyright (c) Newtonsoft. All Rights Reserved.
// License: https://raw.github.com/JamesNK/Newtonsoft.Json.Schema.AspNetCore/master/LICENSE.md
#endregion

using System;
using Newtonsoft.Json.Schema.AspNetCore.Infrastructure;

namespace Newtonsoft.Json.Schema.AspNetCore.Tests.Fakes
{
    public class FakeSchemaGenerator : ISchemaGenerator
    {
        public JSchema GeneratedSchema { get; set; }
        public Type GenerateSchemaType { get; set; }

        public JSchema GetGeneratedSchema(Type type)
        {
            GenerateSchemaType = type;

            return GeneratedSchema ?? new JSchema();
        }
    }
}
