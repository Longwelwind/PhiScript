using System;
using System.IO;
using System.Linq;
using System.Xml;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MethodBody = Mono.Cecil.Cil.MethodBody;
using System.Collections.Generic;

namespace PhiPatcher
{
    class Program
    {
        private string ModificationsXmlPath = "PhiPatcher.Modifications.xml";

        private string AssemblyPath = "Assembly-CSharp.dll";
        private string MovedAssemblyPath = "Assembly-CSharp.original.dll";

        private Dictionary<string, AssemblyDefinition> _loadedAssemblies = new Dictionary<string, AssemblyDefinition>();

        private bool _alreadyPatched;

        private XmlDocument _modificationsXml;

        private AssemblyDefinition _cSharpAssembly;
        private ModuleDefinition _cSharpModule;

        public void Run()
        {
            /**
             * We check if the assembly has already been patched
             */
            _alreadyPatched = File.Exists(MovedAssemblyPath);

            if (_alreadyPatched)
            {
                Console.WriteLine(MovedAssemblyPath + " already present");
                Console.WriteLine("Using the backed-up " + MovedAssemblyPath);
            }

            /**
             * We load the target assembly
             */
            string assemblyPath = _alreadyPatched ? MovedAssemblyPath : AssemblyPath;
            _cSharpAssembly = GetAssembly(assemblyPath);

            if (_cSharpAssembly == null)
            {
                return;
            }
            
            /**
             * We launch the patching
             */
            _modificationsXml = LoadModifications(ModificationsXmlPath);
            
            PatchModifications();

            /**
             * We save or rename everything
             */
            // We rename the original dll
            if (!_alreadyPatched)
            {
                File.Move(AssemblyPath, MovedAssemblyPath);
            }

            Console.WriteLine("Writing the new Assembly in " + AssemblyPath);

            _cSharpAssembly.Write(AssemblyPath);

            Console.WriteLine("Finished Writing");

            Console.WriteLine("Enter to continue");
            Console.Read();
        }

        public AssemblyDefinition GetAssembly(string name)
        {
            if (_loadedAssemblies.ContainsKey(name))
            {
                return _loadedAssemblies[name];
            }
            else
            {
                AssemblyDefinition assembly = LoadAssembly(name);

                if (assembly != null)
                {
                    _loadedAssemblies.Add(name, assembly);
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
            XmlNode modifsNode = _modificationsXml.SelectSingleNode("Modifications");

            foreach (XmlNode classNode in modifsNode.ChildNodes)
            {
                // We load the class in which the modifications will take place
                string nameTypeToPatch = classNode.Attributes["Name"].Value;
                TypeDefinition typeToPatch = _cSharpModule.Types.FirstOrDefault(t => t.Name == nameTypeToPatch);

                if (typeToPatch == null)
                {
                    Console.WriteLine("Couldn't find type/class named" + nameTypeToPatch);
                    continue;
                }

                foreach (XmlNode methodNode in classNode.ChildNodes)
                {
                    string nameMethodTopatch = methodNode.Attributes["Name"].Value;
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
                    if (methodNode.Attributes["Location"] != null)
                    {
                        indexBegin = Int32.Parse(methodNode.Attributes["Location"].Value);
                    }

                    // If the user specified a count of instructions to delete,
                    // we delete them
                    if (methodNode.Attributes["DeleteCount"] != null)
                    {
                        int countInstrToDelete = Int32.Parse(methodNode.Attributes["DeleteCount"].Value);

                        for (int i = 0;i < countInstrToDelete;i++)
                        {
                            processor.Remove(methodToPatch.Body.Instructions.ElementAt(indexBegin));
                        }
                    }

                    if (methodNode.Attributes["TempVariable"] != null)
                    {
                        string tempVariable = methodNode.Attributes["TempVariable"].Value;

                        methodBody.Variables.Add(new VariableDefinition(_cSharpModule.Import(Type.GetType(tempVariable))));
                    }


                    Instruction locationInstr = methodToPatch.Body.Instructions.ElementAt(indexBegin);
                    Instruction prevInstr = locationInstr.Previous;

                    foreach (XmlNode instrNode in methodNode.ChildNodes)
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

        public Instruction ParseInstruction(ILProcessor processor, MethodBody methodBody, TypeDefinition type, XmlNode instrXml, Instruction locationInstr)
        {
            Instruction instr = null;

            string nameOpCode = instrXml.Attributes["OpCode"].Value;

            if (nameOpCode == "Call")
            {
                string assemblyName = instrXml.Attributes["Assembly"].Value;
                string classToAddName = instrXml.Attributes["Class"].Value;
                string methodToAddName = instrXml.Attributes["Method"].Value;

                

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

                MethodReference methodToAddImported = _cSharpAssembly.MainModule.Import(methodToAdd);

                instr = processor.Create(OpCodes.Call, methodToAddImported);
            }
            else if (nameOpCode == "Ldc.I4")
            {
                int value = Int32.Parse(instrXml.Attributes["Value"].Value);
                instr = processor.Create(OpCodes.Ldc_I4, value);
            }
            else if (nameOpCode == "Ldfld")
            {
                string fieldName = instrXml.Attributes["Field"].Value;
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
                int value = Int32.Parse(instrXml.Attributes["Value"].Value);
                instr = processor.Create(OpCodes.Stloc_S, methodBody.Variables[value]);
            }
            else if (nameOpCode == "Ldloc_S")
            {
                int value = Int32.Parse(instrXml.Attributes["Value"].Value);
                instr = processor.Create(OpCodes.Ldloc_S, methodBody.Variables[value]);
            }
            else if (nameOpCode == "Brtrue_S")
            {
                Instruction target = locationInstr;
                if (instrXml.Attributes["Value"] != null)
                {
                    
                }

                instr = processor.Create(OpCodes.Brtrue_S, target);
            }
            else if (nameOpCode == "Brfalse_S")
            {
                Instruction target = locationInstr;
                if (instrXml.Attributes["Value"] != null)
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

        public XmlDocument LoadModifications(string path)
        {
            /**
             * We load the file containing the modifications to patch
             * in the assembly
             */
            var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            StreamReader reader = new StreamReader(stream);

            XmlDocument modif = new XmlDocument();
            modif.LoadXml(reader.ReadToEnd());

            return modif;
        }

        static void Main()
        {
            Program program = new Program();

            program.Run();
        }
    }
}
