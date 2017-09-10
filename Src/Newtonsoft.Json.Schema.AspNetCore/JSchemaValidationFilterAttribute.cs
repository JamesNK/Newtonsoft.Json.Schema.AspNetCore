#region License
// Copyright (c) Newtonsoft. All Rights Reserved.
// License: https://raw.github.com/JamesNK/Newtonsoft.Json.Schema.AspNetCore/master/LICENSE.md
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema.AspNetCore.Infrastructure;

namespace Newtonsoft.Json.Schema.AspNetCore
{
    /// <summary>
    /// A filter that validates input and output JSON against JSON Schemas.
    /// </summary>
    public class JSchemaValidationFilterAttribute : Attribute, IAsyncResourceFilter, IAsyncResultFilter
    {
        internal ISchemaGenerator SchemaGenerator = new SchemaGenerator();

        private static readonly EmptyModelMetadataProvider ModelMetadataProvider = new EmptyModelMetadataProvider();

        /// <inheritdoc />
        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            ParameterDescriptor bodyParameter = context.ActionDescriptor.Parameters.FirstOrDefault(p => p.BindingInfo.BindingSource == BindingSource.Body);

            Stream originalRequestBody = null;

            if (bodyParameter != null)
            {
                Type requestType = bodyParameter.ParameterType;

                if (!typeof(JToken).IsAssignableFrom(requestType))
                {
                    JSchema requestSchema = SchemaGenerator.GetGenerateSchema(requestType);

                    MemoryStream buffer = new MemoryStream();
                    originalRequestBody = context.HttpContext.Request.Body;

                    await context.HttpContext.Request.Body.CopyToAsync(buffer);
                    buffer.Seek(0, SeekOrigin.Begin);

                    IList<ValidationError> validationErrors = ValidationHelper.Validate(buffer, requestSchema);

                    if (validationErrors.Count > 0)
                    {
                        JSchemaValidationErrorsException ex = JSchemaValidationErrorsException.Create(validationErrors);

                        context.ModelState.AddModelError(bodyParameter.Name, ex, ModelMetadataProvider.GetMetadataForType(requestType));
                    }

                    buffer.Seek(0, SeekOrigin.Begin);
                    context.HttpContext.Request.Body = buffer;
                }
            }

            await next();

            if (originalRequestBody != null)
            {
                context.HttpContext.Request.Body = originalRequestBody;
            }
        }

        /// <inheritdoc />
        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            JSchema responseSchema = null;

            if (context.ActionDescriptor is ControllerActionDescriptor descriptor)
            {
                Type returnType = descriptor.MethodInfo.ReturnType;

                if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    returnType = returnType.GetGenericArguments()[0];
                }

                if (returnType != typeof(void)
                    && returnType != typeof(Task)
                    && !typeof(JToken).IsAssignableFrom(returnType)
                    && !typeof(IActionResult).IsAssignableFrom(returnType))
                {
                    responseSchema = SchemaGenerator.GetGenerateSchema(returnType);
                }
            }

            MemoryStream buffer = null;
            Stream initialStream = null;

            if (responseSchema != null)
            {
                buffer = new MemoryStream();

                //replace the context response with our buffer
                initialStream = context.HttpContext.Response.Body;
                context.HttpContext.Response.Body = buffer;
            }

            //invoke the rest of the pipeline
            await next();

            if (responseSchema != null)
            {
                //reset the buffer and read out the contents
                buffer.Seek(0, SeekOrigin.Begin);

                IList<ValidationError> validationErrors = ValidationHelper.Validate(buffer, responseSchema);

                //reset to start of stream
                buffer.Seek(0, SeekOrigin.Begin);

                //copy our content to the original stream and put it back
                await buffer.CopyToAsync(initialStream);
                context.HttpContext.Response.Body = initialStream;

                if (validationErrors.Count > 0)
                {
                    throw JSchemaValidationErrorsException.Create(validationErrors);
                }
            }
        }
    }
}