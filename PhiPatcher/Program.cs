using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MethodBody = Mono.Cecil.Cil.MethodBody;
using System.Collections.Generic;
using System.Xml;

namespace PhiPatcher
{
    class Program
    {
        private const string ModificationsXmlPath = "PhiPatcher.Modifications.xml";

        private const string AssemblyPath = "Assembly-CSharp.dll";
        private const string MovedAssemblyPath = "Assembly-CSharp.original.dll";

        private Dictionary<string, AssemblyDefinition> mLoadedAssemblies = new Dictionary<string, AssemblyDefinition>();

        private bool mAlreadyPatched;

        private XElement mXElement;

        private AssemblyDefinition mCSharpAssembly;

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
             * We load the target assembly
             */
            string assemblyPath = mAlreadyPatched ? MovedAssemblyPath : AssemblyPath;
            mCSharpAssembly = GetAssembly(assemblyPath);

            if (mCSharpAssembly == null)
            {
                return;
            }

            /**
             * We launch the patching
             */
            mXElement = LoadModifications(ModificationsXmlPath);
            
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

            mCSharpAssembly.Write(AssemblyPath);

            Console.WriteLine("Finished Writing");

            Console.WriteLine("Enter to continue");
            Console.Read();
        }

        public AssemblyDefinition GetAssembly(string name)
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

        public AssemblyDefinition LoadAssembly(string path)
        {
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
            foreach (XElement classNode in mXElement.Elements("Class"))
            {
                // We load the class in which the modifications will take place
                string nameTypeToPatch = classNode.Attribute("Name").Value;
                TypeDefinition typeToPatch = mCSharpAssembly.MainModule.Types.FirstOrDefault(t => t.Name == nameTypeToPatch);

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

                        for (int i = 0;i < countInstrToDelete;i++)
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

                        AssemblyDefinition assembly = GetAssembly(assemblyName + ".dll");
                        TypeReference typeReference = assembly.MainModule.GetType(variableType, true);
                        VariableDefinition variableDefinition = new VariableDefinition(variableName, typeReference);

                        methodBody.Variables.Add(variableDefinition);
                    }

                    foreach (XElement instrNode in methodNode.Elements("Instruction"))
                    {
                        Instruction instr = ParseInstruction(processor, methodBody, typeToPatch, instrNode, locationInstr);

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

        public Instruction ParseInstruction(ILProcessor processor, MethodBody methodBody, TypeDefinition type, XElement instrXml, Instruction locationInstr)
        {
            string nameOpCode = instrXml.Attribute("OpCode").Value;

            if (nameOpCode == "Call")
            {
                string assemblyName = instrXml.Attribute("Assembly").Value;
                string classToAddName = instrXml.Attribute("Class").Value;
                string methodToAddName = instrXml.Attribute("Method").Value;

                // We search in which assembly should we pull the method
                AssemblyDefinition assembly = GetAssembly(assemblyName + ".dll");

                if (assembly == null)
                {
                    return null;
                }

                ModuleDefinition module = assembly.MainModule;

                TypeDefinition typeToAdd = module.Types.FirstOrDefault(t => t.Name == classToAddName);

                if (typeToAdd == null)
                {
                    Console.WriteLine("Couldn't find type/class named " + classToAddName);
                    return null;
                }

                MethodDefinition methodToAdd = typeToAdd.Methods.FirstOrDefault(m => m.Name == methodToAddName);

                if (methodToAdd == null)
                {
                    Console.WriteLine("Couldn't find method named " + methodToAddName);
                    return null;
                }

                MethodReference methodToAddImported = mCSharpAssembly.MainModule.Import(methodToAdd);

                return processor.Create(OpCodes.Call, methodToAddImported);
            }
            if (nameOpCode == "Call.Generic")
            {
                string assemblyName = instrXml.Attribute("Assembly").Value;
                string genericTypeName = instrXml.Attribute("GenericType").Value;
                string typeName = instrXml.Attribute("Type").Value;
                string methodName = instrXml.Attribute("Method").Value;

                // We search in which assembly should we pull the method
                AssemblyDefinition assemblyDefinition = GetAssembly(assemblyName + ".dll");

                if (assemblyDefinition == null)
                    return null;

                ModuleDefinition moduleDefinition = assemblyDefinition.MainModule;
                TypeReference typeReference = moduleDefinition.GetType(typeName, true);
                TypeReference genericTypeReference = moduleDefinition.GetType(genericTypeName, true).MakeGenericInstanceType(typeReference);
                TypeDefinition typeDefinition = genericTypeReference.Resolve();

                MethodDefinition methodDefinition = typeDefinition.Methods.Single(m => m.Name == methodName);
                MethodReference methodReference = moduleDefinition.Import(methodDefinition);

                if (instrXml.Attribute("ReturnType") != null)
                {
                    string returnType = instrXml.Attribute("ReturnType").Value;

                    TypeReference returnTypeReference = moduleDefinition.GetType(returnType, true);
                    methodReference.ReturnType = returnTypeReference;
                }

                return processor.Create(OpCodes.Call, methodReference);
            }
            if (nameOpCode == "Ldc.I4")
            {
                int value = Int32.Parse(instrXml.Attribute("Value").Value);
                return processor.Create(OpCodes.Ldc_I4, value);
            }
            if (nameOpCode == "Ldfld")
            {
                string fieldName = instrXml.Attribute("Field").Value;
                FieldDefinition field = type.Fields.FirstOrDefault(f => f.Name == fieldName);

                if (field == null)
                {
                    Console.WriteLine("Couldn't find field named " + fieldName);
                }

                return processor.Create(OpCodes.Ldfld, field);
            }
            if (nameOpCode == "Ldarg.0")
            {
                return processor.Create(OpCodes.Ldarg_0);
            }
            if (nameOpCode == "Stloc.0")
            {
                return processor.Create(OpCodes.Stloc_0);
            }
            if (nameOpCode == "Ldloc.0")
            {
                return processor.Create(OpCodes.Ldloc_0);
            }
            if (nameOpCode == "Stloc.S")
            {
                int value = Int32.Parse(instrXml.Attribute("Value").Value);
                return processor.Create(OpCodes.Stloc_S, methodBody.Variables[value]);
            }
            if (nameOpCode == "Ldloc.S")
            {
                int value = Int32.Parse(instrXml.Attribute("Value").Value);
                return processor.Create(OpCodes.Ldloc_S, methodBody.Variables[value]);
            }
            if (nameOpCode == "Brtrue.S")
            {
                Instruction target = locationInstr;
                if (instrXml.Attribute("Value") != null)
                {
                    
                }

                return processor.Create(OpCodes.Brtrue_S, target);
            }
            if (nameOpCode == "Brfalse.S")
            {
                Instruction target = locationInstr;
                if (instrXml.Attribute("Value") != null)
                {

                }

                return processor.Create(OpCodes.Brfalse_S, target);
            }
            if (nameOpCode == "Ret")
            {
                return processor.Create(OpCodes.Ret);
            }
            if (nameOpCode == "Ldnull")
            {
                return processor.Create(OpCodes.Ldnull);
            }
            if (nameOpCode == "Ceq")
            {
                return processor.Create(OpCodes.Ceq);
            }

            Console.WriteLine("Couldn't find OpCode named " + nameOpCode);
            return null;
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

        static void Main()
        {
            Program program = new Program();

            program.Run();
        }
    }
}
