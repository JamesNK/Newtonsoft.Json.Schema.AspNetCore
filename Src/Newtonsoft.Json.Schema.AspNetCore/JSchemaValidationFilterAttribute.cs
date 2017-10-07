#region License
// Copyright (c) Newtonsoft. All Rights Reserved.
// License: https://raw.github.com/JamesNK/Newtonsoft.Json.Schema.AspNetCore/master/LICENSE.md
#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema.AspNetCore.Infrastructure;
using Newtonsoft.Json.Schema.AspNetCore.Infrastructure.Model;

namespace Newtonsoft.Json.Schema.AspNetCore
{
    /// <summary>
    /// A filter that validates input and output JSON against JSON Schemas.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class JSchemaValidationFilterAttribute : Attribute, IAsyncResourceFilter, IAsyncResultFilter
    {
        private static readonly EmptyModelMetadataProvider ModelMetadataProvider = new EmptyModelMetadataProvider();

        private readonly ConcurrentDictionary<ActionDescriptor, ActionValidationModel> ActionModels = new ConcurrentDictionary<ActionDescriptor, ActionValidationModel>();

        internal ISchemaGenerator SchemaGenerator = new SchemaGenerator();
        internal ISchemaLoader SchemaLoader = new SchemaLoader();

        /// <summary>
        /// Gets or sets the path of the schema file used to validate the request.
        /// </summary>
        public string RequestSchema { get; set; }

        /// <summary>
        /// Gets or sets the path of the schema file used to validate the response.
        /// </summary>
        public string ResponseSchema { get; set; }

        private ActionValidationModel GetModel(ActionDescriptor actionDescriptor, IServiceProvider serviceProvider)
        {
            return ActionModels.GetOrAdd(actionDescriptor, d => ActionValidationModel.Create(d, this, (IHostingEnvironment)serviceProvider.GetService(typeof(IHostingEnvironment))));
        }

        /// <inheritdoc />
        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            ActionValidationModel model = GetModel(context.ActionDescriptor, context.HttpContext.RequestServices);

            Stream originalRequestBody = null;

            if (model.RequestBodySchema != null)
            {
                MemoryStream buffer = new MemoryStream();
                originalRequestBody = context.HttpContext.Request.Body;

                await context.HttpContext.Request.Body.CopyToAsync(buffer);
                buffer.Seek(0, SeekOrigin.Begin);

                IList<ValidationError> validationErrors = ValidationHelper.Validate(buffer, model.RequestBodySchema);

                if (validationErrors.Count > 0)
                {
                    JSchemaValidationErrorsException ex = JSchemaValidationErrorsException.Create(validationErrors);

                    context.ModelState.AddModelError(
                        model.RequestBodyName,
                        ex,
                        ModelMetadataProvider.GetMetadataForType(model.RequestBodyType));
                }

                buffer.Seek(0, SeekOrigin.Begin);
                context.HttpContext.Request.Body = buffer;
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
            HttpResponse response = context.HttpContext.Response;
            ActionValidationModel model = GetModel(context.ActionDescriptor, context.HttpContext.RequestServices);

            MemoryStream buffer = null;
            Stream initialStream = null;

            if (model.BufferResponse)
            {
                buffer = new MemoryStream();

                //replace the context response with our buffer
                initialStream = response.Body;
                response.Body = buffer;
            }

            //invoke the rest of the pipeline
            await next();

            if (model.BufferResponse)
            {
                Debug.Assert(buffer != null);

                //reset the buffer and read out the contents
                buffer.Seek(0, SeekOrigin.Begin);

                IList<ValidationError> validationErrors = null;

                if (response.ContentType != null
                    && (response.ContentType.StartsWith(Constants.ContentTypes.ApplicationJson, StringComparison.Ordinal) || response.ContentType.StartsWith(Constants.ContentTypes.TextJson, StringComparison.Ordinal)))
                {
                    ResponseTypeModel responseTypeModel = model.ResponseTypes.SingleOrDefault(r => r.StatusCode == response.StatusCode);

                    if (responseTypeModel != null)
                    {
                        JSchema responseSchema = SchemaGenerator.GetGeneratedSchema(responseTypeModel.Type);

                        validationErrors = ValidationHelper.Validate(buffer, responseSchema);

                        //reset to start of stream
                        buffer.Seek(0, SeekOrigin.Begin);
                    }
                }

                //copy our content to the original stream and put it back
                await buffer.CopyToAsync(initialStream);
                response.Body = initialStream;

                if (validationErrors != null && validationErrors.Count > 0)
                {
                    throw JSchemaValidationErrorsException.Create(validationErrors);
                }
            }
        }
    }
}