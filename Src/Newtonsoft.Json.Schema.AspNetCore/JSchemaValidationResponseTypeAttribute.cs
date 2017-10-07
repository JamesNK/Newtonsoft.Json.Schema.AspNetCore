#region License
// Copyright (c) Newtonsoft. All Rights Reserved.
// License: https://raw.github.com/JamesNK/Newtonsoft.Json.Schema.AspNetCore/master/LICENSE.md
#endregion

using System;
using Microsoft.AspNetCore.Mvc;

namespace Newtonsoft.Json.Schema.AspNetCore
{
    /// <summary>
    /// A filter that specifies the type of the value and status code returned by the action. A schema path can optionally be specified to validate the response.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class JSchemaValidationResponseTypeAttribute : ProducesResponseTypeAttribute
    {
        /// <summary>
        /// Gets or sets the path of the schema file used to validate the response.
        /// </summary>
        public string ResponseSchema { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JSchemaValidationResponseTypeAttribute"/> class with the specified <see cref="Type"/> and status code.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of object that is going to be written in the response.</param>
        /// <param name="statusCode">The HTTP response status code.</param>
        public JSchemaValidationResponseTypeAttribute(Type type, int statusCode) : base(type, statusCode)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JSchemaValidationResponseTypeAttribute"/> class with the specified response schema path and status code.
        /// </summary>
        /// <param name="responseSchema">The path of the schema file used to validate the response.</param>
        /// <param name="statusCode">The HTTP response status code.</param>
        public JSchemaValidationResponseTypeAttribute(string responseSchema, int statusCode) : base(typeof(object), statusCode)
        {
            ResponseSchema = responseSchema;
        }
    }
}