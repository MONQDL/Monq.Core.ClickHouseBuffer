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
        var resultValues = obj.ExtractDbColumnValues();
        var resultColumns = obj.ExtractDbColumnNames(false);

        Assert.Collection(resultColumns,
            (x) => Assert.Equal(nameof(TestObject.Id), x),
            (x) => Assert.Equal(nameof(TestObject.PublicName), x),
            (x) => Assert.Equal(nameof(TestObject.Flag), x)
        );

        Assert.Collection(resultValues,
            (x) => { Assert.Equal(obj.Id, x); },
            (x) => { Assert.Equal(obj.PublicName, x); },
            (x) => { Assert.Equal(obj.Flag, x); }
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
        var resultValues = obj.ExtractDbColumnValues();
        var resultColumns = obj.ExtractDbColumnNames(true);

        Assert.Collection(resultColumns,
            (x) => Assert.Equal("id", x),
            (x) => Assert.Equal("publicName", x),
            (x) => Assert.Equal("flag", x)
        );

        Assert.Collection(resultValues,
            (x) => Assert.Equal(obj.Id, x),
            (x) => Assert.Equal(obj.PublicName, x),
            (x) => Assert.Equal(obj.Flag, x)
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
