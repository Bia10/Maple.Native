namespace Maple.Native.Test;

public class TypeSizesTests
{
    [Test]
    public async Task Int32_Is4Bytes()
    {
        int actual = TypeSizes.Int32;
        await Assert.That(actual).IsEqualTo(4);
    }

    [Test]
    public async Task UInt32_Is4Bytes()
    {
        int actual = TypeSizes.UInt32;
        await Assert.That(actual).IsEqualTo(4);
    }

    [Test]
    public async Task Pointer_Is4Bytes()
    {
        int actual = TypeSizes.Pointer;
        await Assert.That(actual).IsEqualTo(4);
    }
}
