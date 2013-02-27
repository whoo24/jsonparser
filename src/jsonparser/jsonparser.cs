using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace jsonparser
{
    class jsonparser
    {
        object parse<T>(string protocol) where T : new()
        {
            Type protocol_type = typeof(T);

            T response = new T();

            foreach (System.Reflection.FieldInfo field in protocol_type.GetFields())
            {
                Type fieldtype = field.FieldType;

                try
                {
                    if (fieldtype.IsGenericType)
                    {
                        string typename = fieldtype.GetGenericTypeDefinition().Name;
                        string generictype = typename.Remove(typename.IndexOf('`'));

                        Type[] generic_args = fieldtype.GetGenericArguments();
                        StringBuilder generic_args_typename = new StringBuilder();
                        foreach (Type t in generic_args)
                        {
                            generic_args_typename.AppendFormat("[{0}]", t.Name);
                        }

                        try
                        {
                            if (generictype == "List")
                            {
                                System.Object list = fieldtype.GetGenericTypeDefinition().MakeGenericType(generic_args).GetConstructor(Type.EmptyTypes).Invoke(null);
                                field.SetValue(response, list);

                                foreach (JSONNode childData in Data[field.Name].Childs)
                                {
                                    Type generic_arg = generic_args[0];

                                    if (generic_arg.Name == "String")
                                    {
                                        String a = "";
                                        list.GetType().GetMethod("Add").Invoke(list, new[] { a });
                                    }
                                    else
                                    {
                                        System.Object a = generic_arg.GetConstructor(Type.EmptyTypes).Invoke(null);

                                        foreach (System.Reflection.FieldInfo memberinfo in generic_arg.GetFields())
                                        {
                                            try
                                            {
                                                decodeField(childData, memberinfo, a);
                                            }
                                            catch (KeyNotFoundException)
                                            {
                                                Debug.LogWarning(string.Format("Type({1}): 구조체에는 필드({0})가 있는데 네트워크 데이터에는 값이 없다. 구조체 이름 대소문자 확인하고 값이 할당 됐는지 확인할 것", memberinfo.Name, protocol_type.Name));
                                            }

                                        }
                                        list.GetType().GetMethod("Add").Invoke(list, new[] { a });
                                    }
                                }
                            }
                            else
                            {
                                typename = generictype + generic_args_typename.ToString();
                                throw new PacketParserNotFound(typename);
                            }
                        }
                        catch (KeyNotFoundException)
                        {
                            Debug.LogWarning(string.Format("Type({1}): 구조체에는 필드({0})가 있는데 네트워크 데이터에는 값이 없다. 구조체 이름 대소문자 확인하고 값이 할당 됐는지 확인할 것", field.Name, protocol_type.Name));
                        }
                        catch (PacketParserNotFound ex)
                        {
                            Debug.LogError(string.Format("type({0}) name({1}). {0} 타입에 대한 파서가 없습니다. 파서를 구현하세요.", ex.targetType, field.Name));
                        }
                    }
                    else
                    {
                        decodeField(Data, field, response);
                    }
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogWarning(string.Format("구조체에는 필드({0})가 있는데 네트워크 데이터에는 값이 없다. 구조체 이름 대소문자 확인하고 값이 할당 됐는지 확인할 것", field.Name));
                }
                catch (PacketParserNotFound ex)
                {
                    Debug.LogError(string.Format("type({0}) name({1}). {0} 타입에 대한 파서가 없습니다. 파서를 구현하세요.", ex.targetType, field.Name));
                }
            }
        }

        private void decodeField(JSONNode Data, System.Reflection.FieldInfo field, object response)
        {
            string typename = field.FieldType.Name;
            if (typename == "String")
            {
                string value = Data[field.Name];
                field.SetValue(response, value);
            }
            else if (typename == "Int32")
            {
                field.SetValue(response, Data[field.Name].AsInt);
            }
            else if (typename == "Boolean")
            {
                field.SetValue(response, Data[field.Name].AsBool);
            }
            else if (typename == "DateTime")
            {
                DateTime time = DateTime.Parse(Data[field.Name]);
                field.SetValue(response, time);
            }
            else
            {
                throw new PacketParserNotFound(typename);
            }
        }

    }
}
