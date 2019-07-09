using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace Coffee.PackageManager
{
	public class Expose : IEnumerable
	{
		const BindingFlags k_InstanceMemberFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy;
		const BindingFlags k_StaticMemberFlags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy;

		static readonly Type s_extensionType = typeof (System.Runtime.CompilerServices.ExtensionAttribute);

		static readonly Type [] s_AllTypes = System.AppDomain.CurrentDomain
			.GetAssemblies ()
			.SelectMany (x => x.GetTypes ())
			.ToArray ();

		static readonly Dictionary<Type, MethodInfo []> s_extensionMethods =
			 new Dictionary<Type, MethodInfo []> ();
			//s_AllTypes
			//.Where (x => x.IsPublic && x.IsClass)
			//.SelectMany (x => x.GetMethods (BindingFlags.Static | BindingFlags.Public))
			//.Where (x => x.IsDefined (s_extensionType, false))
			//.GroupBy (x => ToBaseClass (x.GetParameters () [0].ParameterType))
			//.ToDictionary (x => x.Key, x => x.ToArray ());

		static readonly Dictionary<string, Type> s_TypeByName = s_AllTypes
			.GroupBy (x => x.FullName)
			.Concat (s_AllTypes.GroupBy (x => x.Name))
			.Select (x => new { x.Key, Value = x.Count () == 1 ? x.First () : null })
			.GroupBy (x => x.Key, x => x.Value)
			.ToDictionary (x => x.Key, x => x.Count () == 1 ? x.First () : null);

		public Type Type { get; set; }
		public object Value { get; set; }
		public BindingFlags Flag { get; set; }

		Expose ()
		{
		}

		public static Expose FromObject (object value)
		{
			if (value is Expose)
				return value as Expose;

			return new Expose ()
			{
				Value = value,
				Type = ReferenceEquals (value, null) ? typeof(object) : value.GetType (),
				Flag = k_InstanceMemberFlags,
			};
		}

		public static Expose FromType (Type type, params Type [] genericTypes)
		{
			if (type.IsGenericTypeDefinition && 0 < genericTypes.Length)
				type = type.MakeGenericType (genericTypes);

			return type == null ? null : new Expose ()
			{
				Value = null,
				Type = type,
				Flag = k_StaticMemberFlags,
			};
		}

		public Expose New (params object [] args)
		{
			args = args.Select (x => x is Expose ? ((Expose)x).Value : x).ToArray ();
			Type [] argTypes = args.Length == 0 ? Type.EmptyTypes : args.Select (x => x == null ? typeof (object) : x.GetType ()).ToArray ();

			ConstructorInfo constructorInfo = Type.GetConstructor (BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, argTypes, null);
			if (constructorInfo != null)
			{
				return FromObject (constructorInfo.Invoke (args));
			}

			throw new MissingMemberException (Type.FullName, string.Format (".ctor({0})", string.Join (", ", Array.ConvertAll (argTypes, t => t.FullName))));
		}

		public T As<T> ()
		{
			return (T)Value;
		}

		public Expose Get (string memberName)
		{
			var pi = Type.GetProperty (memberName, Flag | BindingFlags.GetProperty);
			if (pi != null)
			{
				return Expose.FromObject (pi.GetValue (Value));
			}
			var fi = Type.GetField (memberName, Flag | BindingFlags.GetField);
			if (fi != null)
			{
				return Expose.FromObject (fi.GetValue (Value));
			}

			// Get with indexer
			try
			{
				return Get (new [] { memberName });
			}
			catch
			{
				throw new MissingMemberException (Type.Name, memberName);
			}
		}

		public Expose Get (object index)
		{
			return Get (new [] { index });
		}

		public Expose Get (object [] index)
		{
			var pi = Type.GetProperty (Type.GetCustomAttribute<DefaultMemberAttribute> ().MemberName, index.Select (x => x.GetType ()).ToArray ());
			if (pi != null)
			{
				return Expose.FromObject (pi.GetValue (Value, index));
			}

			throw new MissingMemberException (Type.Name, "Indexer");
		}

		public void Set (string memberName, object value)
		{
			if (value is Expose)
				value = ((Expose)value).Value;

			var pi = Type.GetProperty (memberName, Flag | BindingFlags.SetProperty);
			if (pi != null)
			{
				pi.SetValue (Value, value);
				return;
			}
			var fi = Type.GetField (memberName, Flag | BindingFlags.SetField);
			if (fi != null)
			{
				fi.SetValue (Value, value);
				return;
			}

			// Set with indexer
			try
			{
				Set (new [] { memberName }, value);
			}
			catch
			{
				throw new MissingMemberException (Type.Name, memberName);
			}
		}

		public void Set (object index, object value)
		{
			Set (new [] { index }, value);
		}

		public void Set (object [] index, object value)
		{
			if (value is Expose)
				value = ((Expose)value).Value;

			var pi = Type.GetProperty (Type.GetCustomAttribute<DefaultMemberAttribute> ().MemberName, index.Select (x => x.GetType ()).ToArray ());
			if (pi != null)
			{
				pi.SetValue (Value, value, index);
			}

			throw new MissingMemberException (Type.Name, "Indexer");
		}

		public Expose Call<T0> (string methodName, params object [] args)
		{
			return Call (new [] { typeof (T0) }, methodName, args);
		}

		public Expose Call<T0, T1> (string methodName, params object [] args)
		{
			return Call (new [] { typeof (T0), typeof (T1) }, methodName, args);
		}

		public Expose Call<T0, T1, T2> (string methodName, params object [] args)
		{
			return Call (new [] { typeof (T0), typeof (T1), typeof (T2) }, methodName, args);
		}

		public Expose Call<T0, T1, T2, T3> (string methodName, params object [] args)
		{
			return Call (new [] { typeof (T0), typeof (T1), typeof (T2), typeof (T3) }, methodName, args);
		}


		public Expose Call (Type [] genericTypes, string methodName, params object [] args)
		{
			args = args.Select (x => x is Expose ? ((Expose)x).Value : x).ToArray ();

			// Find method
			if (TryInvoke (methodName, Value, Type.GetMethods (Flag | BindingFlags.InvokeMethod), genericTypes, args, out Expose result))
			{
				return result;
			}

			args = new [] { Value }.Concat (args).ToArray ();
			if (Value != null && TryInvoke (methodName, null, s_extensionMethods.Where (x => Is (Type, x.Key)).SelectMany (x => x.Value), genericTypes, args, out result))
			{
				return result;
			}

			throw new MissingMemberException (Type.Name, methodName);
		}


		public Expose Call (string methodName, params object [] args)
		{
			args = args.Select (x => x is Expose ? ((Expose)x).Value : x).ToArray ();

			// Find method
			if (TryInvoke (methodName, Value, Type.GetMethods (Flag | BindingFlags.InvokeMethod), args, out Expose result))
			{
				return result;
			}

			// Find event
			var fi = Type.GetField (methodName, Flag | BindingFlags.SetField);
			if (fi != null)
			{
				var del = fi.GetValue (Value) as MulticastDelegate;
				if (del != null && IsInvokable (del.Method, args))
				{
					object lastResult = null;
					foreach (var h in del.GetInvocationList ())
					{
						lastResult = h.Method.Invoke (h.Target, args);
					}
					return Expose.FromObject (lastResult);
				}
			}

			// Find operator method
			args = new [] { Value }.Concat (args).ToArray ();
			if (TryInvoke (methodName, null, Type.GetMethods (Flag | BindingFlags.InvokeMethod), args, out result))
			{
				return result;
			}

			// Find extension method
			if (Value != null
				&& TryInvoke (methodName, null, s_extensionMethods.Where (x => Is (Type, x.Key)).SelectMany (x => x.Value), null, args, out result))
			{
				if (
					TryInvoke (methodName, null, s_extensionMethods.Where (x => Is (Type, x.Key)).SelectMany (x => x.Value), null, args, out result)
					|| TryInvoke (methodName, null, s_extensionMethods.Where (x => Is (Type, x.Key)).SelectMany (x => x.Value), new [] { Type }, args, out result)
				)
					return result;
			}

			throw new MissingMemberException (Type.Name, methodName);
		}



		public static Type GetType (string name)
		{
			Type ret = Type.GetType (name);

			if (ret == null && s_TypeByName.TryGetValue (name, out ret))
			{
				return ret ?? throw new AmbiguousMatchException (string.Format ("Type '{0}' is ambigous. Use full name or assembly qualified name.", name));
			}

			return ret;
		}

		//public static Type GetType<T>(params Type[] genericTypes)
		//{
		//	return GetType(new [] { typeof(T) }.Concat( genericTypes).ToArray());
		//}

		//public static Type GetType<T>(params string[] genericTypes)
		//{
		//return GetType(new [] { typeof(T).AssemblyQualifiedName }.Concat( genericTypes).ToArray());
		//}

		public static Type GetType (params string [] genericTypes)
		{
			return GetType (genericTypes.Select (x => GetType (x)).ToArray ());
		}

		public static Type GetType (params Type [] genericTypes)
		{
			if (genericTypes.Length == 0)
			{
				throw new ArgumentException ("Requires one or more Type objects", "genericTypes");
			}
			else if (genericTypes.Length == 1)
			{
				if (!genericTypes [0].IsGenericTypeDefinition)
				{
					return genericTypes [0];
				}
				throw new ArgumentException ("Missing generic type parameter", "genericTypes");
			}
			else
			{
				if (genericTypes [0].IsGenericTypeDefinition)
				{
					return genericTypes [0].MakeGenericType (genericTypes.Skip (1).ToArray ());
				}
				throw new ArgumentException ("Missing generic type definition", "genericTypes");
			}
		}


		static Type ToBaseClass (Type type)
		{
			if (!type.IsGenericType || !type.IsConstructedGenericType || type.IsGenericTypeDefinition)
				return type;

			if (type.GenericTypeArguments [0].IsGenericParameter)
				return type.GetGenericTypeDefinition ();

			return type;
		}

		static bool TryInvoke (string methodName, object instance, IEnumerable<MethodInfo> methodInfos, object [] args, out Expose result)
		{
			var methodInfo = methodInfos.FirstOrDefault (x => x.Name == methodName && IsInvokable (x, args));

			if (methodInfo != null)
			{
				result = Expose.FromObject (methodInfo.Invoke (instance, args));
				return true;
			}
			result = null;
			return false;
		}

		static bool TryInvoke (string methodName, object instance, IEnumerable<MethodInfo> methodInfos, Type [] genericTypes, object [] args, out Expose result)
		{
			bool isGeneric = genericTypes != null && 0 < genericTypes.Length;
			foreach (MethodInfo methodInfo in methodInfos)
			{
				MethodInfo mi = methodInfo;
				if (mi.Name != methodName || isGeneric && !mi.IsGenericMethod)
					continue;

				try
				{
					if (isGeneric)
					{
						mi = mi.GetGenericMethodDefinition ().MakeGenericMethod (genericTypes);
					}

					if (IsInvokable (mi, args))
					{
						result = Expose.FromObject (mi.Invoke (instance, args));
						return true;
					}
				}
				catch
				{
				}
			}
			result = null;
			return false;
		}

		static bool IsInvokable (MethodInfo methodInfo, object [] args)
		{
			return IsAssignableFrom (
				methodInfo.GetParameters ().Select (x => x.ParameterType),
				args.Select (x => x == null ? typeof (object) : x.GetType ())
			);
		}

		static bool IsAssignableFrom (IEnumerable<Type> expectedTypes, IEnumerable<Type> actualTypes)
		{
			return actualTypes
				.Zip (expectedTypes, (actual, expected) => new { actual, expected })
				.All (x => Is (x.actual, x.expected));

		}

		public override string ToString ()
		{
			return !ReferenceEquals (Value, null) ? Value.ToString () :
				!ReferenceEquals (Type, null) ? Type.ToString () :
				"null";
		}

		public static bool Equals (Expose left, Expose right)
		{
			object lv = ReferenceEquals (left, null) ? null : left.Value;
			object rv = ReferenceEquals (right, null) ? null : right.Value;
			return ReferenceEquals (lv, null) ? ReferenceEquals (rv, null) : Expose.Equals (left, rv);
		}

		public static bool Equals (Expose left, object right)
		{
			try
			{
				return (bool)left.Call ("op_Equality", right).Value;
			}
			catch
			{ }

			try
			{
				return ToDecimal (left) == ToDecimal (right);
			}
			catch
			{ }

			try
			{
				return (bool)left.Call ("Equals", right).Value;
			}
			catch
			{ }

			object lv = ReferenceEquals (left, null) ? null : left.Value;
			object rv = right;

			return ReferenceEquals (lv, null) ? ReferenceEquals (rv, null) : lv.Equals (rv);
		}

		public static decimal ToDecimal (object v)
		{
			if (v is Expose)
				v = ((Expose)v).Value;
			if (IsNumber (v))
				return decimal.Parse (v.ToString ());

			throw new InvalidCastException (string.Format ("{0} ({1}) can not cast to {2}.", v != null ? v.ToString () : "null", v != null ? v.GetType () : typeof (object), typeof (decimal)));
		}

		public static int ToInt (object v)
		{
			if (v is Expose)
				v = ((Expose)v).Value;
			if (IsNumber (v))
				return int.Parse (v.ToString ());

			throw new InvalidCastException (string.Format ("{0} ({1}) can not cast to {2}.", v != null ? v.ToString () : "null", v != null ? v.GetType () : typeof (object), typeof (decimal)));
		}

		public static bool IsNumber (object v)
		{
			if (v is Expose)
				v = ((Expose)v).Value;
			return v is sbyte || v is byte || v is short || v is ushort || v is int || v is uint || v is long || v is ulong || v is float || v is double || v is decimal;
		}

		public override bool Equals (object other)
		{
			return Equals (this, other);
		}

		public bool Equals (Expose other)
		{
			return Equals (this, other);
		}

		public override int GetHashCode ()
		{
			return Value != null ? Value.GetHashCode () : 0;
		}

		public static int Compare (Expose left, Expose right)
		{
			return Compare_Impl (
				ReferenceEquals (left, null) ? null : left.Value,
				ReferenceEquals (right, null) ? null : right.Value);
		}

		public static int Compare (Expose left, object right)
		{
			return Compare_Impl (ReferenceEquals (left, null) ? null : left.Value, right);
		}

		public static int Compare_Impl (object lv, object rv)
		{
			if (ReferenceEquals (lv, null))
				return ReferenceEquals (rv, null) ? 0 : -1;

			if (ReferenceEquals (rv, null))
				return 1;

			var c = lv as IComparable;
			return ReferenceEquals (c, null) ? -1 : c.CompareTo (rv);
		}

		public static bool operator == (Expose left, Expose right)
		{
			return Expose.Equals (left, right);
		}

		public static bool operator == (Expose left, object right)
		{
			return Expose.Equals (left, right);
		}

		public static bool operator == (object left, Expose right)
		{
			return Expose.Equals (right, left);
		}

		public static bool operator != (Expose left, Expose right)
		{
			return !Expose.Equals (left, right);
		}

		public static bool operator != (Expose left, object right)
		{
			return !Expose.Equals (left, right);
		}

		public static bool operator != (object left, Expose right)
		{
			return !Expose.Equals (right, left);
		}

		public static bool operator > (Expose left, Expose right)
		{
			return GreaterThan (left, right.Value);
		}
		public static bool operator > (Expose left, object right)
		{
			return GreaterThan (left, right);
		}
		public static bool operator > (object left, Expose right)
		{
			return !GreaterThan (right, left);
		}

		public static bool operator < (Expose left, Expose right)
		{
			return LessThan (left, right.Value);
		}
		public static bool operator < (Expose left, object right)
		{
			return LessThan (left, right);
		}
		public static bool operator < (object left, Expose right)
		{
			return !LessThan (right, left);
		}


		public static bool operator >= (Expose left, Expose right)
		{
			return !LessThan (left, right.Value);
		}
		public static bool operator >= (Expose left, object right)
		{
			return !LessThan (left, right);
		}
		public static bool operator >= (object left, Expose right)
		{
			return LessThan (right, left);
		}

		public static bool operator <= (Expose left, Expose right)
		{
			return !GreaterThan (left, right.Value);
		}
		public static bool operator <= (Expose left, object right)
		{
			return GreaterThan (left, right);
		}
		public static bool operator <= (object left, Expose right)
		{
			return GreaterThan (right, left);
		}

		static bool GreaterThan (Expose left, object right)
		{
			try
			{
				return (bool)Expose.FromType (left.Type).Call ("op_GreaterThan", left.Value, right).Value;
			}
			catch
			{ }

			try
			{
				return ToDecimal (left) > ToDecimal (right);
			}
			catch
			{
				return Expose.Compare (left, right) > 0;
			}
		}

		static bool LessThan (Expose left, object right)
		{
			try
			{
				return (bool)Expose.FromType (left.Type).Call ("op_LessThan", left.Value, right).Value;
			}
			catch
			{ }

			try
			{
				return ToDecimal (left) < ToDecimal (right);
			}
			catch
			{
				return Expose.Compare (left, right) < 0;
			}
		}

		static Expose Addition (Expose left, object right)
		{
			try
			{
				if (IsNumber (left) && IsNumber (right))
				{
					var d = ToDecimal (left) + ToDecimal (right);
					return Expose.FromObject (ToDecimal (left) + ToDecimal (right));
				}
				return Expose.FromType (left.Type).Call ("op_Addition", left.Value, right);
			}
			catch
			{
			}

			object lv = ReferenceEquals (left, null) ? null : left.Value;
			if (lv is string || right is string)
			{
				string ls = ReferenceEquals (lv, null) ? "" : lv.ToString ();
				string rs = ReferenceEquals (right, null) ? "" : right.ToString ();
				return Expose.FromObject (ls + rs);
			}

			try
			{
				return Expose.FromObject (ToDecimal (left) + ToDecimal (right));
			}
			catch
			{

			}
			return null;
		}

		static Expose Subtraction (Expose left, object right)
		{
			try
			{
				return Expose.FromType (left.Type).Call ("op_Subtraction", left.Value, right);
			}
			catch
			{ }

			try
			{
				return Expose.FromObject (ToDecimal (left) - ToDecimal (right));
			}
			catch
			{ }

			return null;
		}

		enum Operation
		{
			op_Equality,
			op_Inequality,
			op_GreaterThan,
			op_LessThan,
			op_GreaterThanOrEqual,
			op_LessThanOrEqual,
			op_BitwiseAnd,
			op_BitwiseOr,
			op_Addition,
			op_Subtraction,
			op_Division,
			op_Modulus,
			op_Multiply,
			op_LeftShift,
			op_RightShift,
			op_ExclusiveOr,
			op_UnaryNegation,
			op_UnaryPlus,
			op_LogicalNot,
			op_OnesComplement,
			op_False,
			op_True,
			op_Increment,
			op_Decrement,
		}

		static Expose DoOperation (Expose left, object right, Operation op)
		{
			try
			{
				return Expose.FromType (left.Type).Call (op.ToString (), left.Value, right);
			}
			catch
			{ }

			try
			{
				switch (op)
				{
					case Operation.op_Addition:
						return Expose.FromObject (ToDecimal (left) + ToDecimal (right));
					case Operation.op_Subtraction:
						return Expose.FromObject (ToDecimal (left) - ToDecimal (right));
					case Operation.op_Equality:
						return Expose.FromObject (ToDecimal (left) == ToDecimal (right));
					case Operation.op_Inequality:
						return Expose.FromObject (ToDecimal (left) != ToDecimal (right));
					case Operation.op_GreaterThan:
						return Expose.FromObject (ToDecimal (left) > ToDecimal (right));
					case Operation.op_LessThan:
						return Expose.FromObject (ToDecimal (left) < ToDecimal (right));
					case Operation.op_GreaterThanOrEqual:
						return Expose.FromObject (ToDecimal (left) <= ToDecimal (right));
					case Operation.op_LessThanOrEqual:
						return Expose.FromObject (ToDecimal (left) >= ToDecimal (right));
					case Operation.op_BitwiseAnd:
						return Expose.FromObject (ToInt (left) & ToInt (right));
					case Operation.op_BitwiseOr:
						return Expose.FromObject (ToInt (left) | ToInt (right));
					case Operation.op_Division:
						return Expose.FromObject (ToDecimal (left) / ToDecimal (right));
					case Operation.op_Modulus:
						return Expose.FromObject (ToDecimal (left) % ToDecimal (right));
					case Operation.op_Multiply:
						return Expose.FromObject (ToDecimal (left) * ToDecimal (right));
					case Operation.op_LeftShift:
						return Expose.FromObject (ToInt (left) << ToInt (right));
					case Operation.op_RightShift:
						return Expose.FromObject (ToInt (left) >> ToInt (right));
					case Operation.op_ExclusiveOr:
						return Expose.FromObject (ToInt (left) ^ ToInt (right));
					case Operation.op_UnaryNegation:
						break;
					case Operation.op_UnaryPlus:
						break;
					case Operation.op_LogicalNot:
						break;
					case Operation.op_OnesComplement:
						break;
					case Operation.op_False:
						break;
					case Operation.op_True:
						break;
					case Operation.op_Increment:
						break;
					case Operation.op_Decrement:
						break;
				}
			}
			catch
			{ }

			return null;
		}

		public IEnumerator GetEnumerator ()
		{
			if (Value is IEnumerable)
			{
				foreach (object o in Value as IEnumerable)
				{
					yield return Expose.FromObject (o);
				}
			}
			yield break;
		}

		public static Expose operator + (Expose left, Expose right)
		{
			return Expose.Addition (left, right.Value);
		}

		public static Expose operator + (Expose left, object right)
		{
			return Addition (left, right);
		}

		public static Expose operator + (object left, Expose right)
		{
			return Addition (Expose.FromObject (left), right.Value);
		}

		public static Expose operator - (Expose left, Expose right)
		{
			return Expose.Subtraction (left, right.Value);
		}

		public static Expose operator - (Expose left, object right)
		{
			return Subtraction (left, right);
		}

		public static Expose operator - (object left, Expose right)
		{
			return Subtraction (Expose.FromObject (left), right.Value);
		}

		public Expose this [string i]
		{
			set { Set (i, value); }
			get { return Get (i); }
		}

		public Expose this [object i]
		{
			set { Set (i, value); }
			get { return Get (i); }
		}

		public Expose this [params object [] i]
		{
			set { Set (i, value); }
			get { return Get (i); }
		}

		public static bool Is (Type actual, Type expected)
		{
			if (expected.IsAssignableFrom (actual))
				return true;

			var interfaceTypes = actual.GetInterfaces ();

			foreach (var it in interfaceTypes)
			{
				if (it.IsGenericType && it.GetGenericTypeDefinition () == expected)
					return true;
			}

			if (actual.IsGenericType && actual.GetGenericTypeDefinition () == expected)
				return true;

			Type baseType = actual.BaseType;
			if (baseType == null) return false;

			return Is (baseType, expected);
		}
	}
}