#region License
// Copyright (c) Newtonsoft. All Rights Reserved.
// License: https://raw.github.com/JamesNK/Newtonsoft.Json.Schema.AspNetCore/master/LICENSE.md
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json.Linq;

namespace Newtonsoft.Json.Schema.AspNetCore.Infrastructure.Model
{
    internal class ActionValidationModel
    {
        public ActionDescriptor ActionDescriptor { get; private set; }
        public JSchema RequestBodySchema { get; private set; }
        public Type RequestBodyType { get; set; }
        public List<ResponseTypeModel> ResponseTypes { get; private set; }
        public string RequestBodyName { get; private set; }
        public bool BufferRequest { get; private set; }
        public bool BufferResponse { get; private set; }

        public static ActionValidationModel Create(ActionDescriptor action, JSchemaValidationFilterAttribute filter, IHostingEnvironment hostingEnvironment)
        {
            ActionValidationModel model = new ActionValidationModel();
            model.ActionDescriptor = action;

            ParameterDescriptor bodyParameter = action.Parameters?.FirstOrDefault(p => p.BindingInfo.BindingSource == BindingSource.Body);

            if (bodyParameter != null)
            {
                Type requestType = bodyParameter.ParameterType;

                model.RequestBodySchema = ResolveSchema(filter, hostingEnvironment, filter.RequestSchema, requestType);
                if (model.RequestBodySchema != null)
                {
                    model.RequestBodyName = bodyParameter.Name;
                    model.RequestBodyType = requestType;
                    model.BufferRequest = true;
                }
            }

            IApiResponseMetadataProvider[] responseMetadataAttributes = GetResponseMetadataAttributes(action);

            Type declaredReturnType;
            if (action is ControllerActionDescriptor controllerAction)
            {
                declaredReturnType = GetDeclaredReturnType(controllerAction);
            }
            else
            {
                declaredReturnType = null;
            }

            Dictionary<int, IApiResponseMetadataProvider> objectTypes = GetApiResponseTypes(responseMetadataAttributes, declaredReturnType, filter.ResponseSchema);

            List<ResponseTypeModel> responseTypes = new List<ResponseTypeModel>();
            foreach (KeyValuePair<int, IApiResponseMetadataProvider> objectType in objectTypes)
            {
                string responseSchema = (objectType.Value as JSchemaValidationResponseTypeAttribute)?.ResponseSchema;
                Type responseType = objectType.Value.Type;

                JSchema schema = ResolveSchema(filter, hostingEnvironment, responseSchema, responseType);

                if (schema != null)
                {
                    ResponseTypeModel apiResponseTypeModel = new ResponseTypeModel(
                        responseType,
                        schema,
                        objectType.Key);

                    responseTypes.Add(apiResponseTypeModel);
                }
            }

            model.ResponseTypes = responseTypes;
            model.BufferResponse = model.ResponseTypes.Count > 0;

            return model;
        }

        private static JSchema ResolveSchema(JSchemaValidationFilterAttribute filter, IHostingEnvironment hostingEnvironment, string path, Type type)
        {
            if (!string.IsNullOrEmpty(path))
            {
                return filter.SchemaLoader.GetLoadedSchema(hostingEnvironment, path);
            }
            else if (IsValidatableType(type))
            {
                return filter.SchemaGenerator.GetGeneratedSchema(type);
            }

            return null;
        }

        private static bool IsValidatableType(Type type)
        {
            return type != null && type != typeof(void) && !typeof(JToken).IsAssignableFrom(type);
        }

        private static Dictionary<int, IApiResponseMetadataProvider> GetApiResponseTypes(
            IApiResponseMetadataProvider[] responseMetadataAttributes,
            Type type,
            string responseSchema)
        {
            Dictionary<int, IApiResponseMetadataProvider> objectTypes = new Dictionary<int, IApiResponseMetadataProvider>();

            if (responseMetadataAttributes != null)
            {
                foreach (IApiResponseMetadataProvider metadataAttribute in responseMetadataAttributes)
                {
                    objectTypes[metadataAttribute.StatusCode] = metadataAttribute;
                }
            }

            if (objectTypes.Count == 0)
            {
                IApiResponseMetadataProvider metadataProvider = null;
                if (!string.IsNullOrEmpty(responseSchema))
                {
                    metadataProvider = new JSchemaValidationResponseTypeAttribute(responseSchema, StatusCodes.Status200OK);
                }
                else if (type != null)
                {
                    metadataProvider = new JSchemaValidationResponseTypeAttribute(type, StatusCodes.Status200OK);
                }

                if (metadataProvider != null)
                {
                    objectTypes[StatusCodes.Status200OK] = metadataProvider;
                }
            }

            return objectTypes;
        }

        private static Type GetDeclaredReturnType(ControllerActionDescriptor action)
        {
            Type declaredReturnType = action.MethodInfo.ReturnType;
            if (declaredReturnType == typeof(void) ||
                declaredReturnType == typeof(Task))
            {
                return null;
            }

            if (ReflectionHelpers.InheritsGenericDefinition(declaredReturnType, typeof(Task<>), out Type implementingType))
            {
                declaredReturnType = implementingType.GetGenericArguments()[0];
            }

            if (typeof(IActionResult).IsAssignableFrom(declaredReturnType))
            {
                return null;
            }
            else
            {
                return declaredReturnType;
            }
        }

        private static IApiResponseMetadataProvider[] GetResponseMetadataAttributes(ActionDescriptor action)
        {
            return action.FilterDescriptors?.Select(fd => fd.Filter)
                .OfType<IApiResponseMetadataProvider>()
                .ToArray();
        }
    }
}