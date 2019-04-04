using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

using Mono.Cecil;

using HolisticWare.Xamarin.Tools.Bindings.XamarinAndroid.AndroidX.Migraineator.AST;

namespace HolisticWare.Xamarin.Tools.Bindings.XamarinAndroid.AndroidX.Migraineator
{
    public partial class AndroidXMigrator
    {
        private void MigrateWithWithStringsOriginalPatchByRedth(ref long duration)
        {
            string msg = $"{DateTime.Now.ToString("yyyyMMdd-HHmmss")}-redths-patch-androidx-migrated";
            string alg = "redth";

            int idx = this.PathAssemblyOutput.LastIndexOf(Path.DirectorySeparatorChar) + 1;
            string asm = this.PathAssemblyOutput.Substring(idx, this.PathAssemblyOutput.Length - idx);

            if
                (
                    asm.StartsWith("System", StringComparison.InvariantCultureIgnoreCase)
                    ||
                    asm.StartsWith("Microsoft", StringComparison.InvariantCultureIgnoreCase)
                    ||
                    asm.StartsWith("Java.Interop", StringComparison.InvariantCultureIgnoreCase)
                )
            {
                duration = -1;

                return;
            }

            log = new System.Text.StringBuilder();
            timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            string i = Path.ChangeExtension(this.PathAssemblyInput, $"{alg}.dll");
            if (File.Exists(i))
            {
                File.Delete(i);
            }
            File.Copy(this.PathAssemblyInput, i);
            string o = Path.ChangeExtension(this.PathAssemblyOutput, $"{alg}.dll");
            if (File.Exists(o))
            {
                File.Delete(o);
            }
            File.Copy(i, o);

            string i_pdb = Path.ChangeExtension(this.PathAssemblyInput, $"{alg}.pdb");
            string o_pdb = Path.ChangeExtension(this.PathAssemblyOutput, $"{alg}.pdb");

            bool hasPdb = File.Exists(i_pdb);
            if (hasPdb)
            {
                if (File.Exists(o_pdb))
                {
                    File.Delete(o_pdb);
                }
                if (File.Exists(i_pdb))
                {
                    File.Delete(i_pdb);
                }
                File.Copy(i_pdb, o_pdb);
            }

            var readerParams = new ReaderParameters
            {
                ReadSymbols = hasPdb,
            };

            asm_def = Mono.Cecil.AssemblyDefinition.ReadAssembly
                                                        (
                                                            o,
                                                            new Mono.Cecil.ReaderParameters
                                                            {
                                                                AssemblyResolver = CreateAssemblyResolver(),
                                                                ReadWrite = true,
                                                                //InMemory = true,
                                                                ReadSymbols = hasPdb,
                                                            }
                                                        );

            Trace.WriteLine($"===================================================================================");
            Trace.WriteLine($"migrating assembly               = {this.PathAssemblyInput}");

            AST.Assembly ast_assembly = new AST.Assembly()
            {
                Name = asm
            };

            foreach (ModuleDefinition module in asm_def.Modules)
            {
                Trace.WriteLine($"--------------------------------------------------------------------------");
                Trace.WriteLine($"    migrating Module           = {module.Name}");
                //module.AssemblyReferences;

                AST.Module ast_module = ProcessModuleRedth(module);

                if (ast_module != null)
                {
                    if (ast_assembly == null)
                    {
                        ast_assembly = new AST.Assembly()
                        {

                        };
                    }
                    ast_assembly.Modules.Add(ast_module);
                }
            }

            AndroidXMigrator.AbstractSyntaxTree.Assemblies.Add(ast_assembly);
            timer.Stop();

            log.AppendLine($"{timer.ElapsedMilliseconds}ms");
            Trace.WriteLine($"{timer.ElapsedMilliseconds}ms");
            //Trace.WriteLine(log.ToString());


            File.WriteAllText
                (
                    Path.ChangeExtension(this.PathAssemblyInput, $"AbstractSyntaxTree.{msg}.json"),
                    Newtonsoft.Json.JsonConvert.SerializeObject
                    (
                        ast_assembly,
                        Newtonsoft.Json.Formatting.Indented,
                        new Newtonsoft.Json.JsonSerializerSettings()
                        {
                            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                        }
                    )
                );


            System.Diagnostics.Debug.WriteLine(log.ToString());
            System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt"), log.ToString());

            asm_def.Write();

            duration = timer.ElapsedMilliseconds;

            return;
        }

