// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace System.Activities.Runtime.Collections
{
    // simple helper class to allow passing in a func that performs validations of
    // acceptable values
    public class ValidatingCollection<T> : Collection<T>
    {
        public ValidatingCollection()
        {
        }
        public static ValidatingCollection<T> NullCheck() => new()
        {
            // disallow null values
            OnAddValidationCallback = item =>
            {
                if (item == null)
                {
                    throw FxTrace.Exception.ArgumentNull(nameof(item));
                }
            }
        };
        public Action<T> OnAddValidationCallback { get; set; }
        public Action OnMutateValidationCallback { get; set; }

        private void OnAdd(T item)
        {
            if (OnAddValidationCallback != null)
            {
                OnAddValidationCallback(item);
            }
        }

        private void OnMutate()
        {
            if (OnMutateValidationCallback != null)
            {
                OnMutateValidationCallback();
            }
        }

        protected override void ClearItems()
        {
            OnMutate();
            base.ClearItems();
        }

        protected override void InsertItem(int index, T item)
        {
            OnAdd(item);
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            OnMutate();
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, T item)
        {
            OnAdd(item);
            OnMutate();
            base.SetItem(index, item);
        }
    }
}
