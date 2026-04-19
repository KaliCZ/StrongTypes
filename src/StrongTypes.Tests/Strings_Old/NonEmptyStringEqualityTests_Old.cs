using Xunit;

namespace StrongTypes.Tests.Strings;

public class NonEmptyStringTests
{
    [Fact]
    public void EqualityTest()
    {
#pragma warning disable xUnit2010

        string text = "ASDF123";
        NonEmptyString nonEmptyString = NonEmptyString.Create("ASDF123");
        NonEmptyString nonEmptyStringWithSameValue = NonEmptyString.Create("ASDF123");
        Assert.True(text == nonEmptyString);
        Assert.False(text != nonEmptyString);
        Assert.True(nonEmptyString == text);
        Assert.False(nonEmptyString != text);
        Assert.True(nonEmptyString == nonEmptyStringWithSameValue);
        Assert.False(nonEmptyString != nonEmptyStringWithSameValue);

        Assert.True(text.Equals(nonEmptyString));
        Assert.True(nonEmptyString.Equals(text));
        Assert.True(nonEmptyString.Equals(nonEmptyStringWithSameValue));

        Assert.False(object.Equals(text, nonEmptyString)); // Unfortunately string doesn't override the default Equals method to compare with IEquatable<string> therefore this is false.
        Assert.True(object.Equals(nonEmptyString, text));
        Assert.True(object.Equals(nonEmptyString, nonEmptyStringWithSameValue));

        string differentString = "Text14";
        NonEmptyString differentNonEmptyString = NonEmptyString.Create("Totally different text here.");
        NonEmptyString differentNonEmptyString2 = NonEmptyString.Create("And completely different again.");

        Assert.False(differentString == differentNonEmptyString);
        Assert.True(differentString != differentNonEmptyString);
        Assert.False(differentNonEmptyString == differentString);
        Assert.True(differentNonEmptyString != differentString);
        Assert.False(differentNonEmptyString == differentNonEmptyString2);
        Assert.True(differentNonEmptyString != differentNonEmptyString2);

        Assert.False(differentString.Equals(differentNonEmptyString));
        Assert.False(differentNonEmptyString.Equals(differentString));
        Assert.False(differentNonEmptyString.Equals(differentNonEmptyString2));

        Assert.False(object.Equals(differentString, differentNonEmptyString));
        Assert.False(object.Equals(differentNonEmptyString, differentString));
        Assert.False(object.Equals(differentNonEmptyString, differentNonEmptyString2));

        NonEmptyString null1 = null;
        string null2 = null;
        Assert.True(object.Equals(null1, null2));
        Assert.True(object.Equals(null2, null1));

#pragma warning restore xUnit2010
    }
}