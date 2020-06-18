﻿using Il2Cpp_Modding_Codegen.Serialization;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Il2Cpp_Modding_Codegen.Data.DllHandling
{
    public class DllTypeRef : TypeRef
    {
        internal TypeReference This;
        private readonly string _namespace;
        public override string Namespace { get => _namespace; }
        private readonly string _name;
        public override string Name { get => _name; }

        private bool _isGenericInstance;
        public override bool IsGenericInstance { get => _isGenericInstance; }
        private bool _isGenericTemplate;
        public override bool IsGenericTemplate { get => _isGenericTemplate; }
        private List<TypeRef> _generics = new List<TypeRef>();
        public override IReadOnlyList<TypeRef> Generics { get => _generics; }

        public override TypeRef DeclaringType { get => From(This.DeclaringType); }

        public override TypeRef ElementType
        {
            get
            {
                if (!(This is TypeSpecification typeSpec)) return null;
                if (typeSpec.MetadataType == MetadataType.GenericInstance) return null;
                return From(typeSpec.ElementType);
            }
        }

        public override bool IsVoid() => This.MetadataType == MetadataType.Void;

        public override bool IsPointer() => This.IsPointer;

        public override bool IsPrimitive() => This.IsPrimitive || IsArray() || This.MetadataType == MetadataType.String;

        public override bool IsArray() => This.IsArray;

        private static readonly Dictionary<TypeReference, DllTypeRef> cache = new Dictionary<TypeReference, DllTypeRef>();

        public static int hits = 0;
        public static int misses = 0;

        // Should use DllTypeRef.From instead!
        private DllTypeRef(TypeReference reference)
        {
            This = reference;

            if (This.IsByReference)
            {
                // TODO: Set as ByReference? For method params, the ref keyword is handled by Parameter.cs
                This = (This as ByReferenceType).ElementType;
            }
            _name = This.Name;

            _isGenericTemplate = This.HasGenericParameters;
            _isGenericInstance = This.IsGenericInstance;

            if (IsGenericInstance)
                _generics.AddRange((This as GenericInstanceType).GenericArguments.Select(From));
            else if (IsGenericTemplate)
                _generics.AddRange(This.GenericParameters.Select(From));
            if (IsGeneric && Generics.Count == 0)
                throw new InvalidDataException($"Wtf? In DllTypeRef constructor, a generic with no generics: {this}, IsGenInst: {this.IsGenericInstance}");

            DllTypeRef refDeclaring = null;
            if (!This.IsGenericParameter && This.IsNested)
                refDeclaring = From(This.DeclaringType);

            //if (refDeclaring != null)
            //    _name = refDeclaring.Name + "/" + _name;

            // Remove *, [] from end of variable name
            _name = Regex.Replace(_name, @"\W+$", "");
            if (!char.IsLetterOrDigit(_name.Last())) Console.WriteLine(reference);

            _namespace = (refDeclaring?.Namespace ?? This.Namespace) ?? "";
        }

        public static DllTypeRef From(TypeReference type)
        {
            if (type is null) return null;
            if (cache.TryGetValue(type, out var value))
            {
                hits++;
                return value;
            }
            misses++;

            // Creates new TypeRef and add it to map
            value = new DllTypeRef(type);
            cache.Add(type, value);
            return value;
        }

        // For better comments
        public override string ToString()
        {
            return This.ToString();
        }
    }
}