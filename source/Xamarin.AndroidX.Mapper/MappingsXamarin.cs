﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Core.Text;
using Mono.Cecil;
using Xamarin.AndroidX.Data;

namespace Xamarin.AndroidX.Mapper
{
    public class MappingsXamarin
    {
        protected static HttpClient client = null;

        static MappingsXamarin()
        {
            client = new HttpClient();

            return;
        }

        public MappingsXamarin()
        {
            client = new HttpClient();

            return;
        }

        public GoogleMappingData GoogleMappingsData
        {
            get;
            set;
        }


        string filename = null;

        public void Download(string name, string url)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException($"Argument needed {nameof(name)}");
            }

            if (string.IsNullOrEmpty(url) && string.IsNullOrEmpty(AssemblyUrl))
            {
                throw new InvalidOperationException($"Argument needed {nameof(url)} or {nameof(AssemblyUrl)}");
            }

            if (string.IsNullOrEmpty(url))
            {
                url = AssemblyUrl;
            }

            Stream result = null;

            using (HttpResponseMessage response = client.GetAsync(url).Result)
            using (HttpContent content = response.Content)
            {
                // ... Read the string.
                result = content.ReadAsStreamAsync().Result;

                filename = $"{name}.dll";
                if (File.Exists(name))
                {
                    File.Delete(name);
                }
                FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);

