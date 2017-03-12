using System;
using EasyNetQ.Tests.ProducerTests.Very.Long.Namespace.Certainly.Longer.Than.The255.Char.Length.That.RabbitMQ.Likes.That.Will.Certainly.Cause.An.AMQP.Exception.If.We.Dont.Do.Something.About.It.And.Stop.It.From.Happening;
using NUnit.Framework;
using System.Collections.Generic;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class SimpleTypeNameSerializerTests
    {
        const string expectedTypeName = "System.String:mscorlib";
        private const string expectedCustomTypeName = "EasyNetQ.Tests.SomeRandomClass:EasyNetQ.Tests";
        private const string emptyGeneric = "System.Collections.Generic.List`1:mscorlib";
        private const string simpleGeneric =
            "System.Collections.Generic.Dictionary`2[[System.Int32:mscorlib],[System.String:mscorlib]]:mscorlib";
        private const string simpleGenericWithCustomType =
            "System.Collections.Generic.List`1[[" + expectedCustomTypeName + "]]:mscorlib";
        private const string nestedGeneric =
            "System.Collections.Generic.List`1[[System.Collections.Generic.Dictionary`2[[System.Int32:mscorlib],[System.String:mscorlib]]:mscorlib]]:mscorlib";

        private ITypeNameSerializer simpleTypeNameSerializer;

        [SetUp]
        public void SetUp()
        {
            simpleTypeNameSerializer = new SimpleTypeNameSerializer();
        }

        [Test]
        public void Should_serialize_a_type_name()
        {
            var typeName = simpleTypeNameSerializer.Serialize(typeof(string));
            typeName.ShouldEqual(expectedTypeName);
        }

        [Test]
        public void Should_serialize_a_custom_type()
        {
            var typeName = simpleTypeNameSerializer.Serialize(typeof(SomeRandomClass));
            typeName.ShouldEqual(expectedCustomTypeName);
        }

        [Test]
        public void Should_deserialize_a_type_name()
        {
            var type = simpleTypeNameSerializer.DeSerialize(expectedTypeName);
            type.ShouldEqual(typeof(string));
        }

        [Test]
        public void Should_deserialize_a_custom_type()
        {
            var type = simpleTypeNameSerializer.DeSerialize(expectedCustomTypeName);
            type.ShouldEqual(typeof(SomeRandomClass));
        }

        [Test]
        public void Should_serialize_an_empty_generic()
        {
            var typeName = simpleTypeNameSerializer.Serialize(typeof(List<>));
            typeName.ShouldEqual(emptyGeneric);
        }

        [Test]
        public void Should_deserialize_an_empty_generic()
        {
            var type = simpleTypeNameSerializer.DeSerialize(emptyGeneric);
            type.ShouldEqual(typeof(List<>));
        }

        [Test]
        public void Should_serialize_a_simple_generic()
        {
            var typeName = simpleTypeNameSerializer.Serialize(typeof(Dictionary<int, string>));
            typeName.ShouldEqual(simpleGeneric);
        }

        [Test]
        public void Should_deserialize_a_simple_generic()
        {
            var type = simpleTypeNameSerializer.DeSerialize(simpleGeneric);
            type.ShouldEqual(typeof(Dictionary<int, string>));
        }

        [Test]
        public void Should_serialize_a_simple_generic_with_custom_types()
        {
            var typeName = simpleTypeNameSerializer.Serialize(typeof(List<SomeRandomClass>));
            typeName.ShouldEqual(simpleGenericWithCustomType);
        }

        [Test]
        public void Should_deserialize_a_simple_generic_with_custom_types()
        {
            var type = simpleTypeNameSerializer.DeSerialize(simpleGenericWithCustomType);
            type.ShouldEqual(typeof(List<SomeRandomClass>));
        }

        [Test]
        public void Should_serialize_nested_generic()
        {
            var typeName = simpleTypeNameSerializer.Serialize(typeof(List<Dictionary<int, string>>));
            typeName.ShouldEqual(nestedGeneric);
        }

        [Test]
        public void Should_deserialize_nested_generic()
        {
            var type = simpleTypeNameSerializer.DeSerialize(nestedGeneric);
            type.ShouldEqual(typeof(List<Dictionary<int, string>>));
        }

        [Test]
        [ExpectedException(typeof(EasyNetQException))]
        public void Should_throw_exception_when_type_name_is_not_recognised()
        {
            simpleTypeNameSerializer.DeSerialize("EasyNetQ.TypeNameSerializer.None:EasyNetQ");
        }

        [Test]
        [ExpectedException(typeof(EasyNetQException))]
        public void Should_throw_if_type_name_is_too_long()
        {
            simpleTypeNameSerializer.Serialize(
                typeof(
                    MessageWithVeryVEryVEryLongNameThatWillMostCertainlyBreakAmqpsSilly255CharacterNameLimitThatIsAlmostCertainToBeReachedWithGenericTypes
                    ));
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Should_throw_exception_if_type_name_is_null()
        {
            simpleTypeNameSerializer.DeSerialize(null);
        }
    }
}
