using System;
using System.Collections.Concurrent;
using Sprache;
using System.Text;
using System.Linq;

namespace EasyNetQ
{
    public class SimpleTypeNameSerializer : ITypeNameSerializer
    {
        static readonly Parser<string> typeParser;

        static SimpleTypeNameSerializer()
        {
            var typeCharParser = Parse.LetterOrDigit.Or(Parse.Chars("."));

            var typeNameParser = typeCharParser.AtLeastOnce().Text();

            var nameDelimiterParser = Parse.Char(':');
            var genericDelimeterParser = Parse.Char(',');

            var simpleTypeParser =
                from typeName in typeNameParser
                from nameDelimeter in nameDelimiterParser
                from assemblyName in typeNameParser
                select typeName + "," + assemblyName;

            var emptyGenericTypeParser =
                from typeName in typeNameParser
                from genericMarker in Parse.Char('`')
                from numberOfGenerics in Parse.Number
                from nameDelimeter in nameDelimiterParser
                from assemblyName in typeNameParser
                select typeName + genericMarker + numberOfGenerics + "," + assemblyName;

            typeParser = null;

            var genericParameterParser =
                from genericBegin in Parse.Char('[')
                from nestedType in Parse.Ref(() => typeParser)
                from genericEnd in Parse.Char(']')
                from genericDelimeter in genericDelimeterParser.Optional()
                select nestedType;

            var genericParametersParser =
                from genericMarker in Parse.Char('`')
                from numberOfGenerics in Parse.Number
                from genericBegin in Parse.Char('[')
                from genericParameters in genericParameterParser.Repeat(int.Parse(numberOfGenerics))
                from genericEnd in Parse.Char(']')
                select Tuple.Create(numberOfGenerics, string.Join(",", genericParameters.Select(x => "[" + x + "]")));

            var genericTypeParser =
                from typeName in typeNameParser
                from genericParameters in genericParametersParser
                from nameDelimiter in nameDelimiterParser
                from assemblyName in typeNameParser
                select typeName + "`" + genericParameters.Item1 + "[" + genericParameters.Item2 + "]" + "," + assemblyName;

            typeParser = simpleTypeParser.Or(genericTypeParser).Or(emptyGenericTypeParser);
        }

        private readonly ConcurrentDictionary<string, Type> deserializedTypes = new ConcurrentDictionary<string, Type>();

        public Type DeSerialize(string typeName)
        {
            Preconditions.CheckNotBlank(typeName, "typeName");

            return deserializedTypes.GetOrAdd(typeName, t =>
            {
                var type = ParseTypeString(t);
                if (type == null)
                {
                    throw new EasyNetQException("Cannot find type {0}", t);
                }
                return type;
            });
        }

        private Type ParseTypeString(string typeString)
        {  
            return Type.GetType(typeParser.Parse(typeString));
        }

        private readonly ConcurrentDictionary<Type, string> serializedTypes = new ConcurrentDictionary<Type, string>();

        public string Serialize(Type type)
        {
            Preconditions.CheckNotNull(type, "type");

            return serializedTypes.GetOrAdd(type, t =>
            {
                var typeName = new StringBuilder(t.Namespace + "." + t.Name);

                if (t.IsGenericType && !t.IsGenericTypeDefinition)
                {
                    typeName.Append("[");
                    bool needSep = false;
                    foreach (Type argument in t.GetGenericArguments())
                    {
                        if (needSep)
                            typeName.Append(",");
                        typeName.Append("[");
                        typeName.Append(Serialize(argument));
                        typeName.Append("]");
                        needSep = true;
                    }
                    typeName.Append("]");
                }

                var result = typeName.ToString() + ":" + t.Assembly.GetName().Name;

                if (result.Length > 255)
                    throw new EasyNetQException("The serialized name of type '{0}' exceeds the AMQP " +
                                                    "maximum short string length of 255 characters.", t.Name);

                return result;
            });
        }
    }
}