        private Module ProcessModuleRedth(ModuleDefinition module)
        {
            AST.Module ast_module = null;

            foreach (TypeReference type in module.GetTypeReferences())
            {
                AST.Type ast_type = ProcessTypeReferenceRedth(type);

                if (ast_type == null)
                {
                    continue;
                }
                else
                {
                    if (ast_module == null)
                    {
                        ast_module = new AST.Module()
                        {
                            Name = module.Name
                        };
                    }
                    ast_module.TypesReference.Add(ast_type);
                }
            }

            Mono.Collections.Generic.Collection<TypeDefinition> module_types = module.Types;
            IEnumerable<TypeDefinition> module_types_get = module.GetTypes();
            int module_types_count = module_types.Count();
            int module_types_get_count = module_types_get.Count();
            if (module_types_count != module_types_get_count)
            {
                this.Problems.Add("Module API different number of types!");
            }
            foreach (TypeDefinition type in module_types_get)
            {
                AST.Type ast_type = ProcessTypeRedth(type);

                if (ast_type == null)
                {
                    continue;
                }
                else
                {
                    if (ast_module == null)
                    {
                        ast_module = new AST.Module()
                        {
                            Name = module.Name
                        };
                    }
                    ast_module.Types.Add(ast_type);
                }

            }

            return ast_module;
        }

        private AST.Type ProcessTypeRedth(TypeDefinition type)
        {
            AST.Type ast_type = null;

            //------------------------------------------------------------
            if (type.HasCustomAttributes)
            {
                foreach (CustomAttribute attr in type.CustomAttributes)
                {
                    if (attr.AttributeType.FullName.Equals("Android.Runtime.RegisterAttribute"))
                    {
                        int index = 0;
                        CustomAttributeArgument ctor_name = attr.ConstructorArguments[index];

                        string jni_sig = ctor_name.Value?.ToString();
                        string jni_sig_new = ReplaceJniSignatureRedth(jni_sig);

                        if (jni_sig_new != null)
                        {
                            attr.ConstructorArguments[index] = new CustomAttributeArgument(ctor_name.Type, jni_sig_new);
                        }
                    }
                }
            }
            //------------------------------------------------------------

            string type_fqn_old = type.FullName;
            string r = FindReplacingTypeFromMappingsManaged(type.FullName);
            if (string.IsNullOrEmpty(r))
            {
                // it is OK that the type was not found 
                // it cannot be Android.Support... or?
                // add to problems collection!!
                string type_problem = type_fqn_old;
            }
            else
            {
                string tnfq_as = type.FullName;
                int idx = r.LastIndexOf('.');
                string ns = r.Substring(0, idx);
                string tn = r.Substring(idx + 1, r.Length - idx - 1);
                string tnfq = $"{ns}.{tn}";
                type.Namespace = ns;
                type.Scope.Name = ns;

                Trace.WriteLine($"   Migrated");
                Trace.WriteLine($"      Type");
                Trace.WriteLine($"          AS = {tnfq_as}");
                Trace.WriteLine($"          to ");
                Trace.WriteLine($"          AX = {tnfq}");
            }

            AST.Type ast_type_base = ProcessBaseTypeRedth(type.BaseType);

            List<AST.Type> ast_types_nested = null;
            foreach (TypeDefinition type_nested in type.NestedTypes)
            {
                AST.Type ast_type_nested = ProcessNestedTypeRedth(type_nested);

                if (ast_type_nested != null)
                {
                    ast_types_nested = new List<AST.Type>();
                }
                else
                {
                    continue;
                }

                ast_types_nested.Add(ast_type_nested);
            }

            List<AST.Method> ast_methods = null;
            foreach (var method in type.Methods)
            {
                AST.Method ast_method = ProcessMethodRedth(method);

                if (ast_method != null)
                {
                    ast_methods = new List<AST.Method>();
                }
                else
                {
                    continue;
                }

                ast_methods.Add(ast_method);
            }

            if (ast_type_base == null && ast_methods == null)
            {
                return ast_type;
            }

            ast_type = new AST.Type()
            {
                Name = type.Name,
                NameFullyQualified = type.FullName,
            };

            if (ast_type != null)
            {
                ast_type.BaseType = ast_type_base;
            }
            if (ast_methods != null)
            {
                ast_type.Methods = ast_methods;
            }

            return ast_type;
        }

