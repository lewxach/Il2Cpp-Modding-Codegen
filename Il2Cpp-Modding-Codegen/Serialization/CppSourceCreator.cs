﻿using Il2Cpp_Modding_Codegen.Config;
using Il2Cpp_Modding_Codegen.Data;
using Il2Cpp_Modding_Codegen.Serialization.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Il2Cpp_Modding_Codegen.Serialization
{
    public class CppSourceCreator
    {
        private SerializationConfig _config;
        private CppSerializerContext _context;

        public CppSourceCreator(SerializationConfig config, CppSerializerContext context)
        {
            _config = config;
            _context = context;
        }

        public void Serialize(ISerializer<ITypeData> serializer, ITypeData data)
        {
            if (data.Type == TypeEnum.Interface || data.Methods.Count == 0 || data.This.Generic)
            {
                // Don't create C++ for types with no methods, or if it is an interface, or if it is generic
                return;
            }

            var headerLocation = _context.FileName + ".hpp";
            var sourceLocation = Path.Combine(_config.OutputDirectory, _config.OutputSourceDirectory, _context.FileName) + ".cpp";
            Directory.CreateDirectory(Path.GetDirectoryName(sourceLocation));
            using (var ms = new MemoryStream())
            {
                var writer = new StreamWriter(ms);
                // Write header
                writer.WriteLine($"// Autogenerated from {nameof(CppSourceCreator)} on {DateTime.Now}");
                writer.WriteLine($"// Created by Sc2ad");
                writer.WriteLine("// =========================================================================");
                // Write includes
                writer.WriteLine("// Includes");
                writer.WriteLine($"#include \"{headerLocation}\"");
                writer.WriteLine("#include \"utils/il2cpp-utils.hpp\"");
                writer.WriteLine("#include \"utils/utils.h\"");
                writer.WriteLine("#include <optional>");
                foreach (var include in _context.Includes)
                {
                    writer.WriteLine($"#include \"{include}\"");
                }
                writer.WriteLine("// End Includes");
                // Write forward declarations TODO: May not be necessary, or may even be incorrect for C++ files
                writer.WriteLine("// Forward declarations");
                foreach (var fd in _context.ForwardDeclares)
                {
                    writer.WriteLine($"typedef struct {fd} {fd};");
                }
                writer.WriteLine("// End Forward declarations");
                writer.Flush();
                // Write actual type
                try
                {
                    serializer.Serialize(writer.BaseStream, data);
                }
                catch (UnresolvedTypeException e)
                {
                    writer.WriteLine("// Unresolved type exception!");
                    writer.WriteLine("/*");
                    writer.WriteLine(e);
                    writer.WriteLine("*/");
                }
                writer.Flush();
                using (var fs = File.OpenWrite(sourceLocation))
                {
                    writer.BaseStream.Position = 0;
                    writer.BaseStream.CopyTo(fs);
                }
            }
        }
    }
}