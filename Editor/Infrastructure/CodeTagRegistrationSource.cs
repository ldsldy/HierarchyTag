using System;
using System.Collections.Generic;
using System.Reflection;
using HierarchyTags.Application;
using HierarchyTags.Contracts;
using UnityEditor;

namespace HierarchyTags.Editor.Infrastructure
{
    /// <summary>
    /// 코드에 선언된 태그를 검색하고 등록 정보로 변환합니다.
    /// </summary>
    internal sealed class CodeTagRegistrationSource : ITagRegistrationSource
    {
        public IReadOnlyList<TagRegistration> Collect()
        {
            var definitionTypes = new List<Type>(TypeCache.GetTypesWithAttribute<HierarchyTagDefinitionsAttribute>());

            definitionTypes.Sort((left, right) =>
                string.CompareOrdinal(left.AssemblyQualifiedName, right.AssemblyQualifiedName));

            var registrations = new List<TagRegistration>();

            foreach (Type type in definitionTypes)
            {
                ValidateDefinitionType(type);

                string assemblyName = type.Assembly.GetName().Name;

                var source = new TagSourceId(TagSourceKind.Code, assemblyName);

                FieldInfo[] fields = type.GetFields(
                    BindingFlags.Public |
                    BindingFlags.Static |
                    BindingFlags.DeclaredOnly);

                Array.Sort(fields, (left, right) =>
                    string.CompareOrdinal(left.Name, right.Name));

                foreach (FieldInfo field in fields)
                {
                    if (field.FieldType != typeof(HierarchyTag))
                    {
                        continue;
                    }

                    string location = $"{assemblyName}::{type.FullName}.{field.Name}";

                    if (!field.IsInitOnly)
                    {
                        throw new InvalidOperationException(
                            $"{location}: 태그 필드는 " +
                            "public static readonly HierarchyTag여야 합니다.");
                    }

                    TagId tag = ReadTag(field, location);

                    registrations.Add(new TagRegistration(tag, source));
                }
            }

            return registrations.AsReadOnly();
        }

        private static void ValidateDefinitionType(Type type)
        {
            bool isStaticClass =
                type.IsClass &&
                type.IsAbstract &&
                type.IsSealed;

            if (!isStaticClass || type.ContainsGenericParameters)
            {
                throw new InvalidOperationException(
                    $"{type.FullName}: 태그 선언 클래스는 " +
                    "제네릭이 아닌 static 클래스여야 합니다.");
            }
        }

        private static TagId ReadTag(FieldInfo field, string location)
        {
            HierarchyTag value;

            try
            {
                value = (HierarchyTag)field.GetValue(null);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"{location}: 태그 초기화 중 오류가 발생했습니다.",
                    exception);
            }

            if (!TagId.TryParse(value.Value, out TagId tag))
            {
                throw new InvalidOperationException(
                    $"{location}: 비어 있거나 유효하지 않은 태그입니다.");
            }

            return tag;
        }
    }
}