        private AST.Type ProcessTypeReferenceRedth(TypeReference type)
        {
            AST.Type ast_type_base = null;

            if
                (
                    type == null
                    ||
                       (
                            !(type?.FullName).StartsWith("Android.Support.", StringComparison.Ordinal)
                            &&
                            !(type?.FullName).StartsWith("Android.Arch.", StringComparison.Ordinal)
                        )
                )
            {
                return ast_type_base;
            }

            string type_fqn_old = type.FullName;

            string r = FindReplacingTypeFromMappingsManaged(type.FullName);
            if (string.IsNullOrEmpty(r))
            {
                return ast_type_base;
            }
            else
            {
                string tnfq_as = type.FullName;
                int idx = r.LastIndexOf('.');
                string ns = r.Substring(0, idx);
                string tn = r.Substring(idx + 1, r.Length - idx - 1);
                string tnfq = $"{ns}.{tn}";
                type.Namespace = ns;
                type.Scope.Name = ns;

                Trace.WriteLine($"   Migrated");
                Trace.WriteLine($"      Type reference");
                Trace.WriteLine($"          AS = {tnfq_as}");
                Trace.WriteLine($"          to ");
                Trace.WriteLine($"          AX = {tnfq}");
            }


            Console.ForegroundColor = ConsoleColor.DarkRed;
            log.AppendLine($"    BaseType: {type.FullName}");
            Console.ResetColor();

            ast_type_base = new AST.Type()
            {
                Name = type.Name,
                NameFullyQualified = type.FullName,
                NameFullyQualifiedOldMigratred = type_fqn_old
            };

            return ast_type_base;
        }

        private AST.Type ProcessBaseTypeRedth(TypeReference type_base)
        {
            AST.Type ast_type_base = null;

            if
                (
                    type_base == null
                    ||
                    (
                            !(type_base?.FullName).StartsWith("Android.Support.", StringComparison.Ordinal)
                            &&
                            !(type_base?.FullName).StartsWith("Android.Arch.", StringComparison.Ordinal)
                        )
                )
            {
                return ast_type_base;
            }

            Trace.WriteLine($"        processing BaseType - TypeReference");
            Trace.WriteLine($"            Name        = {type_base.Name}");
            Trace.WriteLine($"            FullName    = {type_base.FullName}");

            string type_fqn_old = type_base.FullName;

            string r = FindReplacingTypeFromMappingsManaged(type_base.FullName);
            if (string.IsNullOrEmpty(r))
            {
                return ast_type_base;
            }

            int idx = r.LastIndexOf('.');
            if (idx < 0)
            {
                return ast_type_base;
            }
            else
            {
                string tnfq_as = type_base.FullName;
                string ns = r.Substring(0, idx);
                string tn = r.Substring(idx + 1, r.Length - idx - 1);
                string tnfq = $"{ns}.{tn}";
                type_base.Namespace = ns;
                type_base.Scope.Name = ns;

                Trace.WriteLine($"   Migrated");
                Trace.WriteLine($"      Type base");
                Trace.WriteLine($"          AS = {tnfq_as}");
                Trace.WriteLine($"          to ");
                Trace.WriteLine($"          AX = {tnfq}");
            }


            Console.ForegroundColor = ConsoleColor.DarkRed;
            log.AppendLine($"    BaseType: {type_base.FullName}");
            Console.ResetColor();

            ast_type_base = new AST.Type()
            {
                Name = type_base.Name,
                NameFullyQualified = type_base.FullName,
                NameFullyQualifiedOldMigratred = type_fqn_old
            };

            return ast_type_base;
        }

