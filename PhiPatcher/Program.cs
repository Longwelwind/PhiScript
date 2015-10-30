using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.IO;
using System.Xml;
using System.Reflection;

namespace PhiPatcher
{
    class Program
    {
        private string ModificationsXmlPath = "PhiPatcher.Modifications.xml";

        private string AssemblyPath = "Assembly-CSharp.dll";
        private string MovedAssemblyPath = "Assembly-CSharp.original.dll";
        private string PhiAssemblyPath = "PhiScript.dll";

        private Boolean AlreadyPatched = false;

        private XmlDocument ModificationsXml;

        private AssemblyDefinition CSharpAssembly;
        private ModuleDefinition CSharpModule;

        private AssemblyDefinition PhiAssembly;
        private TypeDefinition PhiType;

        public Program()
        {

        }

        public void Run()
        {
            /**
             * We check if the assembly has already been patched
             */
            this.AlreadyPatched = File.Exists(this.MovedAssemblyPath);

            if (this.AlreadyPatched)
            {
                Console.WriteLine(this.MovedAssemblyPath + " already present");
                Console.WriteLine("Using the backed-up " + this.MovedAssemblyPath);
            }

            /**
             * We launch the patching
             */
            this.ModificationsXml = this.LoadModifications(this.ModificationsXmlPath);

            this.LoadAssemblies();
            this.PatchModifications();

            /**
             * We save or rename everything
             */
            // We rename the original dll
            if (!this.AlreadyPatched)
            {
                System.IO.File.Move(this.AssemblyPath, this.MovedAssemblyPath);
            }

            Console.WriteLine("Writing the new Assembly in " + this.AssemblyPath);

            this.CSharpAssembly.Write(this.AssemblyPath);

            Console.WriteLine("Finished Writing");

            Console.WriteLine("Enter to continue");
            Console.Read();
        }

        public void LoadAssemblies()
        {
            /**
             * We first load the PhiScript assembly containing the static
             * methods that must be called
             */
            if (File.Exists(this.PhiAssemblyPath))
            {
                try
                {
                    this.PhiAssembly = AssemblyDefinition.ReadAssembly(this.PhiAssemblyPath);
                }
                catch (FileNotFoundException e)
                {
                    Console.WriteLine("Can't find file " + this.PhiAssemblyPath);
                    Console.Read();
                    return;
                }
                catch
                {
                    Console.WriteLine("Couldn't load " + this.PhiAssemblyPath);
                    Console.Read();
                    return;
                }

                ModuleDefinition phiModule = this.PhiAssembly.MainModule;
                this.PhiType = phiModule.GetType("PhiScript.Phi");
            }

            /**
             * We then load the Assembly-CSharp assembly that contains the
             * vanilla code
             * If it is the first use, we read Assembly-CSharp.dll, but if it is
             * not we read Assembly-CSharp.original.dll
             */
            try
            {
                if (this.AlreadyPatched)
                {
                    this.CSharpAssembly = AssemblyDefinition.ReadAssembly(this.MovedAssemblyPath);
                }
                else
                {
                    this.CSharpAssembly = AssemblyDefinition.ReadAssembly(this.AssemblyPath);
                }
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine("Can't find file " + this.AssemblyPath);
                Console.Read();
                return;
            }
            catch
            {
                Console.WriteLine("Couldn't load " + this.AssemblyPath);
                Console.Read();
                return;
            }

            this.CSharpModule = this.CSharpAssembly.MainModule;
        }

        public void PatchModifications()
        {
            XmlNode modifsNode = this.ModificationsXml.SelectSingleNode("Modifications");

            foreach (XmlNode classNode in modifsNode.ChildNodes)
            {
                // We load the class in which the modifications will take place
                string nameTypeToPatch = classNode.Attributes["Name"].Value;
                TypeDefinition typeToPatch = this.CSharpModule.Types.FirstOrDefault(t => t.Name == nameTypeToPatch);

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
                        Console.WriteLine("Couldn't find method named" + methodToPatch);
                        continue;
                    }

                    ILProcessor processor = methodToPatch.Body.GetILProcessor();

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

                    Instruction prevInstr = methodToPatch.Body.Instructions.ElementAt(indexBegin).Previous;

                    foreach (XmlNode instrNode in methodNode.ChildNodes)
                    {
                        Instruction instr = this.ParseInstruction(processor, typeToPatch, instrNode);

                        if (instr == null)
                        {
                            continue;
                        }

                        processor.InsertAfter(
                            prevInstr,
                            instr
                        );

                        prevInstr = instr;
                    }
                }
            }
        }

        public Instruction ParseInstruction(ILProcessor processor, TypeDefinition type, XmlNode instrXml)
        {
            Instruction instr = null;

            string nameOpCode = instrXml.Attributes["OpCode"].Value;

            if (nameOpCode == "Call")
            {
                string assemblyName = instrXml.Attributes["Assembly"].Value;
                string classToAddName = instrXml.Attributes["Class"].Value;
                string methodToAddName = instrXml.Attributes["Method"].Value;

                ModuleDefinition module = null;

                // We search in which assembly should we pull the method
                if (assemblyName == "CSharp-Assembly")
                {
                    module = this.CSharpAssembly.MainModule;
                }
                else if (assemblyName == "PhiScript")
                {
                    module = this.PhiAssembly.MainModule;
                }
                else
                {
                    // Error handling
                    Console.WriteLine("Couldn't find assembly named " + assemblyName);
                    return null;
                }

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

                MethodReference methodToAddImported = this.CSharpAssembly.MainModule.Import(methodToAdd);

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
                    Console.WriteLine("Couldn't find field named " + field);
                }

                instr = processor.Create(OpCodes.Ldfld, field);
            }
            else if (nameOpCode == "Ldarg_0")
            {
                instr = processor.Create(OpCodes.Ldarg_0);
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
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            StreamReader reader = new StreamReader(stream);

            XmlDocument modif = new XmlDocument();
            modif.LoadXml(reader.ReadToEnd());

            return modif;
        }

        static void Main(string[] args)
        {
            Program program = new Program();

            program.Run();
        }
    }

    public class InstructionEntry
    {
        public OpCode OpCode
        {
            get;
        }

        public string Arg
        {
            get;
        }

        public InstructionEntry(OpCode opCode, string arg)
        {
            this.OpCode = opCode;
            this.Arg = arg;
        }
    }
}
