// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.XamlIntegration
{
    using System;
    using System.Activities;
    using System.Activities.Expressions;
    using System.Runtime.Serialization;
    using System.Collections.Generic;
    using System.Activities.Internals;

    [DataContract(Name = XD.CompiledLocation.Name, Namespace = XD.Runtime.Namespace)]
    internal class CompiledLocation<T> : Location<T>
    {
        private Func<T> getMethod;
        private Action<T> setMethod;
        private int expressionId;
        private IList<LocationReference> locationReferences;
        private IList<Location> locations;
        private ActivityInstance rootInstance;
        private readonly Activity compiledRootActivity;
        private byte[] compiledRootActivityQualifiedId;
        private readonly Activity expressionActivity;
        private byte[] expressionActivityQualifiedId;
        private string expressionText;
        private bool forImplementation;

        public CompiledLocation(Func<T> getMethod, Action<T> setMethod, IList<LocationReference> locationReferences, IList<Location> locations, int expressionId, Activity compiledRootActivity, ActivityContext currentActivityContext)
        {
            this.getMethod = getMethod;
            this.setMethod = setMethod;

            this.forImplementation = currentActivityContext.Activity.MemberOf != currentActivityContext.Activity.RootActivity.MemberOf;
            this.locationReferences = locationReferences;
            this.locations = locations;
            this.expressionId = expressionId;

            this.compiledRootActivity = compiledRootActivity;
            this.expressionActivity = currentActivityContext.Activity;
            //
            // Save the root activity instance to get the root activity post persistence
            // The root will always be alive as long as the location is valid, which is not
            // true for the activity instance of the expression that is executing
            this.rootInstance = currentActivityContext.CurrentInstance;
            while (this.rootInstance.Parent != null)
            {
                this.rootInstance = this.rootInstance.Parent;
            }
            //
            // Save the text of the expression for exception message
            if (currentActivityContext.Activity is ITextExpression textExpression)
            {
                this.expressionText = textExpression.ExpressionText;
            }
        }

        public CompiledLocation(Func<T> getMethod, Action<T> setMethod)
        {
            //
            // This is the constructor that is used to refresh the get/set methods during rehydration
            // An instance of this class created with the constructor cannot be invoked.
            this.getMethod = getMethod;
            this.setMethod = setMethod;
        }

        public override T Value
        {
            get
            {
                if (this.getMethod == null)
                {
                    RefreshAccessors();
                }
                return getMethod();
            }
            set
            {
                if (this.setMethod == null)
                {
                    RefreshAccessors();
                }
                setMethod(value);
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public byte[] CompiledRootActivityQualifiedId
        {
            get
            {
                if (this.compiledRootActivityQualifiedId == null)
                {
                    return this.compiledRootActivity.QualifiedId.AsByteArray();
                }

                return this.compiledRootActivityQualifiedId;
            }
            set
            {
                this.compiledRootActivityQualifiedId = value;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public byte[] ExpressionActivityQualifiedId
        {
            get
            {
                if (this.expressionActivityQualifiedId == null)
                {
                    return this.expressionActivity.QualifiedId.AsByteArray();
                }

                return this.expressionActivityQualifiedId;
            }
            set
            {
                this.expressionActivityQualifiedId = value;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public List<(string Name, string TypeName)> LocationReferenceCache
        {
            get
            {
                if (locationReferences == null || locationReferences.Count == 0)
                {
                    return null;
                }
                var durableCache = new List<(string, string)>(locationReferences.Count);
                foreach (LocationReference reference in locationReferences)
                {
                    durableCache.Add((reference.Name, reference.Type.AssemblyQualifiedName));
                }
                return durableCache;
            }
            set
            {
                if (value == null || value.Count == 0)
                {
                    locationReferences = new List<LocationReference>();
                    return;
                }
                locationReferences = new List<LocationReference>(value.Count);
                foreach (var (Name, TypeName) in value)
                {
                    locationReferences.Add(new CompiledLocationReference(Name, Type.GetType(TypeName, throwOnError: true)));
                }
            }
        }

        [DataMember(EmitDefaultValue = false, Name = "expressionId")]
        internal int SerializedExpressionId
        {
            get { return this.expressionId; }
            set { this.expressionId = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "locations")]
        internal IList<Location> SerializedLocations
        {
            get { return this.locations; }
            set { this.locations = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "rootInstance")]
        internal ActivityInstance SerializedRootInstance
        {
            get { return this.rootInstance; }
            set { this.rootInstance = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "expressionText")]
        internal string SerializedExpressionText
        {
            get { return this.expressionText; }
            set { this.expressionText = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "forImplementation")]
        internal bool SerializedForImplementation
        {
            get { return this.forImplementation; }
            set { this.forImplementation = value; }
        }

        private void RefreshAccessors()
        {
            //
            // If we've gotten here is means that we have a location that has roundtripped through persistence
            // CompiledDataContext & ICER don't round trip so we need to get them back from the current tree 
            // and get new pointers to the get/set methods for this expression
            ICompiledExpressionRoot compiledRoot = GetCompiledExpressionRoot();
            CompiledLocation<T> tempLocation = (CompiledLocation<T>)compiledRoot.InvokeExpression(this.expressionId, this.locations);
            this.getMethod = tempLocation.getMethod;
            this.setMethod = tempLocation.setMethod;
        }

        private ICompiledExpressionRoot GetCompiledExpressionRoot()
        {
            if (this.rootInstance != null && this.rootInstance.Activity != null)
            {
                ICompiledExpressionRoot compiledExpressionRoot;
                Activity rootActivity = this.rootInstance.Activity;

                if (QualifiedId.TryGetElementFromRoot(rootActivity, this.compiledRootActivityQualifiedId, out Activity compiledRootActivity) &&
                    QualifiedId.TryGetElementFromRoot(rootActivity, this.expressionActivityQualifiedId, out Activity expressionActivity))
                {
                    if (CompiledExpressionInvoker.TryGetCompiledExpressionRoot(expressionActivity, compiledRootActivity, out compiledExpressionRoot))
                    {
                        //
                        // Revalidate to make sure we didn't hit an ID shift
                        if (compiledExpressionRoot.CanExecuteExpression(this.expressionText, true /* this is always a reference */, this.locationReferences, out this.expressionId))
                        {
                            return compiledExpressionRoot;
                        }
                    }
                }
                //
                // We were valid when this location was generated so an ID shift occurred (likely due to a dynamic update)
                // Need to search all of the ICERs for one that can execute this expression.
                if (FindCompiledExpressionRoot(rootActivity, out compiledExpressionRoot))
                {
                    return compiledExpressionRoot;
                }
            }
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.UnableToLocateCompiledLocationContext(this.expressionText)));
        }

        private bool FindCompiledExpressionRoot(Activity activity, out ICompiledExpressionRoot compiledExpressionRoot)
        {
            if (CompiledExpressionInvoker.TryGetCompiledExpressionRoot(activity, this.forImplementation, out compiledExpressionRoot))
            {
                if (compiledExpressionRoot.CanExecuteExpression(this.expressionText, true /* this is always a reference */, this.locationReferences, out this.expressionId))
                {
                    return true;
                }
            }

            foreach (Activity containedActivity in WorkflowInspectionServices.GetActivities(activity))
            {
                if (FindCompiledExpressionRoot(containedActivity, out compiledExpressionRoot))
                {
                    return true;
                }
            }

            compiledExpressionRoot = null;
            return false;
        }

        private class CompiledLocationReference : LocationReference
        {
            private readonly string name;
            private readonly Type type;

            public CompiledLocationReference(string name, Type type)
            {
                this.name = name;
                this.type = type;
            }

            protected override string NameCore
            {
                get
                {
                    return name;
                }
            }

            protected override Type TypeCore
            {
                get
                {
                    return type;
                }
            }

            public override Location GetLocation(ActivityContext context)
            {
                //
                // We should never hit this, these references are strictly for preserving location names/types
                // through persistence to allow for revalidation on the other side
                // Actual execution occurs through the locations that were stored separately
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CompiledLocationReferenceGetLocation));
            }
        }
    }
}
