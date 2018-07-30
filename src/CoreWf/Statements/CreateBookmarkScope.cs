// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Statements
{
    using CoreWf;

    public sealed class CreateBookmarkScope : NativeActivity<BookmarkScope>
    {
        public CreateBookmarkScope()
        {
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            // NoOp - we override this to suppress reflection. The base class
            // takes care of adding the Result argument.
        }

        protected override void Execute(NativeActivityContext context)
        {
            context.SetValue(this.Result, context.CreateBookmarkScope());
        }
    }
}
