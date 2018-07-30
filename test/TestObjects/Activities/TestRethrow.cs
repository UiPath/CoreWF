// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CoreWf.Statements;

namespace Test.Common.TestObjects.Activities
{
    public class TestRethrow : TestActivity
    {
        public TestRethrow()
        {
            this.ProductActivity = new Rethrow();
        }
        private Rethrow ProductRethrow
        {
            get { return (Rethrow)this.ProductActivity; }
        }
    }
}