        private AST.Type ProcessNestedTypeRedth(TypeDefinition type_nested)
        {
            AST.Type ast_type_nested = null;

            if
                (
                    type_nested == null
                    ||
                    (
                            !(type_nested?.FullName).StartsWith("Android.Support.", StringComparison.Ordinal)
                            &&
                            !(type_nested?.FullName).StartsWith("Android.Arch.", StringComparison.Ordinal)
                        )
                    ||
                    type_nested.Name.Contains("<>c")  // anonymous methods, lambdas 
                )
            {
                return ast_type_nested;
            }

            //------------------------------------------------------------
            if (type_nested.HasCustomAttributes)
            {
                foreach (CustomAttribute attr in type_nested.CustomAttributes)
                {
                    if (attr.AttributeType.FullName.Equals("Android.Runtime.RegisterAttribute"))
                    {
                        int index = 0;
                        CustomAttributeArgument ctor_name = attr.ConstructorArguments[index];

                        string jni_sig = ctor_name.Value?.ToString();
                        string jni_sig_new = ReplaceJniSignatureRedth(jni_sig);

                        if (jni_sig_new != null)
                        {
                            attr.ConstructorArguments[index] = new CustomAttributeArgument(ctor_name.Type, jni_sig_new);
                        }
                    }
                }
            }
            //------------------------------------------------------------

            string type_nested_fqn_old = type_nested.FullName;
            string r = FindReplacingTypeFromMappingsManaged(type_nested.FullName);
            if (string.IsNullOrEmpty(r))
            {
                return ast_type_nested;
            }
            else
            {
                string tnfq_as = type_nested.FullName;
                int idx1 = r.LastIndexOf('.');
                int idx2 = r.LastIndexOf('/');
                string ns = r.Substring(0, idx1);
                string tn = r.Substring(idx1 + 1, r.Length - idx1 - 1);
                string tnfq = $"{ns}.{tn}";
                //string tnfq = r.Substring(idx1 + 1, r.Length - idx2 - 1);
                type_nested.Namespace = ns;
                type_nested.Scope.Name = ns;

                Trace.WriteLine($"   Migrated");
                Trace.WriteLine($"      Type nested");
                Trace.WriteLine($"          AS = {tnfq_as}");
                Trace.WriteLine($"          to ");
                Trace.WriteLine($"          AX = {tnfq}");
            }

            ast_type_nested = new AST.Type()
            {
                Name = type_nested.Name,
                NameFullyQualified = type_nested.FullName,
                NameFullyQualifiedOldMigratred = type_nested_fqn_old
            };

            return ast_type_nested;
        }


        private AST.Method ProcessMethodRedth(MethodDefinition method)
        {
            AST.Method ast_method = null;

            Trace.WriteLine($"        processing method");
            Trace.WriteLine($"           Name        = {method.Name}");
            Trace.WriteLine($"           FullName    = {method.ReturnType.FullName}");

            AST.Type ast_method_type_return = ProcessMethodReturnTypeRedth(method.ReturnType);

            //------------------------------------------------------------
            string jni_signature = null;
            if (method.HasCustomAttributes)
            {
                foreach (CustomAttribute attr in method.CustomAttributes)
                {
                    if (attr.AttributeType.FullName.Equals("Android.Runtime.RegisterAttribute"))
                    {
                        // for methods
                        // 0 - methodname
                        // 1 - arguments
                        int index = 1;
                        CustomAttributeArgument ctor_name = attr.ConstructorArguments[index];

                        string jni_sig = ctor_name.Value?.ToString();
                        string jni_sig_new = ReplaceJniSignatureRedth(jni_sig);

                        if (jni_sig_new != null)
                        {
                            attr.ConstructorArguments[index] = new CustomAttributeArgument(ctor_name.Type, jni_sig_new);
                        }
                    }
                }
            }
            //------------------------------------------------------------

            List<AST.Parameter> ast_method_parameters = null;
            foreach (ParameterDefinition method_parameter in method.Parameters)
            {
                AST.Parameter ast_method_parameter = ProcessMethodParameterRedth(method_parameter);

                if (ast_method_parameter != null)
                {
                    if (ast_method_parameters == null)
                    {
                        ast_method_parameters = new List<AST.Parameter>();
                    }
                }
                else
                {
                    continue;
                }
                ast_method_parameters.Add(ast_method_parameter);
            }

            AST.MethodBody ast_method_body = ProcessMethodBodyRedth(method.Body);

            if (ast_method_type_return == null && jni_signature == null && ast_method_body == null && ast_method_parameters == null)
            {
                return ast_method;
            }

            ast_method = new AST.Method();

            if (ast_method_type_return != null)
            {
                ast_method.ReturnType = ast_method_type_return;
            }

            if (ast_method_body != null)
            {
                ast_method.Body = ast_method_body;
            }

            if (ast_method_parameters != null)
            {
                ast_method.Parameters = ast_method_parameters;
            }

            return ast_method;
        }

