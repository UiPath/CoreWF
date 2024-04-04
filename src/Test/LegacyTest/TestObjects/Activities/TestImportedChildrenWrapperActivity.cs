// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using LegacyTest.Test.Common.TestObjects.CustomActivities;

namespace LegacyTest.Test.Common.TestObjects.Activities
{
    public class TestImportedChildrenWrapperActivity : TestActivity
    {
        private TestActivity _body;

        public TestImportedChildrenWrapperActivity()
        {
            this.ProductActivity = new ImportedChildrenWrapperActivity();
        }

        internal override IEnumerable<TestActivity> GetChildren()
        {
            if (this.Body != null)
            {
                yield return this.Body;
            }
        }

        public TestActivity Body
        {
            get
            {
                return _body;
            }
            set
            {
                if (value != null)
                {
                    this.ProductImportedChildrenWrapper.Body = value.ProductActivity;
                    _body = value;
                }
                else
                {
                    this.ProductImportedChildrenWrapper.Body = null;
                    _body = value;
                }
            }
        }

        public ImportedChildrenWrapperActivity ProductImportedChildrenWrapper
        {
            get
            {
                return (ImportedChildrenWrapperActivity)this.ProductActivity;
            }
        }

        public void AddImportedChild(params TestActivity[] importedChildren)
        {
            foreach (TestActivity act in importedChildren)
            {
                this.ProductImportedChildrenWrapper.ImportedChildren.Add(act.ProductActivity);
            }
        }
    }
}
