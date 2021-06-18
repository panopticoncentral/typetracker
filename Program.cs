using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Newtonsoft.Json;

namespace Typetracker
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var namespaces = File.Exists(args[1])
                ? JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Type>>>(File.ReadAllText(args[1]))
                : new Dictionary<string, Dictionary<string, Type>>();
            var peReader = new PEReader(new FileStream(args[0], FileMode.Open));
            var metadataReader = peReader.GetMetadataReader();
            var newNamespaces = metadataReader.TypeDefinitions
                .Select(metadataReader.GetTypeDefinition)
                .GroupBy(typeDefinition => metadataReader.GetString(typeDefinition.Namespace))
                .ToDictionary(g => g.Key,
                    g => g.Select(typeDefinition => metadataReader.GetString(typeDefinition.Name)).ToList());

            foreach (var kvp in newNamespaces.OrderBy(k => k.Key))
            {
                if (!namespaces.TryGetValue(kvp.Key, out var types))
                {
                    types = new Dictionary<string, Type>();
                    namespaces[kvp.Key] = types;
                }

                foreach (var typeName in kvp.Value.OrderBy(k => k))
                {
                    if (typeName.Contains("<"))
                    {
                        continue;
                    }

                    if (!types.TryGetValue(typeName, out var type))
                    {
                        Console.WriteLine($"Found new type '{kvp.Key}.{typeName}'.");
                        type = new Type("unreviewed");
                        types[typeName] = type;
                    }
                }
            }

            var json = JsonConvert.SerializeObject(namespaces, Formatting.Indented);

            File.WriteAllText(args[1], json);
        }

        private sealed class Type
        {
            public string Status { get; }

            public Type(string status)
            {
                Status = status;
            }
        }
    }
}
