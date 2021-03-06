﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace MSBuild.Sdk.SqlProj.DacpacTool
{
    public static class Extensions
    {
        public static void AddReference(this TSqlModel model, string referencePath, string externalParts)
        {
            var dataSchemaModel = GetDataSchemaModel(model);

            var customData = Activator.CreateInstance(Type.GetType("Microsoft.Data.Tools.Schema.SchemaModel.CustomSchemaData, Microsoft.Data.Tools.Schema.Sql"), "Reference", "SqlSchema");
            var setMetadataMethod = customData.GetType().GetMethod("SetMetadata", BindingFlags.Public | BindingFlags.Instance);
            setMetadataMethod.Invoke(customData, new object[] { "FileName", referencePath });
            setMetadataMethod.Invoke(customData, new object[] { "LogicalName", Path.GetFileName(referencePath) });
            setMetadataMethod.Invoke(customData, new object[] { "SuppressMissingDependenciesErrors", "False" });

            if (!string.IsNullOrWhiteSpace(externalParts))
            {
                setMetadataMethod.Invoke(customData, new object[] { "ExternalParts", Identifier.EncodeIdentifier(externalParts) });
            }

            AddCustomData(dataSchemaModel, customData);
        }

        public static void AddSqlCmdVariables(this TSqlModel model, string[] variableNames)
        {
            var dataSchemaModel = GetDataSchemaModel(model);

            var customData = Activator.CreateInstance(Type.GetType("Microsoft.Data.Tools.Schema.SchemaModel.CustomSchemaData, Microsoft.Data.Tools.Schema.Sql"), "SqlCmdVariables", "SqlCmdVariable");

            foreach (var variableName in variableNames)
            {
                var setMetadataMethod = customData.GetType().GetMethod("SetMetadata", BindingFlags.Public | BindingFlags.Instance);
                setMetadataMethod.Invoke(customData, new object[] { variableName, string.Empty });
            }

            AddCustomData(dataSchemaModel, customData);
        }

        public static IEnumerable<ModelValidationError> GetModelValidationErrors(this TSqlModel model, IEnumerable<string> ignoreValidationErrrors)
        {
            var service = model.GetType().GetField("_service", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(model);
            var getModelValidationErrorsMethod = service.GetType().GetMethod("GetModelValidationErrors", BindingFlags.NonPublic | BindingFlags.Instance);
            var modelValidationErrors = getModelValidationErrorsMethod.Invoke(service, new object[] { ignoreValidationErrrors }) as IEnumerable<object>;

            var createDacModelErrorMethod = service.GetType().GetMethod("CreateDacModelError", BindingFlags.NonPublic | BindingFlags.Instance);
            var result = new List<ModelValidationError>();
            PropertyInfo documentProperty = null;
            foreach (var modelValidationError in modelValidationErrors)
            {
                if (documentProperty == null)
                {
                    documentProperty = modelValidationError.GetType().GetProperty("Document", BindingFlags.Public | BindingFlags.Instance);
                }

                var dacModelError = createDacModelErrorMethod.Invoke(service, new object[] { modelValidationError }) as DacModelError;
                result.Add(new ModelValidationError(dacModelError, documentProperty.GetValue(modelValidationError) as string));
            }

            return result;
        }

        private static object GetDataSchemaModel(TSqlModel model)
        {
            var service = model.GetType().GetField("_service", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(model);
            var dataSchemaModel = service.GetType().GetProperty("DataSchemaModel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(service);
            return dataSchemaModel;
        }

        private static void AddCustomData(object dataSchemaModel, object customData)
        {
            var addCustomDataMethod = dataSchemaModel.GetType().GetMethod("AddCustomData", BindingFlags.Public | BindingFlags.Instance);
            addCustomDataMethod.Invoke(dataSchemaModel, new object[] { customData });
        }
    }
}
