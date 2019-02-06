using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace QueryInterception
{
    public static class TypeHelper
    {
        public static Type FindIEnumerable(Type seqType)
        {
            Type ienum;
            Type type;
            Type[] elementType;
            Type[] genericArguments;
            int num;
            if (!(seqType == null ? false : !(seqType == typeof(string))))
            {
                type = null;
            }
            else if (!seqType.IsArray)
            {
                if (seqType.IsGenericType)
                {
                    genericArguments = seqType.GetGenericArguments();
                    num = 0;
                    while (num < genericArguments.Length)
                    {
                        Type arg = genericArguments[num];
                        Type type1 = typeof(IEnumerable<>);
                        elementType = new Type[] { arg };
                        ienum = type1.MakeGenericType(elementType);
                        if (!ienum.IsAssignableFrom(seqType))
                        {
                            num++;
                        }
                        else
                        {
                            type = ienum;
                            return type;
                        }
                    }
                }
                Type[] ifaces = seqType.GetInterfaces();
                if ((ifaces == null ? false : ifaces.Length > 0))
                {
                    genericArguments = ifaces;
                    num = 0;
                    while (num < genericArguments.Length)
                    {
                        ienum = TypeHelper.FindIEnumerable(genericArguments[num]);
                        if (!(ienum != null))
                        {
                            num++;
                        }
                        else
                        {
                            type = ienum;
                            return type;
                        }
                    }
                }
                if ((seqType.BaseType == null ? true : !(seqType.BaseType != typeof(object))))
                {
                    type = null;
                }
                else
                {
                    type = TypeHelper.FindIEnumerable(seqType.BaseType);
                }
            }
            else
            {
                Type type2 = typeof(IEnumerable<>);
                elementType = new Type[] { seqType.GetElementType() };
                type = type2.MakeGenericType(elementType);
            }
            return type;
        }

        public static object GetDefault(Type type)
        {
            return ((!type.IsValueType ? true : TypeHelper.IsNullableType(type)) ? null : Activator.CreateInstance(type));
        }

        public static Type GetElementType(Type seqType)
        {
            Type type;
            Type ienum = TypeHelper.FindIEnumerable(seqType);
            type = (!(ienum == null) ? ienum.GetGenericArguments()[0] : seqType);
            return type;
        }

        public static Type GetMemberType(MemberInfo mi)
        {
            Type returnType;
            FieldInfo fi = mi as FieldInfo;
            if (!(fi != null))
            {
                PropertyInfo pi = mi as PropertyInfo;
                if (!(pi != null))
                {
                    EventInfo ei = mi as EventInfo;
                    if (!(ei != null))
                    {
                        MethodInfo meth = mi as MethodInfo;
                        if (!(meth != null))
                        {
                            returnType = null;
                        }
                        else
                        {
                            returnType = meth.ReturnType;
                        }
                    }
                    else
                    {
                        returnType = ei.EventHandlerType;
                    }
                }
                else
                {
                    returnType = pi.PropertyType;
                }
            }
            else
            {
                returnType = fi.FieldType;
            }
            return returnType;
        }

        public static Type GetNonNullableType(Type type)
        {
            Type type1;
            type1 = (!TypeHelper.IsNullableType(type) ? type : type.GetGenericArguments()[0]);
            return type1;
        }

        public static Type GetNullAssignableType(Type type)
        {
            Type type1;
            type1 = (TypeHelper.IsNullAssignable(type) ? type : typeof(Nullable<>).MakeGenericType(new Type[] { type }));
            return type1;
        }

        public static ConstantExpression GetNullConstant(Type type)
        {
            return Expression.Constant(null, TypeHelper.GetNullAssignableType(type));
        }

        public static Type GetSequenceType(Type elementType)
        {
            return typeof(IEnumerable<>).MakeGenericType(new Type[] { elementType });
        }

        public static bool IsInteger(Type type)
        {
            bool flag;
            TypeHelper.GetNonNullableType(type);
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    {
                        flag = true;
                        break;
                    }
                default:
                    {
                        flag = false;
                        break;
                    }
            }
            return flag;
        }

        public static bool IsNullableType(Type type)
        {
            return (!(type != null) || !type.IsGenericType ? false : type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        public static bool IsNullAssignable(Type type)
        {
            return (!type.IsValueType ? true : TypeHelper.IsNullableType(type));
        }

        public static bool IsReadOnly(MemberInfo member)
        {
            bool attributes;
            MemberTypes memberType = member.MemberType;
            if (memberType == MemberTypes.Field)
            {
                attributes = (((FieldInfo)member).Attributes & FieldAttributes.InitOnly) != FieldAttributes.PrivateScope;
            }
            else if (memberType == MemberTypes.Property)
            {
                PropertyInfo pi = (PropertyInfo)member;
                attributes = (!pi.CanWrite ? true : pi.GetSetMethod() == null);
            }
            else
            {
                attributes = true;
            }
            return attributes;
        }
    }
}