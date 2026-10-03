using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;

// patchcheck <ManagedDir> <Subnautica.Core.dll>
// Tries to apply every Harmony patch class of the mod against the game's assemblies and reports the ones that fail.
class Program
{
    static int Main(string[] a)
    {
        var managed = a[0];
        var core = Path.GetFullPath(a[1]);
        var coreDir = Path.GetDirectoryName(core);
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            var n = new AssemblyName(e.Name).Name;
            foreach (var d in managed.Split(";".ToCharArray()).Concat(new[] { coreDir }))
            {
                var p = Path.Combine(d, n + ".dll");
                if (File.Exists(p)) return Assembly.LoadFrom(p);
            }
            return null;
        };
        var asm = Assembly.LoadFrom(core);
        Type[] types;
        try { types = asm.GetTypes(); } catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); foreach (var le in ex.LoaderExceptions.Take(5)) Console.WriteLine("LOADERR " + le.Message); }

        if (a.Length > 2 && a[2] == "targets") return CheckTargets(types);
        var harmony = new Harmony("patchcheck");
        int ok = 0, fail = 0;
        foreach (var t in types)
        {
            bool has = t.GetCustomAttributes(typeof(HarmonyPatch), true).Any() || t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly).Any(m => m.GetCustomAttributes(typeof(HarmonyPatch), true).Any());
            if (!has) continue;
            try
            {
                new PatchClassProcessor(harmony, t).Patch();
                ok++;
            }
            catch (Exception ex)
            {
                fail++;
                var inner = ex;
                while (inner.InnerException != null) inner = inner.InnerException;
                Console.WriteLine("FAIL " + t.FullName + " :: " + inner.GetType().Name + " :: " + inner.Message.Split('\n')[0]);
            }
        }
        Console.WriteLine($"patch classes ok={ok} failed={fail}");
        return fail == 0 ? 0 : 2;
    }

    static HarmonyMethod Merge(HarmonyMethod baseInfo, HarmonyMethod over)
    {
        var r = new HarmonyMethod();
        foreach (var src in new[] { baseInfo, over })
        {
            if (src == null) continue;
            if (src.declaringType != null) r.declaringType = src.declaringType;
            if (src.methodName != null) r.methodName = src.methodName;
            if (src.methodType != null) r.methodType = src.methodType;
            if (src.argumentTypes != null) r.argumentTypes = src.argumentTypes;
        }
        return r;
    }

    static int CheckTargets(Type[] types)
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        string[] kinds = { "Prefix", "Postfix", "Transpiler", "Finalizer" };
        int bad = 0, total = 0;
        foreach (var t in types)
        {
            var classInfos = t.GetCustomAttributes(typeof(HarmonyPatch), true).Cast<HarmonyPatch>().Select(x => x.info).ToList();
            foreach (var m in t.GetMethods(All))
            {
                var methodInfos = m.GetCustomAttributes(typeof(HarmonyPatch), true).Cast<HarmonyPatch>().Select(x => x.info).ToList();
                bool isPatch = methodInfos.Count > 0 || m.GetCustomAttributes(true).Any(x => x is HarmonyPrefix || x is HarmonyPostfix || x is HarmonyTranspiler || x is HarmonyFinalizer) || kinds.Contains(m.Name);
                if (!isPatch) continue;
                if (classInfos.Count == 0 && methodInfos.Count == 0) continue;
                var targets = new System.Collections.Generic.List<HarmonyMethod>();
                if (classInfos.Count == 0) targets.AddRange(methodInfos.Select(x => Merge(null, x)));
                else if (methodInfos.Count == 0) targets.AddRange(classInfos.Select(x => Merge(x, null)));
                else foreach (var c in classInfos) foreach (var mi in methodInfos) targets.Add(Merge(c, mi));
                foreach (var tg in targets)
                {
                    total++;
                    if (Environment.GetEnvironmentVariable("PC_LIST") == "1" && tg.declaringType != null && tg.methodName != null) Console.WriteLine("TARGET " + tg.declaringType.FullName + " " + tg.methodName + " " + (m.GetCustomAttributes(true).Any(x => x is HarmonyTranspiler) || m.Name.Contains("Transpiler") || m.GetParameters().Any(p => p.Name == "instructions") ? "T" : "-"));
                    string err = null;
                    MethodBase orig = null;
                    try
                    {
                        if (tg.declaringType == null) { err = "no declaringType (TargetMethod?)"; }
                        else
                        {
                            var mt = tg.methodType ?? MethodType.Normal;
                            switch (mt)
                            {
                                case MethodType.Normal: orig = tg.methodName == null ? null : AccessTools.DeclaredMethod(tg.declaringType, tg.methodName, tg.argumentTypes); break;
                                case MethodType.Getter: orig = AccessTools.DeclaredProperty(tg.declaringType, tg.methodName)?.GetGetMethod(true); break;
                                case MethodType.Setter: orig = AccessTools.DeclaredProperty(tg.declaringType, tg.methodName)?.GetSetMethod(true); break;
                                case MethodType.Constructor: orig = AccessTools.DeclaredConstructor(tg.declaringType, tg.argumentTypes); break;
                                case MethodType.StaticConstructor: orig = tg.declaringType.TypeInitializer; break;
                                case MethodType.Enumerator:
                                    var em = AccessTools.DeclaredMethod(tg.declaringType, tg.methodName, tg.argumentTypes);
                                    orig = em == null ? null : AccessTools.EnumeratorMoveNext(em); break;
                            }
                            if (orig == null && err == null) err = "target not found: " + tg.declaringType.FullName + "::" + tg.methodName + (tg.argumentTypes != null ? "(" + string.Join(",", tg.argumentTypes.Select(x => x.Name)) + ")" : "");
                        }
                        if (orig != null)
                        {
                            var names = orig.GetParameters().Select(p => p.Name).ToList();
                            foreach (var p in m.GetParameters())
                            {
                                var n = p.Name;
                                if (n == "__instance" || n == "__result" || n == "__state" || n == "__originalMethod" || n == "__runOriginal" || n == "__exception" || n == "__args" || n.StartsWith("__")) { if (n.StartsWith("__") && int.TryParse(n.Substring(2), out var idx) && idx >= names.Count) err = "param index " + n + " out of range"; continue; }
                                if (n.StartsWith("___")) continue;
                                if (!names.Contains(n)) err = "param '" + n + "' not in original (" + string.Join(",", names) + ")";
                            }
                            foreach (var p in m.GetParameters().Where(x => x.Name.StartsWith("___")))
                            {
                                var fname = p.Name.Substring(3);
                                if (AccessTools.Field(orig.DeclaringType, fname) == null && AccessTools.Property(orig.DeclaringType, fname) == null) err = "field '" + fname + "' not in " + orig.DeclaringType.Name;
                            }
                        }
                    }
                    catch (Exception ex) { err = ex.GetType().Name + ": " + ex.Message.Split((char)10)[0]; }
                    if (err != null) { bad++; Console.WriteLine("BAD " + t.FullName + "." + m.Name + " -> " + err); }
                }
            }
        }
        Console.WriteLine("checked patch methods=" + total + " bad=" + bad);
        return bad == 0 ? 0 : 2;
    }
}
