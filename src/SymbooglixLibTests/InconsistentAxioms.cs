﻿using NUnit.Framework;
using System;
using Symbooglix;

namespace SymbooglixLibTests
{
    [TestFixture()]
    public class InconsistentAxioms : SymbooglixTest
    {
        private void Init()
        {
            p = LoadProgramFrom("programs/InconsistentAxioms.bpl");
            this.e = GetExecutor(p, new DFSStateScheduler(), GetSolver());
        }

        [Test(),ExpectedException(typeof(Symbooglix.ExecuteTerminatedStateException))]
        public void ExceptionThrown()
        {
            Init();
            e.Run(GetMain(p));

        }
    }


}