        private AST.Type ProcessMethodReturnTypeRedth(TypeReference type_return)
        {
            AST.Type ast_type_return = null;

            if
                (
                    type_return == null
                    ||
                    (
                            !(type_return?.FullName).StartsWith("Android.Support.", StringComparison.Ordinal)
                            &&
                            !(type_return?.FullName).StartsWith("Android.Arch.", StringComparison.Ordinal)
                        )
                )
            {
                return ast_type_return;
            }


            string r = FindReplacingTypeFromMappingsManaged(type_return.FullName);
            if (string.IsNullOrEmpty(r))
            {
                return ast_type_return;
            }
            else
            {
                string tnfq_as = type_return.FullName;
                int idx = r.LastIndexOf('.');
                string ns = r.Substring(0, idx);
                string tn = r.Substring(idx + 1, r.Length - idx - 1);
                string tnfq = $"{ns}.{tn}";
                type_return.Namespace = ns;
                type_return.Scope.Name = ns;

                Trace.WriteLine($"   Migrated");
                Trace.WriteLine($"      Type method Return");
                Trace.WriteLine($"          AS = {tnfq_as}");
                Trace.WriteLine($"          to ");
                Trace.WriteLine($"          AX = {tnfq}");
            }
            Console.ForegroundColor = ConsoleColor.DarkRed;
            log.AppendLine($"{type_return.Name} returns {type_return.FullName}");
            Console.ResetColor();

            return ast_type_return;
        }

        private AST.Parameter ProcessMethodParameterRedth(ParameterDefinition method_parameter)
        {
            AST.Parameter ast_method_parameter = null;

            if
                 (
                     method_parameter == null
                     ||
                     !(
                             (method_parameter?.ParameterType.FullName).StartsWith("Android.Support.", StringComparison.Ordinal)
                             ||
                             (method_parameter?.ParameterType.FullName).StartsWith("Android.Arch.", StringComparison.Ordinal)
                         )
                 )
            {
                return ast_method_parameter;
            }

            //------------------------------------------------------------
            if (method_parameter.HasCustomAttributes)
            {
                foreach (CustomAttribute attr in method_parameter.CustomAttributes)
                {
                    if (attr.AttributeType.FullName.Equals("Android.Runtime.RegisterAttribute"))
                    {
                        int index = 0;
                        CustomAttributeArgument ctor_name = attr.ConstructorArguments[index];

                        string jni_sig = ctor_name.Value?.ToString();
                        string jni_sig_new = ReplaceJniSignatureRedth(jni_sig);

                        if (jni_sig_new != null)
                        {
                            attr.ConstructorArguments[index] = new CustomAttributeArgument(ctor_name.Type, jni_sig_new);
                        }
                    }
                }
            }
            //------------------------------------------------------------

            string r = FindReplacingTypeFromMappingsManaged(method_parameter.ParameterType.FullName);
            if (string.IsNullOrEmpty(r))
            {
                return ast_method_parameter;
            }
            else
            {
                string tnfq_as = method_parameter.ParameterType.FullName;
                int idx = r.LastIndexOf('.');
                string ns = r.Substring(0, idx);
                string tn = r.Substring(idx + 1, r.Length - idx - 1);
                string tnfq = $"{ns}.{tn}";

                method_parameter.ParameterType.Namespace = ns;
                method_parameter.ParameterType.Scope.Name = ns;
                Trace.WriteLine($"   Migrated");
                Trace.WriteLine($"      Method Parameter");
                Trace.WriteLine($"          AS = {tnfq_as}");
                Trace.WriteLine($"          to ");
                Trace.WriteLine($"          AX = {tnfq}");
            }

            ast_method_parameter = new AST.Parameter()
            {

            };

            return ast_method_parameter;
        }

        private AST.MethodBody ProcessMethodBodyRedth(Mono.Cecil.Cil.MethodBody method_body)
        {
            AST.MethodBody ast_method_body = null;

            if (method_body == null)
            {
                return ast_method_body;
            }

            // Replace all the JNI Signatures inside the method body
            foreach (Mono.Cecil.Cil.Instruction instr in method_body.Instructions)
            {
                if (instr.OpCode.Name == "ldstr")
                {
                    string jniSig = instr.Operand.ToString();

                    int indexOfDot = jniSig.IndexOf('.');

                    if (indexOfDot < 0)
                    {
                        continue;
                    }

                    // New binding glue style is `methodName.(Lparamater/Type;)Lreturn/Type;`
                    if (indexOfDot >= 0)
                    {
                        string methodName = jniSig.Substring(0, indexOfDot);
                        string newJniSig = ReplaceJniSignatureRedth(jniSig.Substring(indexOfDot + 1));
                        instr.Operand = $"{methodName}.{newJniSig}";

                        log.AppendLine($"{methodName} -> {newJniSig}");
                    }
                    // Old style is two strings, one with method name and then `(Lparameter/Type;)Lreturn/Type;`
                    else if (jniSig.Contains('(') && jniSig.Contains(')'))
                    {
                        string methodName = instr.Previous.Operand.ToString();
                        string newJniSig = ReplaceJniSignatureRedth(jniSig);
                        instr.Operand = newJniSig;

                        log.AppendLine($"{methodName} -> {newJniSig}");
                    }
                    else
                    {
                        this.Problems.Add($"Method Body Code Smell {method_body}");
                    }

                    if (ast_method_body == null)
                    {
                        ast_method_body = new MethodBody();
                    }
                }
            }

            return ast_method_body;
        }


