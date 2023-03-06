// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace System.Activities.XamlIntegration;

public abstract class CompiledDataContext
{
    private readonly IList<Location> _locations;
    private readonly IList<LocationReference> _locationReferences;
    private readonly ExpressionTreeRewriter _visitor;

    //[SuppressMessage(FxCop.Category.Usage, FxCop.Rule.DoNotCallOverridableMethodsInConstructors, Justification = "Derived classes are always generated code")]
    protected CompiledDataContext(IList<LocationReference> locationReferences, ActivityContext activityContext)
    {
        _locationReferences = locationReferences ?? new List<LocationReference>();
        _locations = ConvertReferences(_locationReferences, activityContext);
    }

    protected CompiledDataContext(IList<Location> locations)
    {
        _locations = locations;
    }

    protected CompiledDataContext(IList<LocationReference> locationReferences)
    {
        _visitor = new ExpressionTreeRewriter(locationReferences);
    }

    protected object GetVariableValue(int index) => _locations[index].Value;

    protected void SetVariableValue(int index, object value) => _locations[index].Value = value;

    protected virtual void GetValueTypeValues() { }

    protected virtual void SetValueTypeValues() { }

    protected Expression RewriteExpressionTree(Expression originalExpression)
    {

        if (originalExpression is not LambdaExpression lambdaExpression)
        {
            throw FxTrace.Exception.Argument(nameof(originalExpression), SR.LambdaExpressionTypeRequired);
        }

        if (lambdaExpression.ReturnType == null || lambdaExpression.ReturnType == typeof(void))
        {
            throw FxTrace.Exception.Argument(nameof(originalExpression), SR.LambdaExpressionReturnTypeInvalid);
        }

        return _visitor.Visit(Expression.Lambda(
            typeof(Func<,>).MakeGenericType(typeof(ActivityContext), lambdaExpression.ReturnType),
            lambdaExpression.Body,
            new ParameterExpression[] { ExpressionUtilities.RuntimeContextParameter }));
    }

    public Location<T> GetLocation<T>(Func<T> getMethod, Action<T> setMethod, int expressionId, Activity compiledRootActivity, ActivityContext activityContext)
        => new CompiledLocation<T>(getMethod, setMethod, _locationReferences, _locations, expressionId, compiledRootActivity, activityContext);

#pragma warning disable CA1822 // Mark members as static
    public Location<T> GetLocation<T>(Func<T> getMethod, Action<T> setMethod)
#pragma warning restore CA1822 // Mark members as static
        => new CompiledLocation<T>(getMethod, setMethod);

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

        HybridDictionary<Activity, CompiledDataContext[]> cache;
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
        private readonly bool _optimized;
        private readonly HashSet<Activity> _activities;
        private readonly Activity _activity0;
        private readonly Activity _activity1;
        private readonly Activity _activity2;
        private readonly Activity _activity3;
        private readonly Activity _activity4;

        public CompiledDataContextActivitiesCache(HashSet<Activity> dataContextActivities)
        {
            _activities = dataContextActivities;

            if (_activities != null && _activities.Count <= 5)
            {
                Activity[] activitiesArray = new Activity[5];
                _activities.CopyTo(activitiesArray);

                _activity0 = activitiesArray[0];
                _activity1 = activitiesArray[1];
                _activity2 = activitiesArray[2];
                _activity3 = activitiesArray[3];
                _activity4 = activitiesArray[4];

                _optimized = true;
            }
        }

        public bool Contains(Activity target)
        {
            if (_optimized)
            {
                if (_activity0 == target)
                {
                    return true;
                }
                else if (_activity1 == target)
                {
                    return true;
                }
                else if (_activity2 == target)
                {
                    return true;
                }
                else if (_activity3 == target)
                {
                    return true;
                }
                else if (_activity4 == target)
                {
                    return true;
                }

                return false;
            }
            else
            {
                return _activities.Contains(target);
            }
        }
    }

    private class CompiledDataContextActivityVistor : CompiledExpressionActivityVisitor
    {
        private readonly HashSet<Activity> _dataContextActivities;
        private bool _inVariableScopeArgument;

        public CompiledDataContextActivityVistor()
        {
            _dataContextActivities = new HashSet<Activity>(new ReferenceComparer<Activity>());
        }

        public HashSet<Activity> DataContextActivities => _dataContextActivities;

        protected override void VisitRoot(Activity activity)
        {
            _dataContextActivities.Add(activity);
            base.VisitRoot(activity);
        }

        protected override void VisitVariableScope(Activity activity)
        {
            if (!_dataContextActivities.Contains(activity))
            {
                _dataContextActivities.Add(activity);
            }
            base.VisitVariableScope(activity);
        }

        protected override void VisitDelegate(ActivityDelegate activityDelegate)
        {
            if (activityDelegate.Handler != null)
            {
                _dataContextActivities.Add(activityDelegate.Handler);
            }
            base.VisitDelegate(activityDelegate);
        }

        protected override void VisitVariableScopeArgument(RuntimeArgument runtimeArgument)
        {
            _inVariableScopeArgument = true;
            base.VisitVariableScopeArgument(runtimeArgument);
            _inVariableScopeArgument = false;
        }

        protected override void VisitITextExpression(Activity activity)
        {
            if (_inVariableScopeArgument)
            {
                _dataContextActivities.Add(activity);
            }
            base.VisitITextExpression(activity);
        }
    }

    private class ReferenceComparer<T> : IEqualityComparer<T>
    {
        bool IEqualityComparer<T>.Equals(T x, T y) => ReferenceEquals(x, y);

        int IEqualityComparer<T>.GetHashCode(T target) => target.GetHashCode();
    }
}
