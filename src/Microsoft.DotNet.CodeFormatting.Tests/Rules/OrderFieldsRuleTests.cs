using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Rules = Microsoft.DotNet.CodeFormatting.Rules;
using Xunit;

namespace Microsoft.DotNet.CodeFormatting.Tests
{
    public class OrderFieldsRuleTests : SyntaxRuleTestBase
    {
        internal override ISyntaxFormattingRule Rule
        {
            get { return new Rules.OrderFieldsRule(); }
        }

        [Fact]
        public void SimpleClassWithoutWrongOrderPrivateFields()
        {
            var text = @"using System;
public class TestClass
{
    private bool _b;
    private bool _a;
}
";

            var expected = @"using System;
public class TestClass
{

    private bool _a;

    private bool _b;
}
";

            Verify(text, expected);
        }

        [Fact]
        public void ClassWithCtor()
        {
            var text = @"using System;
public class TestClass
{
    private bool _a;

    public TestClass()
    {
    }

    public void Beta()
    {
    }

    public void Alpha()
    {
    }
}
";

            var expected = @"using System;
public class TestClass
{

    private bool _a;

    public TestClass()
    {
    }

    public void Alpha()
    {
    }

    public void Beta()
    {
    }
}
";

            Verify(text, expected);
        }

        [Fact]
        public void SimpleClassWithComments()
        {
            var text = @"using System;
public class TestClass
{
    private bool _b;

    /// <summary>
    /// Test comment
    /// </summary>
    private bool _a;
}
";

            var expected = @"using System;
public class TestClass
{

    /// <summary>
    /// Test comment
    /// </summary>
    private bool _a;

    private bool _b;
}
";

            Verify(text, expected);
        }

    }
}
