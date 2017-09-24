#region License
// Copyright (c) Newtonsoft. All Rights Reserved.
// License: https://raw.github.com/JamesNK/Newtonsoft.Json.Schema.AspNetCore/master/LICENSE.md
#endregion

using System;

namespace Newtonsoft.Json.Schema.AspNetCore.Infrastructure.Model
{
    internal class ResponseTypeModel
    {
        public ResponseTypeModel(Type type, JSchema schema, int statusCode)
        {
            Type = type;
            Schema = schema;
            StatusCode = statusCode;
        }

        public Type Type { get; }
        public JSchema Schema { get; }
        public int StatusCode { get; }
    }
}