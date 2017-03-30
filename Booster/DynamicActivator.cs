using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Booster
{
    public class DynamicActivator
    {
        public delegate object ConstructorHandler(params object[] args);


        protected class ArgumentInfo
        {
            public ArgumentInfo(Type t)
            {
                this.Type = t;
            }
            public Type Type { get; private set; }
            public bool Compatible(object obj)
            {
                if (obj == null)
                {
                    return !Type.IsValueType;
                }
                return GetAllTypes(obj.GetType()).Any(x => x == Type);
            }
            private static IEnumerable<Type> GetAllTypes(Type t)
            {
                yield return t;
                if (t.BaseType != null)
                {
                    foreach (var each in GetAllTypes(t.BaseType))
                    {
                        yield return each;
                    }
                }
            }
        }
        protected class TypeConstructor
        {
            public TypeConstructor(ArgumentInfo[] args)
            {
                this.Arguments = args;
            }
            public ArgumentInfo[] Arguments { get; private set; }
            public ConstructorHandler Constructor { get; set; }
            public bool IsCompatibleParameters(params object[] args)
            {
                if (args == null && args.Length == 0)
                    return Arguments == null || Arguments.Length == 0;

                if (Arguments == null || Arguments.Length == 0)
                    return args == null && args.Length == 0;

                if (Arguments.Length != args.Length)
                    return false;

                for (int i = 0; i < Arguments.Length; i++)
                {
                    var source = Arguments[i];
                    var target = args[i];
                    if (!source.Compatible(target))
                        return false;
                }
                return true;
            }
        }
        private static Dictionary<Type, TypeConstructor[]> dic =
            new Dictionary<Type, TypeConstructor[]>();
        private static readonly object Sync = new object();


        public static object CreateInstance(Type t, params object[] args)
        {
            var info = CreateFactory(t, args);
            return info?.Invoke(args);
        }

        // http://stackoverflow.com/questions/8219343/reflection-emit-create-object-with-parameters
        public static ConstructorHandler CreateFactory(Type t, params object[] args)
        {
            TypeConstructor[] ctors;
            if (!dic.TryGetValue(t, out ctors))
            {
                lock (Sync)
                {
                    if (!dic.TryGetValue(t, out ctors))
                    {
                        var list = new List<TypeConstructor>();
                        foreach (var each in t.GetConstructors(BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            var ps = each.GetParameters();
                            var item = new TypeConstructor(ps.Select(x => new ArgumentInfo(x.ParameterType)).ToArray());
                            var method = new DynamicMethod($"._{Guid.NewGuid()}", typeof(object), new Type[] { typeof(object[]) }, true);
                            var il = method.GetILGenerator();

                            il.DeclareLocal(typeof(int));
                            il.DeclareLocal(typeof(object));
                            il.Emit(OpCodes.Ldc_I4_0); // [0]
                            il.Emit(OpCodes.Stloc_0); //[nothing]
                            var parameters = each.GetParameters();
                            for (int i = 0; i < parameters.Length; i++)
                            {
                                EmitInt32(il, i); // [index]
                                il.Emit(OpCodes.Stloc_0); // [nothing]
                                il.Emit(OpCodes.Ldarg_0); //[args]
                                EmitInt32(il, i); // [args][index]
                                il.Emit(OpCodes.Ldelem_Ref); // [item-in-args-at-index]
                                var paramType = parameters[i].ParameterType;
                                if (paramType != typeof(object))
                                {
                                    il.Emit(OpCodes.Unbox_Any, paramType); // same as a cast if ref-type
                                }
                            }
                            il.Emit(OpCodes.Newobj, each); //[new-object]
                            if (each.DeclaringType.IsValueType)
                            {
                                il.Emit(OpCodes.Box, each.DeclaringType);
                            }
                            il.Emit(OpCodes.Stloc_1); // nothing
                            il.Emit(OpCodes.Ldloc_1); //[new-object]
                            il.Emit(OpCodes.Ret);

                            item.Constructor = (ConstructorHandler)method.CreateDelegate(typeof(ConstructorHandler));
                            list.Add(item);
                        }
                        dic[t] = list.ToArray();
                    }
                }
            }
            if (ctors == null)
                ctors = dic[t];

            return ctors?.FirstOrDefault(x => x.IsCompatibleParameters(args))?.Constructor;
        }

        private static void EmitInt32(ILGenerator il, int value)
        {
            switch (value)
            {
                case -1: il.Emit(OpCodes.Ldc_I4_M1); break;
                case 0: il.Emit(OpCodes.Ldc_I4_0); break;
                case 1: il.Emit(OpCodes.Ldc_I4_1); break;
                case 2: il.Emit(OpCodes.Ldc_I4_2); break;
                case 3: il.Emit(OpCodes.Ldc_I4_3); break;
                case 4: il.Emit(OpCodes.Ldc_I4_4); break;
                case 5: il.Emit(OpCodes.Ldc_I4_5); break;
                case 6: il.Emit(OpCodes.Ldc_I4_6); break;
                case 7: il.Emit(OpCodes.Ldc_I4_7); break;
                case 8: il.Emit(OpCodes.Ldc_I4_8); break;
                default:
                    if (value >= -128 && value <= 127)
                    {
                        il.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldc_I4, value);
                    }
                    break;
            }
        }
    }
}
