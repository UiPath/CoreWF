// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.XamlIntegration
{
    using System;
    using CoreWf;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using CoreWf.Internals;

    public abstract class CompiledDataContext
    {
        private readonly IList<Location> locations;
        private readonly IList<LocationReference> locationReferences;
        private readonly ExpressionTreeRewriter visitor;        

        //[SuppressMessage(FxCop.Category.Usage, FxCop.Rule.DoNotCallOverridableMethodsInConstructors, Justification = "Derived classes are always generated code")]
        protected CompiledDataContext(IList<LocationReference> locationReferences, ActivityContext activityContext)
        {
            this.locationReferences = locationReferences;

            if (this.locationReferences == null)
            {
                this.locationReferences = new List<LocationReference>();
            }

            this.locations = ConvertReferences(this.locationReferences, activityContext);
        }

        protected CompiledDataContext(IList<Location> locations)
        {
            this.locations = locations;
        }

        protected CompiledDataContext(IList<LocationReference> locationReferences)
        {
            this.visitor = new ExpressionTreeRewriter(locationReferences);
        }

        protected object GetVariableValue(int index)
        {
            return this.locations[index].Value;
        }

        protected void SetVariableValue(int index, object value)
        {
            this.locations[index].Value = value;
        }

        protected virtual void GetValueTypeValues()
        {
        }
        
        protected virtual void SetValueTypeValues()
        {
        }

        protected Expression RewriteExpressionTree(Expression originalExpression)
        {

            if (!(originalExpression is LambdaExpression lambdaExpression))
            {
                throw FxTrace.Exception.Argument(nameof(originalExpression), SR.LambdaExpressionTypeRequired);
            }

            if (lambdaExpression.ReturnType == null || lambdaExpression.ReturnType == typeof(void))
            {
                throw FxTrace.Exception.Argument(nameof(originalExpression), SR.LambdaExpressionReturnTypeInvalid);
            }
            
            return this.visitor.Visit(Expression.Lambda(
                typeof(Func<,>).MakeGenericType(typeof(ActivityContext), lambdaExpression.ReturnType),
                lambdaExpression.Body, 
                new ParameterExpression[] { ExpressionUtilities.RuntimeContextParameter }));
        }
                
        public Location<T> GetLocation<T>(Func<T> getMethod, Action<T> setMethod, int expressionId, Activity compiledRootActivity, ActivityContext activityContext)
        {
            return new CompiledLocation<T>(getMethod, setMethod, this.locationReferences, this.locations, expressionId, compiledRootActivity, activityContext);
        }

        public Location<T> GetLocation<T>(Func<T> getMethod, Action<T> setMethod)
        {
            return new CompiledLocation<T>(getMethod, setMethod);
        }

        protected static object GetDataContextActivities(Activity compiledRoot, bool forImplementation)
        {
            CompiledDataContextActivityVistor vistor = new CompiledDataContextActivityVistor();
            vistor.Visit(compiledRoot, forImplementation);
            CompiledDataContextActivitiesCache cache = new CompiledDataContextActivitiesCache(vistor.DataContextActivities);
            return cache;
        }

        protected static CompiledDataContext[] GetCompiledDataContextCache(object dataContextActivities, ActivityContext activityContext, Activity compiledRoot, bool forImplementation, int compiledDataContextCount)
        {
            ActivityInstance cacheInstance = GetDataContextInstance((CompiledDataContextActivitiesCache)dataContextActivities, activityContext, compiledRoot);

            HybridDictionary<Activity, CompiledDataContext[]> cache = null;
            if (forImplementation)
            {
                cache = (HybridDictionary<Activity, CompiledDataContext[]>)cacheInstance.CompiledDataContextsForImplementation;
            }
            else
            {
                cache = (HybridDictionary<Activity, CompiledDataContext[]>)cacheInstance.CompiledDataContexts;
            }

            if (cache == null)
            {
                cache = new HybridDictionary<Activity, CompiledDataContext[]>();

                if (forImplementation)
                {
                    cacheInstance.CompiledDataContextsForImplementation = cache;
                }
                else
                {
                    cacheInstance.CompiledDataContexts = cache;
                }
            }

            if (!cache.TryGetValue(compiledRoot, out CompiledDataContext[] result))
            {
                result = new CompiledDataContext[compiledDataContextCount];
                cache.Add(compiledRoot, result);
            }

            return result;
        }

        private static ActivityInstance GetDataContextInstance(CompiledDataContextActivitiesCache dataContextActivities, ActivityContext activityContext, Activity compiledRoot)
        {
            ActivityInstance dataContextInstance = null;

            ActivityInstance currentInstance = activityContext.CurrentInstance;

            while (currentInstance != null)
            {
                if (dataContextActivities.Contains(currentInstance.Activity))
                {
                    dataContextInstance = currentInstance;
                    break;
                }
                //
                // Make sure we don't walk out of our IdSpace
                if (currentInstance.Activity == compiledRoot)
                {
                    break;
                }
                //
                // For SecondaryRoot scenarios the ActivityInstance tree may not
                // contain any of the data context activity instances because
                // the instance tree does not have to match the activity definition tree.
                // In this case just use the root instance.
                if (currentInstance.Parent == null)
                {
                    dataContextInstance = currentInstance;
                }

                currentInstance = currentInstance.Parent;
            }
            
            if (dataContextInstance == null)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CompiledExpressionsNoCompiledRoot(activityContext.Activity.Id)));
            }

            return dataContextInstance;
        }

        private IList<Location> ConvertReferences(IList<LocationReference> locationReferences, ActivityContext activityContext)
        {
            IList<Location> temp = new List<Location>(locationReferences.Count);

            foreach (LocationReference reference in locationReferences)
            {
                temp.Add(reference.GetLocation(activityContext));
            }

            return temp;
        }

        private class CompiledDataContextActivitiesCache
        {
            private readonly bool optimized;
            private readonly HashSet<Activity> activities;
            private readonly Activity activity0;
            private readonly Activity activity1;
            private readonly Activity activity2;
            private readonly Activity activity3;
            private readonly Activity activity4;

            public CompiledDataContextActivitiesCache(HashSet<Activity> dataContextActivities)
            {
                this.activities = dataContextActivities;

                if (this.activities != null && this.activities.Count <= 5)
                {
                    Activity[] activitiesArray = new Activity[5];
                    this.activities.CopyTo(activitiesArray);

                    activity0 = activitiesArray[0];
                    activity1 = activitiesArray[1];
                    activity2 = activitiesArray[2];
                    activity3 = activitiesArray[3];
                    activity4 = activitiesArray[4];

                    this.optimized = true;
                }
            }

            public bool Contains(Activity target)
            {
                if (this.optimized)
                {
                    if (this.activity0 == target)
                    {
                        return true;
                    }
                    else if (this.activity1 == target)
                    {
                        return true;
                    }
                    else if (this.activity2 == target)
                    {
                        return true;
                    }
                    else if (this.activity3 == target)
                    {
                        return true;
                    }
                    else if (this.activity4 == target)
                    {
                        return true;
                    }

                    return false;
                }
                else
                {
                    return this.activities.Contains(target);
                }
            }
        }

        private class CompiledDataContextActivityVistor : CompiledExpressionActivityVisitor
        {
            private readonly HashSet<Activity> dataContextActivities;
            private bool inVariableScopeArgument;

            public CompiledDataContextActivityVistor()
            {
                this.dataContextActivities = new HashSet<Activity>(new ReferenceComparer<Activity>());
            }

            public HashSet<Activity> DataContextActivities
            {
                get
                {
                    return this.dataContextActivities;
                }
            }

            protected override void VisitRoot(Activity activity, out bool exit)
            {
                this.dataContextActivities.Add(activity);
                base.VisitRoot(activity, out exit);
            }
            
            protected override void VisitVariableScope(Activity activity, out bool exit)
            {
                if (!this.dataContextActivities.Contains(activity))
                {
                    this.dataContextActivities.Add(activity);
                }
                base.VisitVariableScope(activity, out exit);
            }

            protected override void VisitDelegate(ActivityDelegate activityDelegate, out bool exit)
            {
                if (activityDelegate.Handler != null)
                {
                    this.dataContextActivities.Add(activityDelegate.Handler);
                }
                base.VisitDelegate(activityDelegate, out exit);
            }

            protected override void VisitVariableScopeArgument(RuntimeArgument runtimeArgument, out bool exit)
            {
                this.inVariableScopeArgument = true;
                base.VisitVariableScopeArgument(runtimeArgument, out exit);
                this.inVariableScopeArgument = false;
            }

            protected override void VisitITextExpression(Activity activity, out bool exit)
            {
                if (this.inVariableScopeArgument)
                {
                    this.dataContextActivities.Add(activity);
                }
                base.VisitITextExpression(activity, out exit);
            }
        }

        private class ReferenceComparer<T> : IEqualityComparer<T>
        {
            bool IEqualityComparer<T>.Equals(T x, T y)
            {
                return object.ReferenceEquals(x, y);
            }

            int IEqualityComparer<T>.GetHashCode(T target)
            {
                return target.GetHashCode();
            }
        }
    }    
}