        public string ReplaceJniSignatureRedth(string jniSignature)
        {
            if
                (
                    string.IsNullOrEmpty(jniSignature)
                    ||
                    jniSignature.Equals("()V") // speeding up - no need to parse it
                )
            {
                return jniSignature;
            }

            // TODO: replace this PoC code
            int index_bracket_left = jniSignature.IndexOf('(');
            int index_bracket_right = jniSignature.IndexOf(')');


            string jni_signature_new = jniSignature;

            string parameters = null;

            if ( index_bracket_left >= 0 && index_bracket_right >= 0)
            {
                int length = index_bracket_right - index_bracket_left - 1;
                parameters = jniSignature.Substring(index_bracket_left + 1, length);
            }

            if (! string.IsNullOrEmpty(parameters))
            {
                string[] parameter_list = parameters
                                                .Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries)
                                                ;

                for(int i = 0; i < parameter_list.Length; i++)
                {
                    string parameter_old = parameter_list[i].Replace('/', '.');

                    bool has_prefix = false;

                    if (parameter_old[0] == 'L')
                    {
                        has_prefix = true;
                        parameter_old = parameter_old.Replace("L", "");
                    }

                    string parameter_new = FindReplacingTypeFromMappingsJava(parameter_old);

                    if (!string.IsNullOrEmpty(parameter_new))
                    {
                        parameter_new = parameter_new.Replace('.', '/');
                        if (has_prefix)
                        {
                            parameter_new = $"L{parameter_new}";
                        }
                        jni_signature_new = jni_signature_new.Replace(parameter_list[i], parameter_new);
                    }
                }
            }

            if(index_bracket_right == -1)
            {
                // no parameters - no brackets ()
                index_bracket_right = -2;
            }

            string return_type = jniSignature
                                        .Substring(index_bracket_right + 2)
                                        .Replace(";", "")
                                        .Replace('/', '.')
                                        ;
            string return_type_new = FindReplacingTypeFromMappingsJava(return_type);

            if (!string.IsNullOrEmpty(return_type_new))
            {
                jni_signature_new = jni_signature_new.Replace
                                                        (
                                                            return_type.Replace('.', '/'),
                                                            return_type_new.Replace('.', '/')
                                                        );
            }

            if ( ! jniSignature.Equals(jni_signature_new) )
            {
                MigratedJNI.Add((jniSignature, jni_signature_new));
            }

            return jni_signature_new;

            if
                (
                    //-------------------------------
                    // WTF ??
                    // Use Application Insights without Xamarin Forms
                    // https://github.com/Microsoft/ApplicationInsights-Xamarin/issues/2
                    jniSignature.Contains("Forms.Init(); prior to using it.") // WTF ???
                    ||
                    jniSignature.Contains("Init() before this")
                    ||
                    jniSignature.Contains("Init(); prior to using it.")
                    //-------------------------------
                    // iOS - picked during batch brute force Ceciling
                    ||
                    jniSignature.Contains("FinishedLaunching ()") // Xamarin.Forms.Platform.iOS.migrated.dll
                    //-------------------------------
                    ||
                    string.IsNullOrEmpty(jniSignature)
                    ||
                    ! jniSignature.Contains('(')
                    ||
                    ! jniSignature.Contains(')')
                )
            {
                return jniSignature;
            }

            var sig = new global::Xamarin.Android.Tools.Bytecode.MethodTypeSignature(jniSignature);

            var sb_newSig = new System.Text.StringBuilder();

            sb_newSig.Append("(");

            foreach (var p in sig.Parameters)
            {
                string mapped = "mapped"; //mappings[p];
                sb_newSig.Append($"L{mapped};" ?? p);               
            }

            sb_newSig.Append(")");

            sb_newSig.Append(sig.ReturnTypeSignature);

            string newSig = null;  // sb_newSig.ToString();

            return newSig;
        }
    }
}
