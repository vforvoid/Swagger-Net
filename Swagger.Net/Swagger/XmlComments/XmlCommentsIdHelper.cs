﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Swagger.Net.XmlComments
{
    public static class XmlCommentsIdHelper
    {
        public static string GetCommentIdForMethod(this MethodInfo methodInfo)
        {
            var builder = new StringBuilder("M:");
            AppendFullTypeName(methodInfo.DeclaringType, builder);
            builder.Append(".");
            AppendMethodName(methodInfo, builder);

            return builder.ToString();
        }

        public static string GetCommentId(this Type type)
        {
            var builder = new StringBuilder("T:");
            AppendFullTypeName(type, builder, expandGenericArgs: false);

            return builder.ToString();
        }

        public static string GetCommentId(this PropertyInfo propertyInfo)
        {
            var builder = new StringBuilder("P:");
            AppendFullTypeName(propertyInfo.DeclaringType, builder);
            builder.Append(".");
            builder.Append(propertyInfo.Name);
            return builder.ToString();
        }

        public static string GetCommentId(this FieldInfo fieldInfo)
        {
            var builder = new StringBuilder("F:");
            AppendFullTypeName(fieldInfo.DeclaringType, builder);
            builder.Append(".");
            builder.Append(fieldInfo.Name);
            return builder.ToString();
        }

        private static void AppendFullTypeName(Type type, StringBuilder builder, bool expandGenericArgs = false)
        {
            if (type.Namespace != null)
            {
                builder.Append(type.Namespace);
                builder.Append(".");
            }
            AppendTypeName(type, builder, expandGenericArgs);
        }

        private static void AppendTypeName(Type type, StringBuilder builder, bool expandGenericArgs)
        {
            if (type.IsNested)
            {
                AppendTypeName(type.DeclaringType, builder, false);
                builder.Append(".");
            }

            builder.Append(type.Name);

            if (expandGenericArgs)
                ExpandGenericArgsIfAny(type, builder);
        }

        public static void ExpandGenericArgsIfAny(Type type, StringBuilder builder)
        {
            if (type.IsGenericType)
            {
                string full = builder.ToString();
                int argPos = full.IndexOf('(');
                if (argPos > 0 || type.IsEnum)
                {
                    argPos = Math.Max(argPos, 0);
                    var genericArgsBuilder = new StringBuilder("{");

                    var genericArgs = type.GetGenericArguments();
                    for (int i = 0; i < genericArgs.Length; i++)
                    {
                        if (type.IsEnum || (type.IsClass && genericArgs[i].FullName == null))
                            genericArgsBuilder.Append($"`{i}");
                        else
                            AppendFullTypeName(genericArgs[i], genericArgsBuilder, true);
                        genericArgsBuilder.Append(",");
                    }
                    genericArgsBuilder.Replace(",", "}", genericArgsBuilder.Length - 1, 1);

                    builder.Clear();
                    builder.Append(full.Substring(0, argPos));
                    string newValue = genericArgsBuilder.ToString();
                    string oldValue = string.Format("`{0}", genericArgs.Length);
                    builder.Append(full.Substring(argPos).Replace(oldValue, newValue));
                }
            }
            else if (type.IsArray)
                ExpandGenericArgsIfAny(type.GetElementType(), builder);
        }

        private static void AppendMethodName(MethodInfo methodInfo, StringBuilder builder)
        {
            builder.Append(methodInfo.Name);
            var declaringType = methodInfo.DeclaringType;
            if (declaringType.IsGenericType)
            {
                methodInfo = declaringType.GetGenericTypeDefinition().GetMethod(methodInfo.Name);
            }

            var parameters = methodInfo.GetParameters();
            if (parameters.Length == 0) return;

            builder.Append("(");
            var paramPos = GetTypeParameterPositions(declaringType);
            foreach (var param in parameters)
            {
                if (param.ParameterType.IsGenericParameter)
                {
                    builder.Append($"`{paramPos[param.ParameterType.Name]}");
                }
                else
                {
                    AppendFullTypeName(param.ParameterType, builder, true);
                }
                builder.Append(",");
            }
            builder.Replace(",", ")", builder.Length - 1, 1);
        }

        private static IDictionary<string, int> GetTypeParameterPositions(Type type)
        {
            var result = new Dictionary<string, int>();
            if (!type.IsGenericType)
                return result;
            var genericTypeDefinition = type.GetGenericTypeDefinition();
            foreach (var genericArgument in genericTypeDefinition.GetGenericArguments())
                result.Add(genericArgument.Name, genericArgument.GenericParameterPosition);
            return result;
        }
    }
}
