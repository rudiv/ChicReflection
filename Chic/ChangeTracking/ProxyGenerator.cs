using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Chic.ChangeTracking
{
    public class ProxyGenerator
    {
        static readonly ConcurrentDictionary<Type, Type> ProxyCache = new ConcurrentDictionary<Type, Type>();

        protected AssemblyBuilder GetBuilder(string name)
        {
            return AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(name), AssemblyBuilderAccess.Run);
        }

        public T GetProxy<T>()
        {
            var tType = typeof(T);

            if (!ProxyCache.TryGetValue(tType, out Type proxyType))
            {
                var asmBuilder = GetBuilder(tType.Name);
                var modBuilder = asmBuilder.DefineDynamicModule($"ChicProxies.{tType.Name}");
                var typeBuilder = default(TypeBuilder);
                if (tType.IsInterface)
                {
                    typeBuilder = modBuilder.DefineType($"{tType.Name}_{Guid.NewGuid()}", TypeAttributes.Public | TypeAttributes.Class);
                    typeBuilder.AddInterfaceImplementation(tType);
                } else
                {
                    typeBuilder = modBuilder.DefineType($"{tType.Name}_{Guid.NewGuid()}", TypeAttributes.Public | TypeAttributes.Class, tType);
                }
                typeBuilder.AddInterfaceImplementation(typeof(IProxyChanges));

                CreateBasicProperty<bool>(typeBuilder, "IsModified", out _, out MethodInfo dirtySetter);
                var origValuesProp = CreateBasicProperty<Dictionary<string, object>>(typeBuilder, "OriginalValues", out MethodInfo origGetter, out MethodInfo origSetter);

                /*
                IL_0000: ldarg.0
			    IL_0001: call instance class [mscorlib]System.Collections.Generic.Dictionary`2<string, object> Program/Proxied::get_OriginalValues()
			    IL_0006: brtrue.s IL_0013
			    IL_0008: ldarg.0
			    IL_0009: newobj instance void class [mscorlib]System.Collections.Generic.Dictionary`2<string, object>::.ctor()
			    IL_000e: call instance void Program/Proxied::set_OriginalValues(class [mscorlib]System.Collections.Generic.Dictionary`2<string, object>)
			    IL_0013: ret
                */
                var ensureTrackableMethod = typeBuilder.DefineMethod("EnsureTrackable", MethodAttributes.Private | MethodAttributes.HideBySig);
                var etIl = ensureTrackableMethod.GetILGenerator();
                var retLbl = etIl.DefineLabel();
                etIl.Emit(OpCodes.Ldarg_0);
                etIl.EmitCall(OpCodes.Call, origGetter, null);
                etIl.Emit(OpCodes.Brtrue_S, retLbl);
                etIl.Emit(OpCodes.Ldarg_0);
                etIl.Emit(OpCodes.Newobj, typeof(Dictionary<string, object>).GetConstructor(Type.EmptyTypes));
                etIl.Emit(OpCodes.Call, origSetter);
                etIl.MarkLabel(retLbl);
                etIl.Emit(OpCodes.Ret);

                // Track all virtual properties in the object
                var properties = tType.GetProperties();
                if (!tType.IsInterface)
                {
                    properties = properties.Where(p => p.GetSetMethod().IsVirtual).ToArray();
                }
                foreach (var prop in properties)
                {
                    CreateOverriddenMagicProperty<T>(typeBuilder, prop.Name, prop.PropertyType, dirtySetter, ensureTrackableMethod, origGetter);
                }

                proxyType = typeBuilder.CreateTypeInfo().AsType();

                ProxyCache.TryAdd(tType, proxyType);
            }

            return (T)Activator.CreateInstance(proxyType);
        }

        protected PropertyInfo CreateBasicProperty<TPropertyType>(TypeBuilder typeBuilder, string propName, out MethodInfo getter, out MethodInfo setter)
        {
            /*
		    .field private bool _isDirty
		    .method public hidebysig specialname 
			    instance bool get_IsDirty () cil managed 
		    {
			    IL_0000: ldarg.0
			    IL_0001: ldfld bool Program/Proxied::_isDirty
			    IL_0006: ret
		    }

		    .method public hidebysig specialname 
			    instance void set_IsDirty (
				    bool 'value'
			    ) cil managed 
		    {
			    IL_0000: ldarg.0
			    IL_0001: ldarg.1
			    IL_0002: stfld bool Program/Proxied::_isDirty
			    IL_0007: ret
		    }
            */
            var tPropType = typeof(TPropertyType);
            var tPropArgs = new[] { tPropType };
            var fieldName = $"_{propName}";
            var getterName = $"get_{propName}";
            var setterName = $"set_{propName}";
            var field = typeBuilder.DefineField(fieldName, tPropType, FieldAttributes.Private);
            var prop = typeBuilder.DefineProperty(propName, PropertyAttributes.None, tPropType, tPropArgs);

            const MethodAttributes attrs = MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.SpecialName | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig;

            var getterBuilder = typeBuilder.DefineMethod(getterName, attrs, tPropType, Type.EmptyTypes);
            var getterIl = getterBuilder.GetILGenerator();
            getterIl.Emit(OpCodes.Ldarg_0);
            getterIl.Emit(OpCodes.Ldfld, field);
            getterIl.Emit(OpCodes.Ret);

            var setterBuilder = typeBuilder.DefineMethod(setterName, attrs, null, tPropArgs);
            var setterIl = setterBuilder.GetILGenerator();
            setterIl.Emit(OpCodes.Ldarg_0);
            setterIl.Emit(OpCodes.Ldarg_1);
            setterIl.Emit(OpCodes.Stfld, field);
            setterIl.Emit(OpCodes.Ret);

            prop.SetGetMethod(getterBuilder);
            prop.SetSetMethod(setterBuilder);

            typeBuilder.DefineMethodOverride(getterBuilder, typeof(IProxyChanges).GetMethod(getterName));
            typeBuilder.DefineMethodOverride(setterBuilder, typeof(IProxyChanges).GetMethod(setterName));

            getter = getterBuilder;
            setter = setterBuilder;

            return prop;
        }

        protected void CreateOverriddenMagicProperty<T>(TypeBuilder typeBuilder, string propName, Type tPropType, MethodInfo dirtySetter, MethodInfo ensureTrackable, MethodInfo ovGetter)
        {
            var tPropArgs = new[] { tPropType };
            var fieldName = $"_{propName}";
            var getterName = $"get_{propName}";
            var setterName = $"set_{propName}";
            var field = typeBuilder.DefineField(fieldName, tPropType, FieldAttributes.Private);
            
            const MethodAttributes attrs = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.HideBySig;

            var getterBuilder = typeBuilder.DefineMethod(getterName, attrs, tPropType, Type.EmptyTypes);
            var getterIl = getterBuilder.GetILGenerator();
            getterIl.Emit(OpCodes.Ldarg_0);
            getterIl.Emit(OpCodes.Ldfld, field);
            getterIl.Emit(OpCodes.Ret);

            /* hehe
            IL_0000: ldarg.0
			IL_0001: call instance void Program/Proxied::EnsureTrackable()
			IL_0006: ldarg.0
			IL_0007: call instance class [mscorlib]System.Collections.Generic.Dictionary`2<string, object> Program/Proxied::get_OriginalValues()
			IL_000c: ldstr "Field"
			IL_0011: callvirt instance bool class [mscorlib]System.Collections.Generic.Dictionary`2<string, object>::ContainsKey(!0)
			IL_0016: brtrue.s IL_0030
			IL_0018: ldarg.0
			IL_0019: call instance class [mscorlib]System.Collections.Generic.Dictionary`2<string, object> Program/Proxied::get_OriginalValues()
			IL_001e: ldstr "Field"
			IL_0023: ldarg.1
			IL_0024: box [mscorlib]System.Int32
			IL_0029: callvirt instance void class [mscorlib]System.Collections.Generic.Dictionary`2<string, object>::Add(!0, !1)
			IL_002e: br.s IL_0040
			IL_0030: ldarg.0
			IL_0031: ldfld int32 Program/Proxied::_field
			IL_0036: ldarg.1
			IL_0037: beq.s IL_0040
			IL_0039: ldarg.0
			IL_003a: ldc.i4.1
			IL_003b: call instance void Program/Proxied::set_IsDirty(bool)
			IL_0040: ldarg.0
			IL_0041: ldarg.1
			IL_0042: stfld int32 Program/Proxied::_field
			IL_0047: ret
            */

            var containsKeyDictMethod = typeof(Dictionary<string, object>).GetMethod("ContainsKey");
            var addDictMethod = typeof(Dictionary<string, object>).GetMethod("Add");

            var setterBuilder = typeBuilder.DefineMethod(setterName, attrs, null, tPropArgs);
            var setterIl = setterBuilder.GetILGenerator();
            var neqLbl = setterIl.DefineLabel();
            var setFieldLbl = setterIl.DefineLabel();
            setterIl.Emit(OpCodes.Ldarg_0);
            setterIl.Emit(OpCodes.Call, ensureTrackable);
            setterIl.Emit(OpCodes.Ldarg_0);
            setterIl.Emit(OpCodes.Call, ovGetter);
            setterIl.Emit(OpCodes.Ldstr, propName);
            setterIl.Emit(OpCodes.Callvirt, containsKeyDictMethod);
            setterIl.Emit(OpCodes.Brtrue_S, neqLbl); // jmp if not first set
            setterIl.Emit(OpCodes.Ldarg_0);
            setterIl.Emit(OpCodes.Call, ovGetter);
            setterIl.Emit(OpCodes.Ldstr, propName);
            setterIl.Emit(OpCodes.Ldarg_1);
            // Box if needed
            if (tPropType.IsValueType) {
                setterIl.Emit(OpCodes.Box, tPropType);
            }
            setterIl.Emit(OpCodes.Callvirt, addDictMethod);
            setterIl.Emit(OpCodes.Br_S, setFieldLbl); // jmp to skip dirty
            setterIl.MarkLabel(neqLbl);
            setterIl.Emit(OpCodes.Ldarg_0);
            setterIl.Emit(OpCodes.Ldfld, field);
            setterIl.Emit(OpCodes.Ldarg_1);
            setterIl.Emit(OpCodes.Beq_S, setFieldLbl); // jmp if values are equal
            setterIl.Emit(OpCodes.Ldarg_0);
            setterIl.Emit(OpCodes.Ldc_I4_1);
            setterIl.Emit(OpCodes.Call, dirtySetter);
            setterIl.MarkLabel(setFieldLbl);
            setterIl.Emit(OpCodes.Ldarg_0);
            setterIl.Emit(OpCodes.Ldarg_1);
            setterIl.Emit(OpCodes.Stfld, field);
            setterIl.Emit(OpCodes.Ret);
            
            if (typeof(T).IsInterface)
            {
                var prop = typeBuilder.DefineProperty(propName, PropertyAttributes.None, tPropType, tPropArgs);
                prop.SetGetMethod(getterBuilder);
                prop.SetSetMethod(setterBuilder);

                typeBuilder.DefineMethodOverride(getterBuilder, typeof(T).GetMethod(getterName));
                typeBuilder.DefineMethodOverride(setterBuilder, typeof(T).GetMethod(setterName));
            }
            else
            {
                typeBuilder.DefineMethodOverride(getterBuilder, typeBuilder.BaseType.GetMethod(getterName));
                typeBuilder.DefineMethodOverride(setterBuilder, typeBuilder.BaseType.GetMethod(setterName));
            }

            //return prop;
        }
    }
}
