// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Internals;
using Runtime;
using Validation;

[Fx.Tag.XamlVisible(false)]
internal sealed class ActivityLocationReferenceEnvironment : LocationReferenceEnvironment
{
    private Dictionary<string, LocationReference> _declarations;
    private string _singleDeclarationName;
    private LocationReference _singleDeclaration;
    private List<LocationReference> _unnamedDeclarations;

    public ActivityLocationReferenceEnvironment()
    {
        Extensions = new();
    }

    public ActivityLocationReferenceEnvironment(LocationReferenceEnvironment parent)
    {
        Parent = parent;
        if (Parent != null)
        {
            CompileExpressions = parent.CompileExpressions;
            IsValidating = parent.IsValidating;
            InternalRoot = parent.Root;
            Extensions = parent.Extensions;
        }
    }

    public override Activity Root => InternalRoot;

    public Activity InternalRoot { get; set; }

    private Dictionary<string, LocationReference> Declarations
    {
        get
        {
            if (_declarations == null)
            {
                _declarations = new Dictionary<string, LocationReference>();
                if (_singleDeclaration != null)
                {
                    _declarations.Add(_singleDeclarationName, _singleDeclaration);
                    _singleDeclarationName = null;
                    _singleDeclaration = null;
                }
            }

            return _declarations;
        }
    }

    public override bool IsVisible(LocationReference locationReference)
    {
        if (locationReference == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(locationReference));
        }

        LocationReferenceEnvironment currentScope = this;

        while (currentScope != null)
        {

            if (currentScope is ActivityLocationReferenceEnvironment activityEnvironment)
            {
                if (locationReference == activityEnvironment._singleDeclaration)
                {
                    return true;
                }
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
            if (TryGetLocalDeclaration(locationReference.Name, out _))
            {
                string id = null;

                if (owner != null)
                {
                    id = owner.Id;
                }

                ValidationError validationError = new(SR.SymbolNamesMustBeUnique(locationReference.Name))
                {
                    Source = owner,
                    Id = id
                };

                ActivityUtilities.Add(ref validationErrors, validationError);
            }
            else
            {
                string name = locationReference.Name;
                if (_declarations == null && _singleDeclaration == null)
                {
                    ArgumentNullException.ThrowIfNull(name, "key");
                    _singleDeclarationName = name;
                    _singleDeclaration = locationReference;
                }
                else
                {
                    Declarations.Add(name, locationReference);
                }
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
            LocationReferenceEnvironment currentEnvironment = Parent;

            while (currentEnvironment is ActivityLocationReferenceEnvironment)
            {
                currentEnvironment = currentEnvironment.Parent;
            }

            if (currentEnvironment != null)
            {
                Fx.Assert(currentEnvironment is not ActivityLocationReferenceEnvironment, "We must be at a non-ActivityLocationReferenceEnvironment implementation.");

                return currentEnvironment.TryGetLocationReference(name, out result);
            }
        }
        else
        {
            if (TryGetLocalDeclaration(name, out result))
            {
                return true;
            }

            bool found = false;
            LocationReferenceEnvironment currentEnvironment = Parent;

            // Loop through all of the ActivityLocationReferenceEnvironments we have chained together
            while (currentEnvironment != null && currentEnvironment is ActivityLocationReferenceEnvironment environment)
            {
                ActivityLocationReferenceEnvironment activityEnvironment = environment;
                if (activityEnvironment.TryGetLocalDeclaration(name, out result))
                {
                    return true;
                }

                currentEnvironment = currentEnvironment.Parent;
            }

            if (!found)
            {
                if (currentEnvironment != null)
                {
                    // Looks like we have a non-ActivityLocationReferenceEnvironment at the root
                    Fx.Assert(currentEnvironment is not ActivityLocationReferenceEnvironment, "We should have some other host environment at this point.");
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

    public override IEnumerable<LocationReference> GetLocationReferences() => Declarations.Values;

    private bool TryGetLocalDeclaration(string name, out LocationReference result)
    {
        ArgumentNullException.ThrowIfNull(name, "key");
        if (_declarations != null)
        {
            return _declarations.TryGetValue(name, out result);
        }
        if (_singleDeclaration != null && string.Equals(_singleDeclarationName, name, StringComparison.Ordinal))
        {
            result = _singleDeclaration;
            return true;
        }
        result = null;
        return false;
    }
}
