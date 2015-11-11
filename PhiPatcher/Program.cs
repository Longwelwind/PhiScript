using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace PhiPatcher
{
    internal class Program
    {
        private const string ModificationsXmlPath = "PhiPatcher.Modifications.xml";

        private const string AssemblyPath = "Assembly-CSharp.dll";
        private const string MovedAssemblyPath = "Assembly-CSharp.original.dll";

        private static Dictionary<string, AssemblyDefinition> mLoadedAssemblies = new Dictionary<string, AssemblyDefinition>();

        private bool mAlreadyPatched;

        private XElement mModifications;

        public void Run()
        {
            /**
             * We check if the assembly has already been patched
             */

            mAlreadyPatched = File.Exists(MovedAssemblyPath);

            if (mAlreadyPatched)
            {
                Console.WriteLine(MovedAssemblyPath + " already present");
                Console.WriteLine("Using the backed-up " + MovedAssemblyPath);
            }

            /**
             * We load necessary assemblies
             */
            string assemblyPath = mAlreadyPatched ? MovedAssemblyPath : AssemblyPath;

            var cSharpAssembly = LoadAssembly(assemblyPath);
            var phiScript = LoadAssembly("PhiScript.dll");

            if (cSharpAssembly == null || phiScript == null)
            {
                Console.Read();
                return;
            }

            var coreLibrary =
                cSharpAssembly.MainModule.AssemblyResolver.Resolve(
                    (AssemblyNameReference)cSharpAssembly.MainModule.TypeSystem.CoreLibrary);

            mLoadedAssemblies.Add("Assembly-CSharp", cSharpAssembly);
            mLoadedAssemblies.Add("PhiScript", phiScript);
            mLoadedAssemblies.Add("CoreLibrary", coreLibrary);

            /**
             * We launch the patching
             */
            mModifications = LoadModifications(ModificationsXmlPath);

            PatchModifications();

            /**
             * We save or rename everything
             */
            // We rename the original dll
            if (!mAlreadyPatched)
            {
                File.Move(AssemblyPath, MovedAssemblyPath);
            }

            Console.WriteLine("Writing the new Assembly in " + AssemblyPath);

            GetAssembly("Assembly-CSharp").Write(AssemblyPath);

            Console.WriteLine("Finished Writing");

            Console.WriteLine("Enter to continue");
            Console.Read();
        }

        public static AssemblyDefinition GetAssembly(string name)
        {
            if (mLoadedAssemblies.ContainsKey(name))
            {
                return mLoadedAssemblies[name];
            }
            AssemblyDefinition assembly = LoadAssembly(name);

            if (assembly != null)
            {
                mLoadedAssemblies.Add(name, assembly);
                return assembly;
            }
            return null;
        }

        public static AssemblyDefinition LoadAssembly(string path)
        {
            if (!path.EndsWith(".dll"))
                path += ".dll";

            if (File.Exists(path))
            {
                try
                {
                    AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(path);

                    return assembly;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Couldn't load assembly " + path);
                    Console.WriteLine(e);
                }
            }
            else
            {
                Console.WriteLine("Assembly " + path + " doesn't exist");
            }

            return null;
        }

        public void PatchModifications()
        {
            foreach (XElement classNode in mModifications.Elements("Class"))
            {
                // We load the class in which the modifications will take place
                string nameTypeToPatch = classNode.Attribute("Name").Value;
                TypeDefinition typeToPatch = GetAssembly("Assembly-CSharp").MainModule.Types.FirstOrDefault(t => t.Name == nameTypeToPatch);

                if (typeToPatch == null)
                {
                    Console.WriteLine("Couldn't find type/class named" + nameTypeToPatch);
                    continue;
                }

                foreach (XElement methodNode in classNode.Elements("Method"))
                {
                    string nameMethodTopatch = methodNode.Attribute("Name").Value;
                    MethodDefinition methodToPatch = typeToPatch.Methods.FirstOrDefault(m => m.Name == nameMethodTopatch);

                    if (methodToPatch == null)
                    {
                        Console.WriteLine("Couldn't find method named" + nameMethodTopatch);
                        continue;
                    }

                    MethodBody methodBody = methodToPatch.Body;

                    ILProcessor processor = methodBody.GetILProcessor();

                    // By default, we place the modification just before the "ret" instruction
                    // (i.e. before the last instruction)
                    int indexBegin = methodToPatch.Body.Instructions.Count - 1;

                    // If the user specified a location, we begin there
                    if (methodNode.Attribute("Location") != null)
                    {
                        indexBegin = int.Parse(methodNode.Attribute("Location").Value);
                    }

                    // If the user specified a count of instructions to delete,
                    // we delete them
                    if (methodNode.Attribute("DeleteCount") != null)
                    {
                        int countInstrToDelete = int.Parse(methodNode.Attribute("DeleteCount").Value);

                        for (int i = 0; i < countInstrToDelete; i++)
                        {
                            processor.Remove(methodToPatch.Body.Instructions.ElementAt(indexBegin));
                        }
                    }

                    Instruction locationInstr = methodToPatch.Body.Instructions.ElementAt(indexBegin);
                    Instruction prevInstr = locationInstr.Previous;

                    foreach (XElement variableNode in methodNode.Elements("Variable"))
                    {
                        string variableName = variableNode.Attribute("Name").Value;
                        string variableType = variableNode.Attribute("Type").Value;
                        string assemblyName = variableNode.Attribute("Assembly").Value;

                        AssemblyDefinition assembly = GetAssembly(assemblyName);
                        TypeDefinition typeDefinition = assembly.MainModule.GetType(variableType);
                        TypeReference typeReference = GetAssembly("Assembly-CSharp").MainModule.ImportReference(typeDefinition);

                        if (variableNode.HasElements)
                        {
                            List<TypeReference> genericParameters = new List<TypeReference>();

                            foreach (XElement genericParameter in variableNode.Elements("GenericParameter"))
                            {
                                var gPAssemblyName = genericParameter.Attribute("Assembly").Value;
                                var gPType = genericParameter.Attribute("Type").Value;

                                AssemblyDefinition gPAssembly = GetAssembly(gPAssemblyName);
                                TypeDefinition gPTypeDefinition = gPAssembly.MainModule.GetType(gPType);
                                TypeReference gPTypeReference =
                                    GetAssembly("Assembly-CSharp").MainModule.ImportReference(gPTypeDefinition);

                                genericParameters.Add(gPTypeReference);
                            }

                            typeReference = typeReference.MakeGenericInstanceType(genericParameters.ToArray());
                        }

                        VariableDefinition variableDefinition = new VariableDefinition(variableName, typeReference);

                        methodBody.Variables.Add(variableDefinition);
                    }

                    foreach (XElement instrNode in methodNode.Elements("Instruction"))
                    {
                        Instruction instr = InstructionBuilder.Build(processor, typeToPatch, instrNode);

                        if (instr == null)
                        {
                            continue;
                        }

                        if (prevInstr == null)
                            processor.InsertBefore(locationInstr, instr);
                        else
                            processor.InsertAfter(prevInstr, instr);

                        prevInstr = instr;
                    }

                    // Optimize the method
                    methodToPatch.Body.OptimizeMacros();
                }
            }
        }

        public XElement LoadModifications(string path)
        {
            /**
             * We load the file containing the modifications to patch
             * in the assembly
             */
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            return XElement.Load(stream);
        }

        private static void Main()
        {
            Program program = new Program();

            program.Run();
        }
    }
}