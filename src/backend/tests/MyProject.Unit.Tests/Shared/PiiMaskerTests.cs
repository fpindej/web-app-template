using MyProject.Shared;

namespace MyProject.Unit.Tests.Shared;

public class PiiMaskerTests
{
    [Fact]
    public void MaskEmail_StandardEmail_ShouldMaskCorrectly()
    {
        Assert.Equal("j***@g***.com", PiiMasker.MaskEmail("john@gmail.com"));
    }

    [Fact]
    public void MaskEmail_ShortLocalPart_ShouldMaskCorrectly()
    {
        Assert.Equal("a***@e***.com", PiiMasker.MaskEmail("a@example.com"));
    }

    [Fact]
    public void MaskEmail_MultipleDomainDots_ShouldPreserveTldAfterLastDot()
    {
        Assert.Equal("u***@m***.uk", PiiMasker.MaskEmail("user@mail.co.uk"));
    }

    [Fact]
    public void MaskEmail_NoDotInDomain_ShouldMaskEntireDomain()
    {
        Assert.Equal("u***@l***", PiiMasker.MaskEmail("user@localhost"));
    }

    [Fact]
    public void MaskEmail_Empty_ShouldReturnPlaceholder()
    {
        Assert.Equal("***", PiiMasker.MaskEmail(""));
    }

    [Fact]
    public void MaskEmail_Whitespace_ShouldReturnPlaceholder()
    {
        Assert.Equal("***", PiiMasker.MaskEmail("   "));
    }

    [Fact]
    public void MaskEmail_NoAtSign_ShouldReturnPlaceholder()
    {
        Assert.Equal("***", PiiMasker.MaskEmail("invalidemail"));
    }

    [Fact]
    public void MaskEmail_AtSignAtStart_ShouldReturnPlaceholder()
    {
        Assert.Equal("***", PiiMasker.MaskEmail("@domain.com"));
    }

    [Fact]
    public void MaskPhone_WithValue_ShouldReturnPlaceholder()
    {
        Assert.Equal("***", PiiMasker.MaskPhone("+420123456789"));
    }

    [Fact]
    public void MaskPhone_Null_ShouldReturnNull()
    {
        Assert.Null(PiiMasker.MaskPhone(null));
    }

    [Fact]
    public void MaskPhone_EmptyString_ShouldReturnPlaceholder()
    {
        Assert.Equal("***", PiiMasker.MaskPhone(""));
    }
}
