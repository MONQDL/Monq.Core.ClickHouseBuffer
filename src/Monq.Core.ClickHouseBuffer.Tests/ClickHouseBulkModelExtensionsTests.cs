using Monq.Core.ClickHouseBuffer.Extensions;
using System;
using Xunit;

namespace Monq.Core.ClickHouseBuffer.Tests;

public class ClickHouseBulkModelExtensionsTests
{
    [Fact(DisplayName = "Check the correctness of field-value dictionary extraction from the object.")]
    public void ShouldProperlyCreateDbValuesDictionary()
    {
        var obj = new TestObject
        {
            Id = Guid.NewGuid(),
            PublicName = "Foo",
            Flag = true,
            PublicField = "Bar"
        };
        var result = obj.CreateDbValues(false);

        Assert.Collection(result,
            (x) => { Assert.Equal(nameof(TestObject.Id), x.Key); Assert.Equal(obj.Id, x.Value); },
            (x) => { Assert.Equal(nameof(TestObject.PublicName), x.Key); Assert.Equal(obj.PublicName, x.Value); },
            (x) => { Assert.Equal(nameof(TestObject.Flag), x.Key); Assert.Equal(obj.Flag, x.Value); }
        );
    }

    [Fact(DisplayName = "Check if the field-value dictionary is correctly extracted from the object. Names in camelCase")]
    public void ShouldProperlyCreateDbValuesDictionary_NamesInCamelCase()
    {
        var obj = new TestObject
        {
            Id = Guid.NewGuid(),
            PublicName = "Foo",
            Flag = true,
            PublicField = "Bar"
        };
        var result = obj.CreateDbValues(true);

        Assert.Collection(result,
            (x) => { Assert.Equal("id", x.Key); Assert.Equal(obj.Id, x.Value); },
            (x) => { Assert.Equal("publicName", x.Key); Assert.Equal(obj.PublicName, x.Value); },
            (x) => { Assert.Equal("flag", x.Key); Assert.Equal(obj.Flag, x.Value); }
        );
    }

    class TestObject
    {
        public Guid Id { get; set; }
        public string PublicName { get; set; }
        public bool Flag { get; set; }
        private string PrivateName { get; set; } = "notVisible";

        public string PublicField;
    }
}
