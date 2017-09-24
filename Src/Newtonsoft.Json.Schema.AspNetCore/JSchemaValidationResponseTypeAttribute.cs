#region License
// Copyright (c) Newtonsoft. All Rights Reserved.
// License: https://raw.github.com/JamesNK/Newtonsoft.Json.Schema.AspNetCore/master/LICENSE.md
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Newtonsoft.Json.Schema.AspNetCore
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class JSchemaValidationResponseTypeAttribute : ProducesResponseTypeAttribute
    {
        public string ResponseSchema { get; }

        public JSchemaValidationResponseTypeAttribute(Type type, int statusCode) : base(type, statusCode)
        {
        }

        public JSchemaValidationResponseTypeAttribute(string responseSchema, int statusCode) : base(typeof(object), statusCode)
        {
            ResponseSchema = responseSchema;
        }
    }
}
