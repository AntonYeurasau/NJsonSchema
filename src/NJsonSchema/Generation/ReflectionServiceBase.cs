//-----------------------------------------------------------------------
// <copyright file="DefaultReflectionService.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Linq;
using NJsonSchema.Annotations;
using System.Reflection;
using Namotion.Reflection;

namespace NJsonSchema.Generation
{
    /// <summary>The default reflection service implementation.</summary>
    public abstract class ReflectionServiceBase<TSettings> : IReflectionService
        where TSettings : JsonSchemaGeneratorSettings
    {
        /// <summary>
        /// Converts an enum value to a JSON string.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public abstract string ConvertEnumValue(object value, TSettings settings);

        /// <summary>
        /// Generates the properties for the given type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="schema"></param>
        /// <param name="settings"></param>
        /// <param name="schemaGenerator"></param>
        /// <param name="schemaResolver"></param>
        public abstract void GenerateProperties(Type type, JsonSchema schema, TSettings settings, JsonSchemaGenerator schemaGenerator, JsonSchemaResolver schemaResolver);

        /// <summary>Creates a <see cref="JsonTypeDescription"/> from a <see cref="Type"/>. </summary>
        /// <param name="contextualType">The type.</param>
        /// <param name="defaultReferenceTypeNullHandling">The default reference type null handling.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonTypeDescription"/>. </returns>
        public JsonTypeDescription GetDescription(ContextualType contextualType, ReferenceTypeNullHandling defaultReferenceTypeNullHandling, TSettings settings)
        {
            var type = contextualType.OriginalType;
            var isNullable = IsNullable(contextualType, defaultReferenceTypeNullHandling);

            var jsonSchemaTypeAttribute = contextualType.GetAttribute<JsonSchemaTypeAttribute>();
            if (jsonSchemaTypeAttribute != null)
            {
                type = jsonSchemaTypeAttribute.Type;
                contextualType = type.ToContextualType();

                if (jsonSchemaTypeAttribute.IsNullableRaw.HasValue)
                {
                    isNullable = jsonSchemaTypeAttribute.IsNullableRaw.Value;
                }
            }

            var jsonSchemaAttribute = contextualType.GetAttribute<JsonSchemaAttribute>();
            if (jsonSchemaAttribute != null)
            {
                var classType = jsonSchemaAttribute.Type != JsonObjectType.None ? jsonSchemaAttribute.Type : JsonObjectType.Object;
                var format = !string.IsNullOrEmpty(jsonSchemaAttribute.Format) ? jsonSchemaAttribute.Format : null;
                return JsonTypeDescription.Create(contextualType, classType, isNullable, format);
            }

            return GetDescription(contextualType, settings, type, isNullable, defaultReferenceTypeNullHandling);
        }

        /// <summary>Creates a <see cref="JsonTypeDescription"/> from a <see cref="Type"/>. </summary>
        /// <param name="contextualType">The type.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="originalType">The original type.</param>
        /// <param name="isNullable">Specifies whether the type is nullable.</param>
        /// <param name="defaultReferenceTypeNullHandling">The default reference type null handling.</param>
        /// <returns>The <see cref="JsonTypeDescription"/>. </returns>
        protected virtual JsonTypeDescription GetDescription(ContextualType contextualType, TSettings settings, Type originalType, bool isNullable, ReferenceTypeNullHandling defaultReferenceTypeNullHandling)
        {
            if (originalType.GetTypeInfo().IsEnum)
            {
                var isStringEnum = IsStringEnum(contextualType, settings);
                return JsonTypeDescription.CreateForEnumeration(contextualType,
                    isStringEnum ? JsonObjectType.String : JsonObjectType.Integer, false);
            }

            // Primitive types

            if (originalType == typeof(short) ||
                originalType == typeof(uint) ||
                originalType == typeof(ushort))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.Integer, false, null);
            }

