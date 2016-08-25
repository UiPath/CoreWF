// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using Microsoft.CoreWf.Validation;
using System.Collections.Generic;

namespace Microsoft.CoreWf
{
    [Fx.Tag.XamlVisible(false)]
    internal sealed class ActivityLocationReferenceEnvironment : LocationReferenceEnvironment
    {
        private Dictionary<string, LocationReference> _declarations;
        private List<LocationReference> _unnamedDeclarations;

        public ActivityLocationReferenceEnvironment()
        {
        }

        public ActivityLocationReferenceEnvironment(LocationReferenceEnvironment parent)
        {
            this.Parent = parent;

            if (this.Parent != null)
            {
                this.InternalRoot = parent.Root;
            }
        }

        public override Activity Root
        {
            get
            {
                return this.InternalRoot;
            }
        }

        public Activity InternalRoot
        {
            get;
            set;
        }

        private Dictionary<string, LocationReference> Declarations
        {
            get
            {
                if (_declarations == null)
                {
                    _declarations = new Dictionary<string, LocationReference>();
                }

                return _declarations;
            }
        }

        public override bool IsVisible(LocationReference locationReference)
        {
            if (locationReference == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("locationReference");
            }

            LocationReferenceEnvironment currentScope = this;

            while (currentScope != null)
            {
                ActivityLocationReferenceEnvironment activityEnvironment = currentScope as ActivityLocationReferenceEnvironment;

                if (activityEnvironment != null)
                {
                    if (activityEnvironment._declarations != null)
                    {
                        foreach (LocationReference declaration in activityEnvironment._declarations.Values)
                        {
                            if (locationReference == declaration)
                            {
                                return true;
                            }
                        }
                    }

                    if (activityEnvironment._unnamedDeclarations != null)
                    {
                        for (int i = 0; i < activityEnvironment._unnamedDeclarations.Count; i++)
                        {
                            if (locationReference == activityEnvironment._unnamedDeclarations[i])
                            {
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    return currentScope.IsVisible(locationReference);
                }

                currentScope = currentScope.Parent;
            }

            return false;
        }

        public void Declare(LocationReference locationReference, Activity owner, ref IList<ValidationError> validationErrors)
        {
            Fx.Assert(locationReference != null, "Must not be null");

            if (locationReference.Name == null)
            {
                if (_unnamedDeclarations == null)
                {
                    _unnamedDeclarations = new List<LocationReference>();
                }

                _unnamedDeclarations.Add(locationReference);
            }
            else
            {
                if (this.Declarations.ContainsKey(locationReference.Name))
                {
                    string id = null;

                    if (owner != null)
                    {
                        id = owner.Id;
                    }

                    ValidationError validationError = new ValidationError(SR.SymbolNamesMustBeUnique(locationReference.Name))
                    {
                        Source = owner,
                        Id = id
                    };

                    ActivityUtilities.Add(ref validationErrors, validationError);
                }
                else
                {
                    this.Declarations.Add(locationReference.Name, locationReference);
                }
            }
        }

        public override bool TryGetLocationReference(string name, out LocationReference result)
        {
            if (name == null)
            {
                // We don't allow null names in our LocationReferenceEnvironment but
                // a custom declared environment might.  We need to walk up
                // to the root and see if it chains to a
                // non-ActivityLocationReferenceEnvironment implementation
                LocationReferenceEnvironment currentEnvironment = this.Parent;

                while (currentEnvironment is ActivityLocationReferenceEnvironment)
                {
                    currentEnvironment = currentEnvironment.Parent;
                }

                if (currentEnvironment != null)
                {
                    Fx.Assert(!(currentEnvironment is ActivityLocationReferenceEnvironment), "We must be at a non-ActivityLocationReferenceEnvironment implementation.");

                    return currentEnvironment.TryGetLocationReference(name, out result);
                }
            }
            else
            {
                if (_declarations != null && _declarations.TryGetValue(name, out result))
                {
                    return true;
                }

                bool found = false;
                LocationReferenceEnvironment currentEnvironment = this.Parent;
                LocationReferenceEnvironment rootEnvironment = this;

                // Loop through all of the ActivityLocationReferenceEnvironments we have chained together
                while (currentEnvironment != null && currentEnvironment is ActivityLocationReferenceEnvironment)
                {
                    ActivityLocationReferenceEnvironment activityEnvironment = (ActivityLocationReferenceEnvironment)currentEnvironment;
                    if (activityEnvironment._declarations != null && activityEnvironment._declarations.TryGetValue(name, out result))
                    {
                        return true;
                    }

                    rootEnvironment = currentEnvironment;
                    currentEnvironment = currentEnvironment.Parent;
                }

                if (!found)
                {
                    if (currentEnvironment != null)
                    {
                        // Looks like we have a non-ActivityLocationReferenceEnvironment at the root
                        Fx.Assert(!(currentEnvironment is ActivityLocationReferenceEnvironment), "We should have some other host environment at this point.");
                        if (currentEnvironment.TryGetLocationReference(name, out result))
                        {
                            return true;
                        }
                    }
                }
            }

            result = null;
            return false;
        }

        public override IEnumerable<LocationReference> GetLocationReferences()
        {
            return this.Declarations.Values;
        }
    }
}
