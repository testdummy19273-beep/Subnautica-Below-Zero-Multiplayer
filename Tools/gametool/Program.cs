using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

// gametool publicize <in.dll> <out.dll>
// gametool inject <gameAssemblyCSharp.dll> <SubnauticaBootstrap.dll> <out.dll> <managedDir>
//   Copies the SubnauticaBootstrap loader class into the game's own DLL and calls it at the
//   start of StartScreen.Start (the hook the mod needs to get loaded).
class Program
{
    static int Main(string[] args)
    {
        if (args.Length >= 3 && args[0] == "publicize") return Publicize(args[1], args[2]);
        if (args.Length >= 5 && args[0] == "inject") return Inject(args[1], args[2], args[3], args[4]);
        Console.Error.WriteLine("usage: publicize <in> <out> | inject <game> <patched> <out> <managedDir>");
        return 1;
    }

    static ReaderParameters Params(string dir)
    {
        var r = new DefaultAssemblyResolver();
        r.AddSearchDirectory(dir);
        return new ReaderParameters { AssemblyResolver = r };
    }

    static int Publicize(string input, string output)
    {
        var asm = AssemblyDefinition.ReadAssembly(input, Params(Path.GetDirectoryName(Path.GetFullPath(input))));
        foreach (var t in asm.MainModule.GetTypes())
        {
            if (t.IsNested) t.IsNestedPublic = true; else t.IsPublic = true;
            foreach (var m in t.Methods) { if (!m.IsCompilerControlled) { m.IsPublic = true; } }
            foreach (var f in t.Fields) { if (!f.IsCompilerControlled) f.IsPublic = true; }
        }
        asm.Write(output);
        return 0;
    }

    static int Inject(string gamePath, string patchedPath, string outPath, string managedDir)
    {
        var game = AssemblyDefinition.ReadAssembly(gamePath, Params(managedDir));
        var patched = AssemblyDefinition.ReadAssembly(patchedPath, Params(managedDir));
        var src = patched.MainModule.GetType("SubnauticaBootstrap");
        if (src == null) throw new Exception("SubnauticaBootstrap not found in bootstrap DLL");
        var dstModule = game.MainModule;
        if (dstModule.GetType("SubnauticaBootstrap") != null) throw new Exception("game DLL already has SubnauticaBootstrap");

        var dst = new TypeDefinition(src.Namespace, src.Name, src.Attributes, dstModule.TypeSystem.Object);
        dstModule.Types.Add(dst);

        var fieldMap = new Dictionary<FieldDefinition, FieldDefinition>();
        foreach (var f in src.Fields)
        {
            var nf = new FieldDefinition(f.Name, f.Attributes, dstModule.ImportReference(f.FieldType));
            dst.Fields.Add(nf);
            fieldMap[f] = nf;
        }

        var methodMap = new Dictionary<MethodDefinition, MethodDefinition>();
        foreach (var m in src.Methods)
        {
            var nm = new MethodDefinition(m.Name, m.Attributes, dstModule.ImportReference(m.ReturnType)) { ImplAttributes = m.ImplAttributes };
            foreach (var p in m.Parameters) nm.Parameters.Add(new ParameterDefinition(p.Name, p.Attributes, dstModule.ImportReference(p.ParameterType)));
            dst.Methods.Add(nm);
            methodMap[m] = nm;
        }

        foreach (var m in src.Methods)
        {
            if (!m.HasBody) continue;
            var nm = methodMap[m];
            var body = nm.Body;
            body.InitLocals = m.Body.InitLocals;
            foreach (var v in m.Body.Variables) body.Variables.Add(new VariableDefinition(dstModule.ImportReference(v.VariableType)));
            var il = body.GetILProcessor();
            var map = new Dictionary<Instruction, Instruction>();
            foreach (var ins in m.Body.Instructions)
            {
                var ni = Clone(ins, dstModule, fieldMap, methodMap, body);
                map[ins] = ni;
                il.Append(ni);
            }
            foreach (var ins in m.Body.Instructions)
            {
                var ni = map[ins];
                if (ins.Operand is Instruction t) ni.Operand = map[t];
                else if (ins.Operand is Instruction[] ts) ni.Operand = ts.Select(x => map[x]).ToArray();
            }
            foreach (var h in m.Body.ExceptionHandlers)
            {
                body.ExceptionHandlers.Add(new ExceptionHandler(h.HandlerType)
                {
                    TryStart = map[h.TryStart],
                    TryEnd = h.TryEnd == null ? null : map[h.TryEnd],
                    HandlerStart = map[h.HandlerStart],
                    HandlerEnd = h.HandlerEnd == null ? null : map[h.HandlerEnd],
                    FilterStart = h.FilterStart == null ? null : map[h.FilterStart],
                    CatchType = h.CatchType == null ? null : dstModule.ImportReference(h.CatchType),
                });
            }
        }

        var startScreen = dstModule.GetType("StartScreen");
        var start = startScreen.Methods.First(x => x.Name == "Start" && x.Parameters.Count == 0);
        var load = methodMap.Keys.First(x => x.Name == "Load");
        var proc = start.Body.GetILProcessor();
        proc.InsertBefore(start.Body.Instructions[0], proc.Create(OpCodes.Call, methodMap[load]));

        game.Write(outPath);
        Console.WriteLine("Injected SubnauticaBootstrap -> " + outPath);
        return 0;
    }

    static Instruction Clone(Instruction ins, ModuleDefinition mod, Dictionary<FieldDefinition, FieldDefinition> fm, Dictionary<MethodDefinition, MethodDefinition> mm, MethodBody body)
    {
        var op = ins.OpCode;
        switch (ins.Operand)
        {
            case null: return Instruction.Create(op);
            case MethodDefinition md when mm.ContainsKey(md): return Instruction.Create(op, mm[md]);
            case MethodReference mr: return Instruction.Create(op, mod.ImportReference(mr));
            case FieldDefinition fd when fm.ContainsKey(fd): return Instruction.Create(op, fm[fd]);
            case FieldReference fr: return Instruction.Create(op, mod.ImportReference(fr));
            case TypeReference tr: return Instruction.Create(op, mod.ImportReference(tr));
            case VariableDefinition vd: return Instruction.Create(op, body.Variables[vd.Index]);
            case ParameterDefinition pd: return Instruction.Create(op, body.Method.Parameters[pd.Index]);
            case string s: return Instruction.Create(op, s);
            case int i: return Instruction.Create(op, i);
            case long l: return Instruction.Create(op, l);
            case float f: return Instruction.Create(op, f);
            case double d: return Instruction.Create(op, d);
            case sbyte sb: return Instruction.Create(op, sb);
            case byte b: return Instruction.Create(op, b);
            case Instruction _: return Instruction.Create(op, ins); // fixed up later
            case Instruction[] _: return Instruction.Create(op, new Instruction[0]); // fixed up later
            default: throw new Exception("Unhandled operand " + ins.Operand.GetType() + " for " + op);
        }
    }
}