            if (originalType == typeof(int))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.Integer, false, JsonFormatStrings.Integer);
            }

            if (originalType == typeof(long) ||
                originalType == typeof(ulong))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.Integer, false, JsonFormatStrings.Long);
            }

            if (originalType == typeof(double))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.Number, false, JsonFormatStrings.Double);
            }

            if (originalType == typeof(float))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.Number, false, JsonFormatStrings.Float);
            }

            if (originalType == typeof(decimal))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.Number, false, JsonFormatStrings.Decimal);
            }

            if (originalType == typeof(bool))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.Boolean, false, null);
            }

            if (originalType == typeof(string) || originalType == typeof(Type))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.String, isNullable, null);
            }

            if (originalType == typeof(char))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.String, false, null);
            }

            if (originalType == typeof(Guid))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.String, false, JsonFormatStrings.Guid);
            }

            // Date & time types

            if (originalType == typeof(DateTime) ||
                originalType == typeof(DateTimeOffset) ||
                originalType.FullName == "NodaTime.OffsetDateTime" ||
                originalType.FullName == "NodaTime.LocalDateTime" ||
                originalType.FullName == "NodaTime.ZonedDateTime" ||
                originalType.FullName == "NodaTime.Instant")
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.String, false, JsonFormatStrings.DateTime);
            }

            if (originalType == typeof(TimeSpan) ||
                originalType.FullName == "NodaTime.Duration")
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.String, false, JsonFormatStrings.TimeSpan);
            }

            if (originalType.FullName == "NodaTime.LocalDate" ||
                originalType.FullName == "System.DateOnly")
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.String, false, JsonFormatStrings.Date);
            }

            if (originalType.FullName == "NodaTime.LocalTime" ||
                originalType.FullName == "System.TimeOnly")
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.String, false, JsonFormatStrings.Time);
            }

            // Special types

            if (originalType == typeof(Uri))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.String, isNullable, JsonFormatStrings.Uri);
            }

            if (originalType == typeof(byte))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.Integer, false, JsonFormatStrings.Byte);
            }

            if (originalType == typeof(byte[]))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.String, isNullable, JsonFormatStrings.Byte);
            }

            if (originalType.FullName == "Newtonsoft.Json.Linq.JArray")
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.Array, isNullable, null);
            }

            if (originalType.FullName == "Newtonsoft.Json.Linq.JToken" ||
                originalType.FullName == "Newtonsoft.Json.Linq.JObject" ||
                originalType.FullName == "System.Dynamic.ExpandoObject" ||
                originalType.FullName == "System.Text.Json.JsonElement" ||
                originalType == typeof(object))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.None, isNullable, null);
            }

            if (IsBinary(contextualType))
            {
                if (settings.SchemaType == SchemaType.Swagger2)
                {
                    return JsonTypeDescription.Create(contextualType, JsonObjectType.File, isNullable, null);
                }
                else
                {
                    return JsonTypeDescription.Create(contextualType, JsonObjectType.String, isNullable, JsonFormatStrings.Binary);
                }
            }

            if (contextualType.IsNullableType)
            {
                var typeDescription = GetDescription(contextualType.OriginalGenericArguments[0], defaultReferenceTypeNullHandling, settings);
                typeDescription.IsNullable = true;
                return typeDescription;
            }

            if (IsDictionaryType(contextualType))
            {
                return JsonTypeDescription.CreateForDictionary(contextualType, JsonObjectType.Object, isNullable);
            }

            if (IsIAsyncEnumerableType(contextualType) || IsArrayType(contextualType))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.Array, isNullable, null);
            }

            return JsonTypeDescription.Create(contextualType, JsonObjectType.Object, isNullable, null);
        }

        /// <summary>Checks whether a type is nullable.</summary>
        /// <param name="contextualType">The type.</param>
        /// <param name="defaultReferenceTypeNullHandling">The default reference type null handling used when no nullability information is available.</param>
        /// <returns>true if the type can be null.</returns>
        public virtual bool IsNullable(ContextualType contextualType, ReferenceTypeNullHandling defaultReferenceTypeNullHandling)
        {
            if (contextualType.ContextAttributes.FirstAssignableToTypeNameOrDefault("NotNullAttribute", TypeNameStyle.Name) != null)
            {
                return false;
            }

            if (contextualType.ContextAttributes.FirstAssignableToTypeNameOrDefault("CanBeNullAttribute", TypeNameStyle.Name) != null)
            {
                return true;
            }

            if (contextualType.Nullability != Nullability.Unknown)
            {
                return contextualType.Nullability == Nullability.Nullable;
            }

            var isValueType = contextualType.Type != typeof(string) &&
                              contextualType.TypeInfo.IsValueType;

            return isValueType == false &&
                   defaultReferenceTypeNullHandling != ReferenceTypeNullHandling.NotNull;
        }

        /// <summary>Checks whether the give type is a string enum.</summary>
        /// <param name="contextualType">The type.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The result.</returns>
        public virtual bool IsStringEnum(ContextualType contextualType, TSettings settings)
        {
            if (!contextualType.TypeInfo.IsEnum)
            {
                return false;
            }

            if (HasStringEnumConverter(contextualType))
            {
                return true;
            }

            return false;
        }

        /// <summary>Checks whether the given type is a file/binary type.</summary>
        /// <param name="contextualType">The type.</param>
        /// <returns>true or false.</returns>
        protected virtual bool IsBinary(ContextualType contextualType)
        {
            // TODO: Move all file handling to NSwag. How?

            var parameterTypeName = contextualType.TypeName;
            return parameterTypeName == "IFormFile" ||
                   contextualType.IsAssignableToTypeName("HttpPostedFile", TypeNameStyle.Name) ||
                   contextualType.IsAssignableToTypeName("HttpPostedFileBase", TypeNameStyle.Name) ||
                   contextualType.TypeInfo.ImplementedInterfaces.Any(i => i.Name == "IFormFile");
        }

        /// <summary>Checks whether the given type is an IAsyncEnumerable type.</summary>
        /// <remarks>
        /// See this issue: https://github.com/RicoSuter/NSwag/issues/2582#issuecomment-576165669
        /// </remarks>
        /// <param name="contextualType">The type.</param>
        /// <returns>true or false.</returns>
        private bool IsIAsyncEnumerableType(ContextualType contextualType)
        {
            return contextualType.TypeName == "IAsyncEnumerable`1";
        }

        /// <summary>Checks whether the given type is an array type.</summary>
        /// <param name="contextualType">The type.</param>
        /// <returns>true or false.</returns>
        protected virtual bool IsArrayType(ContextualType contextualType)
        {
            if (IsDictionaryType(contextualType))
            {
                return false;
            }

            if (contextualType.TypeName == "ObservableCollection`1")
            {
                return true;
            }

            return contextualType.Type.IsArray ||
                (contextualType.TypeInfo.ImplementedInterfaces.Contains(typeof(IEnumerable)) &&
                (contextualType.TypeInfo.BaseType == null ||
                    !contextualType.TypeInfo.BaseType.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IEnumerable))));
        }

        /// <summary>Checks whether the given type is a dictionary type.</summary>
        /// <param name="contextualType">The type.</param>
        /// <returns>true or false.</returns>
        protected virtual bool IsDictionaryType(ContextualType contextualType)
        {
            if (contextualType.TypeName == "IDictionary`2" || contextualType.TypeName == "IReadOnlyDictionary`2")
            {
                return true;
            }

            return contextualType.TypeInfo.ImplementedInterfaces.Contains(typeof(IDictionary)) &&
                (contextualType.TypeInfo.BaseType == null ||
                    !contextualType.TypeInfo.BaseType.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IDictionary)));
        }

        private bool HasStringEnumConverter(ContextualType contextualType)
        {
            dynamic jsonConverterAttribute = contextualType.Attributes?.FirstOrDefault(a => a.GetType().Name == "JsonConverterAttribute");
            if (jsonConverterAttribute != null && ObjectExtensions.HasProperty(jsonConverterAttribute, "ConverterType"))
            {
                var converterType = (Type)jsonConverterAttribute.ConverterType;
                if (converterType != null)
                {
                    return converterType.IsAssignableToTypeName("StringEnumConverter", TypeNameStyle.Name) ||
                           converterType.IsAssignableToTypeName("System.Text.Json.Serialization.JsonStringEnumConverter", TypeNameStyle.FullName);
                }
            }

            return false;
        }

        JsonTypeDescription IReflectionService.GetDescription(ContextualType contextualType, ReferenceTypeNullHandling defaultReferenceTypeNullHandling, JsonSchemaGeneratorSettings settings)
        {
            return GetDescription(contextualType, defaultReferenceTypeNullHandling, (TSettings)settings);
        }

        JsonTypeDescription IReflectionService.GetDescription(ContextualType contextualType, JsonSchemaGeneratorSettings settings)
        {
            return GetDescription(contextualType, settings.DefaultReferenceTypeNullHandling, (TSettings)settings);
        }

        bool IReflectionService.IsStringEnum(ContextualType contextualType, JsonSchemaGeneratorSettings settings)
        {
            return IsStringEnum(contextualType, (TSettings)settings);
        }

        string IReflectionService.ConvertEnumValue(object value, JsonSchemaGeneratorSettings settings)
        {
            return ConvertEnumValue(value, (TSettings)settings);
        }

        void IReflectionService.GenerateProperties(JsonSchema schema, Type type, JsonSchemaGeneratorSettings settings, JsonSchemaGenerator schemaGenerator, JsonSchemaResolver schemaResolver)
        {
            GenerateProperties(type, schema, (TSettings)settings, schemaGenerator, schemaResolver);
        }
    }
}