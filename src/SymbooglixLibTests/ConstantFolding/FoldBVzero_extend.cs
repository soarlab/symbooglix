using NUnit.Framework;
using System;
using Symbooglix;
using Microsoft.Boogie;
using System.Collections.Generic;
using Microsoft.Basetypes;


namespace ConstantFoldingTests
{
    [TestFixture()]
    public class FoldBVzero_extend : TestBase
    {
        public FoldBVzero_extend()
        {
            // Boogie hits NullPtrException if the cmdline parser
            // isn't setup. This is sooo annoying!
            SymbooglixLibTests.SymbooglixTest.SetupCmdLineParser();
        }

        [Test()]
        public void Simple()
        {
            var inputBV = new LiteralExpr(Token.NoToken, BigNum.FromInt(7), 4); // 7bv4
            Microsoft.Boogie.Type[] argTypes = {BvType.GetBvType(4) };
            var zeroExtend = CreateBVBuiltIn("BV4_ZEXT8", "zero_extend 8", BvType.GetBvType(8), argTypes);

            var args = new List<Expr>();
            args.Add(inputBV);
            var nary = new NAryExpr(Token.NoToken, zeroExtend, args);
            var TC = new TypecheckingContext(this);
            nary.Typecheck(TC);

            var CFT = new ConstantFoldingTraverser();
            var result = CFT.Traverse(nary);
            Assert.IsTrue(result is LiteralExpr);

            var literal = result as LiteralExpr;
            Assert.IsTrue(literal.isBvConst);
            Assert.IsTrue(literal.asBvConst.Bits == 8);
            Assert.IsTrue(literal.asBvConst.Value == BigNum.FromInt(7));
        }
    }
}

