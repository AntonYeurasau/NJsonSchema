﻿//-----------------------------------------------------------------------
// <copyright file="DefaultReflectionService.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Namotion.Reflection;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NJsonSchema.Generation
{
    /// <inheritdocs />
    public class SystemTextJsonReflectionService : ReflectionServiceBase<SystemTextJsonSchemaGeneratorSettings>
    {
        /// <inheritdocs />
        public override void GenerateProperties(Type type, JsonSchema schema, SystemTextJsonSchemaGeneratorSettings settings, JsonSchemaGenerator schemaGenerator, JsonSchemaResolver schemaResolver)
        {
            foreach (var accessorInfo in type.GetContextualAccessors())
            {
                if (accessorInfo.MemberInfo is FieldInfo fieldInfo && (fieldInfo.IsPrivate || fieldInfo.IsStatic || !fieldInfo.IsDefined(typeof(DataMemberAttribute))))
                {
                    continue;
                }

                if (accessorInfo.MemberInfo is PropertyInfo propertyInfo && (
                    !(propertyInfo.GetMethod?.IsPrivate != true && propertyInfo.GetMethod?.IsStatic == false) ||
                    !(propertyInfo.SetMethod?.IsPrivate != true && propertyInfo.SetMethod?.IsStatic == false) &&
                    !propertyInfo.IsDefined(typeof(DataMemberAttribute))))
                {
                    continue;
                }

                var propertyIgnored = false;
                var jsonIgnoreAttribute = accessorInfo
                    .ContextAttributes
                    .FirstAssignableToTypeNameOrDefault("System.Text.Json.Serialization.JsonIgnoreAttribute", TypeNameStyle.FullName);

                if (jsonIgnoreAttribute != null)
                {
                    var condition = jsonIgnoreAttribute.TryGetPropertyValue<object>("Condition");
                    if (condition is null || condition.ToString() == "Always")
                    {
                        propertyIgnored = true;
                    }
                }

                var ignored = propertyIgnored
                    || schemaGenerator.IsPropertyIgnoredBySettings(accessorInfo)
                    || accessorInfo.ContextAttributes
                    .FirstAssignableToTypeNameOrDefault("System.Text.Json.Serialization.JsonExtensionDataAttribute", TypeNameStyle.FullName) != null;

                if (!ignored)
                {
                    var propertyTypeDescription = ((IReflectionService)this).GetDescription(accessorInfo.AccessorType, settings);
                    var propertyName = GetPropertyName(accessorInfo, settings);
                    var propertyAlreadyExists = schema.Properties.ContainsKey(propertyName);

                    if (propertyAlreadyExists)
                    {
                        if (settings.GetActualFlattenInheritanceHierarchy(type))
                        {
                            schema.Properties.Remove(propertyName);
                        }
                        else
                        {
                            throw new InvalidOperationException("The JSON property '" + propertyName + "' is defined multiple times on type '" + type.FullName + "'.");
                        }
                    }

                    var requiredAttribute = accessorInfo.ContextAttributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.RequiredAttribute");

                    var isDataContractMemberRequired = schemaGenerator.GetDataMemberAttribute(accessorInfo, type)?.IsRequired == true;

                    var hasRequiredAttribute = requiredAttribute != null;
                    if (hasRequiredAttribute || isDataContractMemberRequired)
                    {
                        schema.RequiredProperties.Add(propertyName);
                    }

                    var isNullable = propertyTypeDescription.IsNullable && hasRequiredAttribute == false;

                    // TODO: Add default value
                    schemaGenerator.AddProperty(schema, accessorInfo, propertyTypeDescription, propertyName, requiredAttribute, hasRequiredAttribute, isNullable, null, schemaResolver);
                }
            }
        }

        /// <inheritdocs />
        public override string ConvertEnumValue(object value, SystemTextJsonSchemaGeneratorSettings settings)
        {
            // TODO(performance): How to improve this one here?
            var serializerOptions = new JsonSerializerOptions();
            foreach (var converter in settings.SerializerOptions.Converters)
            {
                serializerOptions.Converters.Add(converter);
            }

            if (!serializerOptions.Converters.OfType<JsonStringEnumConverter>().Any())
            {
                serializerOptions.Converters.Add(new JsonStringEnumConverter());
            }

            var json = JsonSerializer.Serialize(value, value.GetType(), serializerOptions);
            return JsonSerializer.Deserialize<string>(json);
        }

        private string GetPropertyName(ContextualAccessorInfo accessorInfo, SystemTextJsonSchemaGeneratorSettings settings)
        {
            dynamic jsonPropertyNameAttribute = accessorInfo.ContextAttributes
                               .FirstAssignableToTypeNameOrDefault("System.Text.Json.Serialization.JsonPropertyNameAttribute", TypeNameStyle.FullName);

            if (jsonPropertyNameAttribute != null && !string.IsNullOrEmpty(jsonPropertyNameAttribute.Name))
            {
                return jsonPropertyNameAttribute.Name;
            }

            if (settings.SerializerOptions.PropertyNamingPolicy != null)
            {
                return settings.SerializerOptions.PropertyNamingPolicy.ConvertName(accessorInfo.Name);
            }

            return accessorInfo.Name;
        }

    }
}