                result
                        .CopyToAsync(fs)
                        .ContinueWith
                                (
                                    (task) =>
                                    {
                                        fs.Flush();
                                        fs.Close();
                                        fs = null;
                                    }
                                );
            }

            return;
        }

        public string AssemblyUrl
        {
            get;
            set;
        }

        /// <summary>
        /// whether duplicates are removed (Distinct() called)
        /// </summary>
        public bool IsDataUnique
        {
            get;
            set;
        } = true;

        protected
                (
                    string TypenameFullyQualifiedAndroidSupport,
                    string TypenameFullyQualifiedAndroidX
                )
                    []
                    mapping_sorted_androidx = null;

        protected string[] mapping_sorted_androidx_index = null;

        protected
                (
                    string TypenameFullyQualifiedAndroidSupport,
                    string TypenameFullyQualifiedAndroidX
                )
                    []
                    mapping_sorted_android_support = null;

        protected string[] mapping_sorted_android_support_index = null;

        public void Initialize()
        {
            if (filename.ToLowerInvariant().Contains("androidx"))
            {
                mapping_sorted_androidx = this.GoogleMappingsData
                                                    .Mapping
                                                    .OrderBy(tuple => tuple.TypenameFullyQualifiedAndroidX)
                                                    .ToArray();

                mapping_sorted_androidx_index = mapping_sorted_androidx
                                                    .Select(tuple => tuple.TypenameFullyQualifiedAndroidX)
                                                    .ToArray();
            }
            else
            {
                mapping_sorted_android_support = this.GoogleMappingsData
                                                            .Mapping
                                                            .OrderBy(tuple => tuple.TypenameFullyQualifiedAndroidSupport)
                                                            .ToArray();

                mapping_sorted_android_support_index = mapping_sorted_android_support
                                                            .Select(tuple => tuple.TypenameFullyQualifiedAndroidSupport)
                                                            .ToArray();
            }

            // Cecilize(); // let user call Cecilize

            //MappingsXamarinManaged = Cecilize().OrderBy(tuple => tuple.JavaType).ToArray();
            //MappingsXamarinManagedIndex = MappingsXamarinManaged.Select(tuple => tuple.JavaType).ToArray();

            return;
        }

        //public void FinalizeMappings()
        //{
        //    MappingsForMigrationMergeJoin =
        //            new List
        //                    <
        //                        (
        //                            string TypenameFullyQualifiedAndroidSupport,
        //                            string TypenameFullyQualifiedAndroidX,
        //                            string TypenameFullyQualifiedXamarin
        //                        )
        //                    >();

        //    if (filename.ToLowerInvariant().Contains("androidx"))
        //    {
        //        FinalizeMappingsAX();
        //    }
        //    else
        //    {
        //        FinalizeMappingsAS();
        //    }

        //    return;
        //}

        //public void FinalizeMappingsAX()
        //{
        //    foreach
        //            (
        //                (
        //                    string JavaType,
        //                    string ManagedClass,
        //                    string ManagedNamespace,
        //                    string JNIPackage,
        //                    string JNIType
        //                )
        //                    row_mxm in this.MappingsXamarinManaged
        //            )
        //    {
        //        int index = System.Array.BinarySearch
        //                                        (
        //                                            mapping_sorted_androidx_index,
        //                                            row_mxm.JavaType
        //                                        );
        //        string found = mapping_sorted_androidx_index[index];

        //    }

        //    return;
        //}

        //public void FinalizeMappingsAS()
        //{
        //    foreach
        //            (
        //                (
        //                    string JavaType,
        //                    string ManagedClass,
        //                    string ManagedNamespace,
        //                    string JNIPackage,
        //                    string JNIType
        //                )
        //                    row_mxm in this.MappingsXamarinManaged
        //            )
        //    {
        //        int index = System.Array.BinarySearch
        //                                        (
        //                                            mapping_sorted_android_support_index,
        //                                            row_mxm.JavaType
        //                                        );

        //        string found = mapping_sorted_android_support_index[index];
        //    }

        //    return;
        //}

        public
            (
                string JavaType,
                string ManagedClass,
                string ManagedNamespace,
                string JNIPackage,
                string JNIType
            )[]
            MappingsXamarinManaged
        {
            get;
            private set;
        }

        public string[] MappingsXamarinManagedIndex
        {
            get;
            private set;
        }

        public
            (
                string JavaType,
                string ManagedClass,
                string ManagedNamespace,
                string JNIPackage,
                string JNIType
            )
                Find(string java_type)
        {
            (
                string JavaType,
                string ManagedClass,
                string ManagedNamespace,
                string JNIPackage,
                string JNIType
            )
                result;

            int index = System.Array.BinarySearch(this.MappingsXamarinManagedIndex, java_type);
            result = MappingsXamarinManaged[index];

            return result;
        }

        public List
                    <
                        (
                            string JavaTypeFullyQualified,
                            string ManagedTypeFullyQualified
                        )
                    >
                        TAR;

        public List
                    <
                        (
                            string JavaTypeFullyQualified,
                            string ManagedTypeFullyQualified
                        )
                    >
                        TARIG;

        public List
                    <
                        (
                            string JavaTypeFullyQualified,
                            string ManagedTypeFullyQualified
                        )
                    >
                        TARNIG;

        public List
                    <
                        (
                            string JavaTypeFullyQualified,
                            string ManagedTypeFullyQualified
                        )
                    >
                        TRAR;
        public List
                    <
                        (
                            string JavaTypeFullyQualified,
                            string ManagedTypeFullyQualified
                        )
                    >
                        TAUR;
        public List
                    <
                        (
                            string JavaTypeFullyQualified,
                            string ManagedTypeFullyQualified
                        )
                    >
                        TNAR;

        public List
                    <
                        (
                            string JavaTypeFullyQualified,
                            string ManagedTypeFullyQualified
                        )
                    >
                        TNARIG;

        public List
                    <
                        (
                            string JavaTypeFullyQualified,
                            string ManagedTypeFullyQualified
                        )
                    >
                        TNARNIG;

        public List
                    <
                        (
                            string JavaTypeFullyQualified,
                            string ManagedTypeFullyQualified
                        )
                    >
                        TR;

         public List
                    <
                        (
                            string TypenameFullyQualifiedAndroidSupport,
                            string TypenameFullyQualifiedAndroidX,
                            string TypenameFullyQualifiedXamarin
                        )
                    >
                        MappingsForMigrationMergeJoin;


        public void Cecilize()
        {
            TAR = new List
                            <
                                (
                                    string JavaTypeFullyQualified,
                                    string ManagedTypeFullyQualified
                                )
                            >();
            TARIG = new List
                            <
                                (
                                    string JavaTypeFullyQualified,
                                    string ManagedTypeFullyQualified
                                )
                            >();
            TARNIG = new List
                            <
                                (
                                    string JavaTypeFullyQualified,
                                    string ManagedTypeFullyQualified
                                )
                            >();
            TAUR = new List
                            <
                                (
                                    string JavaTypeFullyQualified,
                                    string ManagedTypeFullyQualified
                                )
                            >();

            TNAR = new List
                            <
                                (
                                    string JavaTypeFullyQualified,
                                    string ManagedTypeFullyQualified
                                )
                            >();

            TNARIG = new List
                            <
                                (
                                    string JavaTypeFullyQualified,
                                    string ManagedTypeFullyQualified
                                )
                            >();

            TNARNIG = new List
                            <
                                (
                                    string JavaTypeFullyQualified,
                                    string ManagedTypeFullyQualified
                                )
                            >();

            TR = new List
                            <
                                (
                                    string JavaTypeFullyQualified,
                                    string ManagedTypeFullyQualified
                                )
                            >();

            TRAR = new List
                            <
                                (
                                    string JavaTypeFullyQualified,
                                    string ManagedTypeFullyQualified
                                )
                            >();

            MappingsForMigrationMergeJoin = new List
                                                    <
                                                        (
                                                            string TypenameFullyQualifiedAndroidSupport,
                                                            string TypenameFullyQualifiedAndroidX,
                                                            string TypenameFullyQualifiedXamarin
                                                        )
                                                    >();

            if (filename.ToLowerInvariant().Contains("androidx"))
            {
                CecilizeAX();
            }
            else
            {
                CecilizeAS();
            }

            if (IsDataUnique)
            {
                Parallel.Invoke
                    (
                        () => this.TAR = this.TAR.Distinct().ToList(),
                        () => this.TARIG = this.TARIG.Distinct().ToList(),
                        () => this.TARNIG = this.TARNIG.Distinct().ToList(),
                        () => this.TNAR = this.TNAR.Distinct().ToList(),
                        () => this.TNARIG = this.TNARIG.Distinct().ToList(),
                        () => this.TNARNIG = this.TNARNIG.Distinct().ToList(),
                        () => this.TAUR = this.TAUR.Distinct().ToList(),
                        () => this.TRAR = this.TRAR.Distinct().ToList(),
                        () => this.TR = this.TR.Distinct().ToList()
                    );
            }

            return;
        }

        protected void CecilizeAS()
        {
            bool has_symbols_file = false;

            ReaderParameters reader_parameters = new ReaderParameters
            {
                ReadSymbols = has_symbols_file
            };

            AssemblyDefinition assembly_definition = AssemblyDefinition.ReadAssembly(filename, reader_parameters);

            foreach (ModuleDefinition module in assembly_definition.Modules)
            {
                foreach (TypeDefinition type in module.GetTypes())
                {
                    if(type.HasCustomAttributes)
                    {
                        string managed_type = GetTypeName(type);
                        string managed_namespace = GetNamespace(type);
                        string managed_type_fq = $"{managed_namespace}.{managed_type}";

                        foreach (CustomAttribute attr in type.CustomAttributes)
                        {
                            string attribute = attr.AttributeType.FullName;

                            if (attribute.Equals("Android.Runtime.RegisterAttribute"))
                            {
                                string jni_type = attr.ConstructorArguments[0].Value.ToString();
                                string java_type = jni_type.Replace("/", ".");

                                int lastSlash = jni_type.LastIndexOf('/');

                                if (lastSlash < 0 )
                                {
                                    string type_with_nested_type = jni_type;
                                }
                                string jni_class = jni_type.Substring(lastSlash + 1).Replace('$', '.');
                                string jni_package = jni_type.Substring(0, lastSlash).Replace('/', '.');

                                //......................................................................
                                int index;
                                string tas = null;
                                string tax = null;

                                    index = System.Array.BinarySearch
                                                                (
                                                                    mapping_sorted_android_support_index,
                                                                    java_type
                                                                );

                                if ( index >=0 && index < mapping_sorted_android_support_index.Count() -1 )
                                {
                                    // found
                                    TARIG.Add
                                            (
                                                (
                                                    JavaTypeFullyQualified: java_type,
                                                    ManagedTypeFullyQualified: managed_type_fq
                                                )
                                            );

                                    tas = mapping_sorted_android_support[index].TypenameFullyQualifiedAndroidSupport;
                                    tax = mapping_sorted_android_support[index].TypenameFullyQualifiedAndroidX;

                                    MappingsForMigrationMergeJoin.Add
                                                                    (
                                                                        (
                                                                            TypenameFullyQualifiedAndroidSupport: tas,
                                                                            TypenameFullyQualifiedAndroidX: tax,
                                                                            TypenameFullyQualifiedXamarin: managed_type_fq
                                                                        )
                                                                    );
                                }
                                else
                                {
                                    // not found
                                    TARNIG.Add
                                            (
                                                (
                                                    JavaTypeFullyQualified: java_type,
                                                    ManagedTypeFullyQualified: managed_type_fq
                                                )
                                            );
                                }
                                //......................................................................

                                TAR.Add
                                        (
                                            (
                                                JavaTypeFullyQualified: java_type,
                                                ManagedTypeFullyQualified: managed_type_fq
                                                //ManagedNamespace: managed_namespace,
                                                //JNIPackage: jni_package,
                                                //JNIType: jni_type
                                            )
                                        );



                            }
                        }
                    }

                    if (type.HasNestedTypes)
                    {
                        foreach(TypeDefinition type_nested in type.NestedTypes)
                        {
                            if (type_nested.HasCustomAttributes)
                            {
                                string managed_type = GetTypeName(type);
                                string managed_namespace = GetNamespace(type);
                                string managed_type_fq = $"{managed_namespace}.{managed_type}";

                                foreach (CustomAttribute attr in type.CustomAttributes)
                                {
                                    string attribute = attr.AttributeType.FullName;

                                    if (attribute.Equals("Android.Runtime.RegisterAttribute"))
                                    {
                                        string jni_type = attr.ConstructorArguments[0].Value.ToString();
                                        string java_type = jni_type.Replace("/", ".");

                                        int lastSlash = jni_type.LastIndexOf('/');

                                        if (lastSlash < 0 )
                                        {
                                            string type_with_nested_type = jni_type;
                                        }
                                        string jni_class = jni_type.Substring(lastSlash + 1).Replace('$', '.');
                                        string jni_package = jni_type.Substring(0, lastSlash).Replace('/', '.');


                                        //......................................................................
                                        int index;
                                        string tas = null;
                                        string tax = null;

                                        index = System.Array.BinarySearch
                                                                    (
                                                                        mapping_sorted_android_support_index,
                                                                        java_type
                                                                    );

                                        if ( index >= 0 && index < mapping_sorted_android_support_index.Count())
                                        {
                                            // found
                                            TNARIG.Add
                                                    (
                                                        (
                                                            JavaTypeFullyQualified: java_type,
                                                            ManagedTypeFullyQualified: managed_type_fq
                                                        )
                                                    );
                                            tas = mapping_sorted_android_support[index].TypenameFullyQualifiedAndroidSupport;
                                            tax = mapping_sorted_android_support[index].TypenameFullyQualifiedAndroidX;

                                            MappingsForMigrationMergeJoin.Add
                                                                            (
                                                                                (
                                                                                    TypenameFullyQualifiedAndroidSupport: tas,
                                                                                    TypenameFullyQualifiedAndroidX: tax,
                                                                                    TypenameFullyQualifiedXamarin: managed_type_fq
                                                                                )
                                                                            );
                                        }
                                        else
                                        {
                                            // not found
                                            TNARNIG.Add
                                                    (
                                                        (
                                                            JavaTypeFullyQualified: java_type,
                                                            ManagedTypeFullyQualified: managed_type_fq
                                                        )
                                                    );

                                        }
                                        //......................................................................

                                        TNAR.Add
                                                (
                                                    (
                                                        JavaTypeFullyQualified: java_type,
                                                        ManagedTypeFullyQualified: managed_type_fq
                                                        //ManagedNamespace: managed_namespace,
                                                        //JNIPackage: jni_package,
                                                        //JNIType: jni_type
                                                    )
                                                );

                                    }
                                }
                            }
                        }
                    }
                }

                foreach (TypeReference type in module.GetTypeReferences())
                {
                    string managed_type = type.FullName;
                    string java_type = "jt";

                    TR.Add
                                        (
                                            (
                                                JavaTypeFullyQualified: java_type,
                                                ManagedTypeFullyQualified: managed_type
                                            )
                                        );
                }
            }

            AnalysisData = new Dictionary<string, int>()
            {
                { "$GoogleMappings$", this.GoogleMappingsData.Mapping.Count() },
                { "TAR", TAR.Count },
                { "TARIG", TARIG.Count },
                { "TARNIG", TARNIG.Count },
                { "TNAR", TNAR.Count },
                { "TNAUR", -1 },
                { "TR", TRAR.Count },
             };


            return;
        }

        protected void CecilizeAX()
        {
            bool has_symbols_file = false;

            ReaderParameters reader_parameters = new ReaderParameters
            {
                ReadSymbols = has_symbols_file
            };

            AssemblyDefinition assembly_definition = AssemblyDefinition.ReadAssembly(filename, reader_parameters);

            foreach (ModuleDefinition module in assembly_definition.Modules)
            {
                foreach (TypeDefinition type in module.GetTypes())
                {
                    if(type.HasCustomAttributes)
                    {
                        string managed_type = GetTypeName(type);
                        string managed_namespace = GetNamespace(type);
                        string managed_type_fq = $"{managed_namespace}.{managed_type}";

                        foreach (CustomAttribute attr in type.CustomAttributes)
                        {
                            string attribute = attr.AttributeType.FullName;

                            if (attribute.Equals("Android.Runtime.RegisterAttribute"))
                            {
                                string jni_type = attr.ConstructorArguments[0].Value.ToString();
                                string java_type = jni_type.Replace("/", ".");

                                int lastSlash = jni_type.LastIndexOf('/');

                                if (lastSlash < 0 )
                                {
                                    string type_with_nested_type = jni_type;
                                }
                                string jni_class = jni_type.Substring(lastSlash + 1).Replace('$', '.');
                                string jni_package = jni_type.Substring(0, lastSlash).Replace('/', '.');

                                //......................................................................
                                int index;
                                string tas = null;
                                string tax = null;

                                index = System.Array.BinarySearch
                                                            (
                                                                mapping_sorted_androidx_index,
                                                                java_type
                                                            );

                                if ( index >= 0 && index < mapping_sorted_androidx_index.Count())
                                {
                                    // found
                                    TARIG.Add
                                            (
                                                (
                                                    JavaTypeFullyQualified: java_type,
                                                    ManagedTypeFullyQualified: managed_type_fq
                                                )
                                            );
                                    tas = mapping_sorted_androidx[index].TypenameFullyQualifiedAndroidSupport;
                                    tax = mapping_sorted_androidx[index].TypenameFullyQualifiedAndroidX;

                                    MappingsForMigrationMergeJoin.Add
                                                                    (
                                                                        (
                                                                            TypenameFullyQualifiedAndroidSupport: tas,
                                                                            TypenameFullyQualifiedAndroidX: tax,
                                                                            TypenameFullyQualifiedXamarin: managed_type_fq
                                                                        )
                                                                    );
                                }
                                else
                                {
                                    // not found
                                    TARNIG.Add
                                            (
                                                (
                                                    JavaTypeFullyQualified: java_type,
                                                    ManagedTypeFullyQualified: managed_type_fq
                                                )
                                            );

                                }
                                //......................................................................

                                TAR.Add
                                        (
                                            (
                                                JavaTypeFullyQualified: java_type,
                                                ManagedTypeFullyQualified: managed_type_fq
                                                //ManagedNamespace: managed_namespace,
                                                //JNIPackage: jni_package,
                                                //JNIType: jni_type
                                            )
                                        );



                            }
                        }
                    }

                    if (type.HasNestedTypes)
                    {
                        foreach(TypeDefinition type_nested in type.NestedTypes)
                        {
                            if (type_nested.HasCustomAttributes)
                            {
                                string managed_type = GetTypeName(type);
                                string managed_namespace = GetNamespace(type);
                                string managed_type_fq = $"{managed_namespace}.{managed_type}";

                                foreach (CustomAttribute attr in type.CustomAttributes)
                                {
                                    string attribute = attr.AttributeType.FullName;

                                    if (attribute.Equals("Android.Runtime.RegisterAttribute"))
                                    {
                                        string jni_type = attr.ConstructorArguments[0].Value.ToString();
                                        string java_type = jni_type.Replace("/", ".");

                                        int lastSlash = jni_type.LastIndexOf('/');

                                        if (lastSlash < 0 )
                                        {
                                            string type_with_nested_type = jni_type;
                                        }
                                        string jni_class = jni_type.Substring(lastSlash + 1).Replace('$', '.');
                                        string jni_package = jni_type.Substring(0, lastSlash).Replace('/', '.');

                                        //......................................................................
                                        int index;
                                        string tas = null;
                                        string tax = null;

                                        index = System.Array.BinarySearch
                                                                    (
                                                                        mapping_sorted_androidx_index,
                                                                        java_type
                                                                    );

                                        if ( index >= 0 && index < mapping_sorted_androidx_index.Count())
                                        {
                                            // found
                                            TNARIG.Add
                                                    (
                                                        (
                                                            JavaTypeFullyQualified: java_type,
                                                            ManagedTypeFullyQualified: managed_type_fq
                                                        )
                                                    );
                                            tas = mapping_sorted_androidx[index].TypenameFullyQualifiedAndroidSupport;
                                            tax = mapping_sorted_androidx[index].TypenameFullyQualifiedAndroidX;

                                            MappingsForMigrationMergeJoin.Add
                                                                            (
                                                                                (
                                                                                    TypenameFullyQualifiedAndroidSupport: tas,
                                                                                    TypenameFullyQualifiedAndroidX: tax,
                                                                                    TypenameFullyQualifiedXamarin: managed_type_fq
                                                                                )
                                                                            );
                                        }
                                        else
                                        {
                                            // not found
                                            TNARNIG.Add
                                                    (
                                                        (
                                                            JavaTypeFullyQualified: java_type,
                                                            ManagedTypeFullyQualified: managed_type_fq
                                                        )
                                                    );

                                        }
                                        //......................................................................

                                        TNAR.Add
                                                (
                                                    (
                                                        JavaTypeFullyQualified: java_type,
                                                        ManagedTypeFullyQualified: managed_type_fq
                                                        //ManagedNamespace: managed_namespace,
                                                        //JNIPackage: jni_package,
                                                        //JNIType: jni_type
                                                    )
                                                );

                                    }
                                }
                            }
                        }
                    }
                }

                foreach (TypeReference type in module.GetTypeReferences())
                {
                    string managed_type = type.FullName;
                    string java_type = "jt";

                    TR.Add
                                        (
                                            (
                                                JavaTypeFullyQualified: java_type,
                                                ManagedTypeFullyQualified: managed_type
                                            )
                                        );
                }
            }

            AnalysisData = new Dictionary<string, int>()
            {
                { "$GoogleMappings$", this.GoogleMappingsData.Mapping.Count() },
                { "TAR", TAR.Count },
                { "TARIG", TARIG.Count },
                { "TARNIG", TARNIG.Count },
                { "TNAR", TNAR.Count },
                { "TNAUR", -1 },
                { "TR", TRAR.Count },
             };


            return;
        }

        public Dictionary<string, int> AnalysisData
        {
            get;
            set;
        }

        private string GetNamespace(TypeDefinition type_definition)
        {
            TypeDefinition td = type_definition;
            string ns = type_definition.Namespace;

            while (string.IsNullOrEmpty(ns))
            {
                if (td.DeclaringType == null)
                {
                    break;
                }
                ns = td.DeclaringType.Namespace;
                td = td.DeclaringType;
            }

            return ns;
        }

        private string GetTypeName(TypeDefinition typeDef)
        {
            TypeDefinition td = typeDef;
            string tn = typeDef.Name;

            while (td.DeclaringType != null)
            {
                tn = td.DeclaringType.Name + "." + tn;
                td = td.DeclaringType;
            }

            return tn;
        }

        public void Dump()
        {
            Parallel.Invoke
                (
                    () =>
                    {
                        Assembly assembly = Assembly.GetExecutingAssembly();
                        string[] resource_names = assembly.GetManifestResourceNames();
                        string file = resource_names
                                        .Single(f => f.EndsWith("assembly-analysis-report.md", StringComparison.InvariantCulture));

                        string report = null;
                        using (Stream stream = assembly.GetManifestResourceStream(file))
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            report = reader.ReadToEnd();
                        }

                        string replacement = null;

                        replacement = this.GoogleMappingsData.Mapping.Count().ToString();
                        report = report.Replace($@"$GoogleMappings$", replacement);

                        replacement = string.Join
                                                (
                                                    Environment.NewLine,
                                                    this.GoogleMappingsData.ReducedWithFiltering
                                                );
                        report = report.Replace($@"$SizeReductionReport$", replacement);

                        int n_google_mappings = GoogleMappingsData.Mapping.Count();
                        
                        report = report.Replace("$FILENAME$", filename);
                        report = report.Replace("$GoogleMappings$", n_google_mappings.ToString());
                        report = report.Replace("$NTAR$", this.TAR.Count().ToString());
                        report = report.Replace("$NTARIG$", this.TARIG.Count().ToString());
                        report = report.Replace("$NTARNIG$", this.TARNIG.Count().ToString());
                        int sum_tar = this.TARIG.Count() + this.TARNIG.Count();
                        report = report.Replace("$SUM_TAR$", sum_tar.ToString());
                        report = report.Replace("$NTNAR$", this.TNAR.Count().ToString());
                        report = report.Replace("$NTNARIG$", this.TNARIG.Count().ToString());
                        report = report.Replace("$NTNARNIG$", this.TNARNIG.Count().ToString());
                        int sum_tnar = this.TNARIG.Count() + this.TNARNIG.Count();
                        report = report.Replace("$SUM_TNAR$", sum_tnar.ToString());
                        report = report.Replace("$NTAUR$", this.TAUR.Count().ToString());
                        report = report.Replace("$NTR$", this.TR.Count().ToString());

                        report = report.Replace
                                            (
                                                "$N_MappingsForMigrationMergeJoin$",
                                                this.MappingsForMigrationMergeJoin.Count().ToString()
                                            );

                        File.WriteAllText(Path.ChangeExtension(filename, "assembly-analysis-report.md"), report);
                    },
                    () =>
                    {
                        string text = string.Join(Environment.NewLine, this.TAR);
                        text = text.Replace("(", "");
                        text = text.Replace(")", "");
                        text = text.Replace(" ", "");
                        File.WriteAllText(Path.ChangeExtension(filename, "dll.TAR.csv"), text);
                    },
                    () =>
                    {
                        string text = string.Join(Environment.NewLine, this.TARIG);
                        text = text.Replace("(", "");
                        text = text.Replace(")", "");
                        text = text.Replace(" ", "");
                        File.WriteAllText(Path.ChangeExtension(filename, "dll.TARIG.csv"), text);
                    },
                    () =>
                    {
                        string text = string.Join(Environment.NewLine, this.TARNIG);
                        text = text.Replace("(", "");
                        text = text.Replace(")", "");
                        text = text.Replace(" ", "");
                        File.WriteAllText(Path.ChangeExtension(filename, "dll.TARNIG.csv"), text);
                    },
                    () =>
                    {
                        string text = string.Join(Environment.NewLine, this.TAUR);
                        text = text.Replace("(", "");
                        text = text.Replace(")", "");
                        text = text.Replace(" ", "");
                        File.WriteAllText(Path.ChangeExtension(filename, "TAUR.csv"), text);
                    },
                    () =>
                    {
                        string text = string.Join(Environment.NewLine, this.TNAR);
                        text = text.Replace("(", "");
                        text = text.Replace(")", "");
                        text = text.Replace(" ", "");
                        File.WriteAllText(Path.ChangeExtension(filename, "dll.TNAR.csv"), text);
                    },
                    () =>
                    {
                        string text = string.Join(Environment.NewLine, this.TNARIG);
                        text = text.Replace("(", "");
                        text = text.Replace(")", "");
                        text = text.Replace(" ", "");
                        File.WriteAllText(Path.ChangeExtension(filename, "dll.TNARIG.csv"), text);
                    },
                    () =>
                    {
                        string text = string.Join(Environment.NewLine, this.TNARNIG);
                        text = text.Replace("(", "");
                        text = text.Replace(")", "");
                        text = text.Replace(" ", "");
                        File.WriteAllText(Path.ChangeExtension(filename, "dll.TNARNIG.csv"), text);
                    },
                    () =>
                    {
                        string text = string.Join(Environment.NewLine, this.TR);
                        text = text.Replace("(", "");
                        text = text.Replace(")", "");
                        text = text.Replace(" ", "");
                        File.WriteAllText(Path.ChangeExtension(filename, "dll.TR.csv"), text);
                    },
                    () =>
                    {
                        string text = string.Join(Environment.NewLine, this.TRAR);
                        text = text.Replace("(", "");
                        text = text.Replace(")", "");
                        text = text.Replace(" ", "");
                        File.WriteAllText(Path.ChangeExtension(filename, "dll.TRAR.csv"), text);
                    },
                    () =>
                    {
                        string text = string.Join(Environment.NewLine, this.MappingsForMigrationMergeJoin);
                        text = text.Replace("(", "");
                        text = text.Replace(")", "");
                        text = text.Replace(" ", "");
                        File.WriteAllText(Path.ChangeExtension(filename, "dll.MappingsForMigrationMergeJoin.csv"), text);
                    }
                );

            return;
        }

    }
}