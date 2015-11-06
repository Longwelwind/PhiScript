﻿using System;
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
        private ModuleDefinition mCSharpModule;

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
            else
            {
                AssemblyDefinition assembly = LoadAssembly(name);

                if (assembly != null)
                {
                    mLoadedAssemblies.Add(name, assembly);
                    return assembly;
                }
                else
                {
                    return null;
                }
            }
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
            foreach (XElement classNode in mPatches.Elements("Class"))
            {
                // We load the class in which the modifications will take place
                string nameTypeToPatch = classNode.Attribute("Name").Value;
                TypeDefinition typeToPatch = mCSharpModule.Types.FirstOrDefault(t => t.Name == nameTypeToPatch);

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

                    if (methodNode.Attribute("TempVariable") != null)
                    {
                        string tempVariable = methodNode.Attribute("TempVariable").Value;

                        methodBody.Variables.Add(new VariableDefinition(mCSharpModule.Import(Type.GetType(tempVariable))));
                    }


                    Instruction locationInstr = methodToPatch.Body.Instructions.ElementAt(indexBegin);
                    Instruction prevInstr = locationInstr.Previous;

                    foreach (XElement instrNode in methodNode.Descendants())
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
            Instruction instr = null;

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

                instr = processor.Create(OpCodes.Call, methodToAddImported);
            }
            else if (nameOpCode == "Ldc.I4")
            {
                int value = Int32.Parse(instrXml.Attribute("Value").Value);
                instr = processor.Create(OpCodes.Ldc_I4, value);
            }
            else if (nameOpCode == "Ldfld")
            {
                string fieldName = instrXml.Attribute("Field").Value;
                FieldDefinition field = type.Fields.FirstOrDefault(f => f.Name == fieldName);

                if (field == null)
                {
                    Console.WriteLine("Couldn't find field named " + fieldName);
                }

                instr = processor.Create(OpCodes.Ldfld, field);
            }
            else if (nameOpCode == "Ldarg_0")
            {
                instr = processor.Create(OpCodes.Ldarg_0);
            }
            else if (nameOpCode == "Stloc_0")
            {
                instr = processor.Create(OpCodes.Stloc_0);
            }
            else if (nameOpCode == "Ldloc_0")
            {
                instr = processor.Create(OpCodes.Ldloc_0);
            }
            else if (nameOpCode == "Stloc_S")
            {
                int value = Int32.Parse(instrXml.Attribute("Value").Value);
                instr = processor.Create(OpCodes.Stloc_S, methodBody.Variables[value]);
            }
            else if (nameOpCode == "Ldloc_S")
            {
                int value = Int32.Parse(instrXml.Attribute("Value").Value);
                instr = processor.Create(OpCodes.Ldloc_S, methodBody.Variables[value]);
            }
            else if (nameOpCode == "Brtrue_S")
            {
                Instruction target = locationInstr;
                if (instrXml.Attribute("Value") != null)
                {
                    
                }

                instr = processor.Create(OpCodes.Brtrue_S, target);
            }
            else if (nameOpCode == "Brfalse_S")
            {
                Instruction target = locationInstr;
                if (instrXml.Attribute("Value") != null)
                {

                }

                instr = processor.Create(OpCodes.Brfalse_S, target);
            }
            else if (nameOpCode == "Ret")
            {
                instr = processor.Create(OpCodes.Ret);
            }
            else if (nameOpCode == "Ldnull")
            {
                instr = processor.Create(OpCodes.Ldnull);
            }
            else if (nameOpCode == "Ceq")
            {
                instr = processor.Create(OpCodes.Ceq);
            }
            else
            {
                Console.WriteLine("Couldn't find OpCode named " + nameOpCode);
            }

            return instr;
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
