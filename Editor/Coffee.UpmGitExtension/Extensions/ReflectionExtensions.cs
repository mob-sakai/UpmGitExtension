using System.Text;
using System;
using System.Linq;
using System.Reflection;

namespace Coffee.UpmGitExtension
{
    internal static class ReflectionExtensions
    {
        const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static object Inst(this object self)
        {
            return (self is Type) ? null : self;
        }

        private static Type Type(this object self)
        {
            return (self as Type) ?? self.GetType();
        }

        public static object New(this Type self, params object[] args)
        {
            var types = args.Select(x => x.GetType()).ToArray();
            return self.Type().GetConstructor(types)
                .Invoke(args);
        }

        public static object Call(this object self, string methodName, params object[] args)
        {
            var types = args.Select(x => x.GetType()).ToArray();
            return self.Type().GetMethods(FLAGS)
                .Where(x => x.Name == methodName)
                .First(x =>
                {
                    var pTypes = x.GetParameters().Select(y => y.ParameterType).ToArray();
                    return pTypes.Length == types.Length
                        && Enumerable.Range(0, types.Length).All(i => pTypes[i].IsAssignableFrom(types[i]));
                })
                .Invoke(self.Inst(), args);
        }

        public static object Call(this object self, Type[] genericTypes, string methodName, params object[] args)
        {
            return self.Type().GetMethods(FLAGS)
                .First(x => x.IsGenericMethodDefinition && x.Name == methodName)
                .MakeGenericMethod(genericTypes)
                .Invoke(self.Inst(), args);
        }

        public static object Get(this object self, string memberName, MemberInfo mi = null)
        {
            mi = mi ?? self.Type().GetProperty(memberName, FLAGS) ?? (MemberInfo)self.Type().GetField(memberName, FLAGS);
            switch (mi)
            {
                case PropertyInfo pi:
                    return pi.GetValue(self.Inst(), new object[0]);
                case FieldInfo fi:
                    return fi.GetValue(self.Inst());
                default:
                    throw new MissingFieldException(self.Type().FullName, memberName);
            }
        }

        public static void Set(this object self, string memberName, object value, MemberInfo mi = null)
        {
            mi = mi ?? self.Type().GetProperty(memberName, FLAGS) ?? (MemberInfo)self.Type().GetField(memberName, FLAGS);
            switch (mi)
            {
                case PropertyInfo pi:
                    pi.SetValue(self.Inst(), value, new object[0]);
                    break;
                case FieldInfo fi:
                    fi.SetValue(self.Inst(), value);
                    break;
                default:
                    throw new MissingFieldException(self.Type().FullName, memberName);
            }
        }

        public static bool Has<T>(this object self, string memberName)
        {
            var mi = self.Type().GetProperty(memberName, FLAGS) ?? (MemberInfo)self.Type().GetField(memberName, FLAGS);
            switch (mi)
            {
                case PropertyInfo pi:
                    return pi.PropertyType == typeof(T);
                case FieldInfo fi:
                    return fi.FieldType == typeof(T);
                default:
                    throw new MissingFieldException(self.Type().FullName, memberName);
            }
        }

        public static bool Has(this object self, string methodName, object[] args)
        {
            var types = args.Select(x => x.GetType()).ToArray();
            return self.Type().GetMethods(FLAGS)
                .Where(x => x.Name == methodName)
                .Any(x =>
                {
                    var pTypes = x.GetParameters().Select(y => y.ParameterType).ToArray();
                    return pTypes.Length == types.Length
                        && Enumerable.Range(0, types.Length).All(i => pTypes[i].IsAssignableFrom(types[i]));
                });
        }

        public static bool Has<T>(this object self, string methodName, object[] args)
        {
            var types = args.Select(x => x.GetType()).ToArray();
            return self.Type().GetMethods(FLAGS)
                .Where(x => x.Name == methodName)
                .Any(x =>
                {
                    var pTypes = x.GetParameters().Select(y => y.ParameterType).ToArray();
                    return pTypes.Length == types.Length
                        && Enumerable.Range(0, types.Length).All(i => pTypes[i].IsAssignableFrom(types[i]))
                        && typeof(T).IsAssignableFrom(x.ReturnType);
                });
        }

        public static void Debug(this object self)
        {
            var sb = new StringBuilder($"{self.Type().FullName}:\n");
            UnityEngine.Debug.Log(self.Type().GetMembers(FLAGS)
                .Select(m => m.ToString())
                .Aggregate(sb, (b, s) => b.AppendLine(s)));
        }
    }
}

