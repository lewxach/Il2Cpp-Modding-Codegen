﻿using Il2Cpp_Modding_Codegen.Config;
using Il2Cpp_Modding_Codegen.Data;
using Il2Cpp_Modding_Codegen.Serialization.Interfaces;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;

namespace Il2Cpp_Modding_Codegen.Serialization
{
    public class CppStaticFieldSerializer : ISerializer<IField>
    {
        private string _declaringFullyQualified;
        private Dictionary<IField, string> _resolvedTypeNames = new Dictionary<IField, string>();
        private bool _asHeader;
        private SerializationConfig _config;

        public CppStaticFieldSerializer(bool asHeader, SerializationConfig config)
        {
            _asHeader = asHeader;
            _config = config;
        }

        public void PreSerialize(ISerializerContext context, IField field)
        {
            _resolvedTypeNames.Add(field, context.GetNameFromReference(field.Type));
            _declaringFullyQualified = context.QualifiedTypeName;
        }

        private string GetGetter(string fieldTypeName, IField field, bool namespaceQualified)
        {
            var retStr = fieldTypeName;
            var ns = "";
            if (_config.OutputStyle == OutputStyle.Normal)
                retStr = "std::optional<" + retStr + ">";
            if (namespaceQualified)
                ns = _declaringFullyQualified + "::";
            // Collisions with this name are incredibly unlikely.
            return $"{retStr} {ns}_get_{field.Name}()";
        }

        private string GetSetter(string fieldTypeName, IField field, bool namespaceQualified)
        {
            var ns = "";
            if (namespaceQualified)
                ns = _declaringFullyQualified + "::";
            return $"void {ns}_set_{field.Name}({fieldTypeName} value)";
        }

        public void Serialize(IndentedTextWriter writer, IField field)
        {
            if (_resolvedTypeNames[field] == null)
                throw new UnresolvedTypeException(field.DeclaringType, field.Type);
            var fieldString = "";
            foreach (var spec in field.Specifiers)
                fieldString += $"{spec} ";
            fieldString += $"{field.Type} {field.Name}";
            var resolvedName = _resolvedTypeNames[field];
            if (_asHeader)
            {
                // Create two method declarations:
                // static FIELDTYPE _get_FIELDNAME();
                // static void _set_FIELDNAME(FIELDTYPE value);
                writer.WriteLine($"// Get static field: {fieldString}");
                writer.WriteLine(GetGetter(resolvedName, field, false) + ";");
                writer.WriteLine($"// Set static field: {fieldString}");
                writer.WriteLine(GetSetter(resolvedName, field, false) + ";");
            }
            else
            {
                // Write getter
                writer.WriteLine("// Autogenerated static field getter");
                writer.WriteLine($"// Get static field: {fieldString}");
                writer.WriteLine(GetGetter(resolvedName, field, true) + " {");
                writer.Indent++;

                var s = "return ";
                var innard = $"<{resolvedName}>";
                var macro = "CRASH_UNLESS(";
                if (_config.OutputStyle != OutputStyle.CrashUnless)
                    macro = "";

                s += $"{macro}il2cpp_utils::GetFieldValue{innard}(";
                s += $"\"{field.DeclaringType.Namespace}\", \"{field.DeclaringType.Name}\", \"{field.Name}\")";
                if (!string.IsNullOrEmpty(macro)) s += ")";
                s += ";";
                writer.WriteLine(s);
                writer.Indent--;
                writer.WriteLine("}");
                // Write setter
                writer.WriteLine("// Autogenerated static field setter");
                writer.WriteLine($"// Set static field: {fieldString}");
                writer.WriteLine(GetSetter(resolvedName, field, true) + " {");
                writer.Indent++;
                s = "";
                if (_config.OutputStyle == OutputStyle.CrashUnless)
                    macro = "CRASH_UNLESS(";
                else
                    macro = "RET_V_UNLESS(";

                s += $"{macro}il2cpp_utils::SetFieldValue(";
                s += $"\"{field.DeclaringType.Namespace}\", \"{field.DeclaringType.Name}\", \"{field.Name}\", value));";
                writer.WriteLine(s);
                writer.Indent--;
                writer.WriteLine("}");
            }
            writer.Flush();
        }
    }
}