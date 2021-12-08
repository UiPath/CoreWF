// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.
namespace System.Activities;
public class FuncValue<TResult> : CodeActivity<TResult>
{
    private readonly Func<ActivityContext, TResult> _func;
    public FuncValue(Func<ActivityContext, TResult> func) => _func = func ?? throw new ArgumentNullException(nameof(func));
    protected override TResult Execute(CodeActivityContext context)
    {
        using (context.InheritVariables())
        {
            return _func(context);
        }
    }
}
public class FuncReference<TLocation, TResult> : CodeActivity<Location<TResult>>
{
    private readonly string _locationName;
    private readonly Func<TLocation, TResult> _get;
    private readonly Func<TLocation, TResult, TLocation> _set;
    public FuncReference(string locationName, Func<TLocation, TResult> get, Func<TLocation, TResult, TLocation> set)
    {
        _locationName = locationName ?? throw new ArgumentNullException(nameof(locationName));
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _set = set ?? throw new ArgumentNullException(nameof(set));
    }
    protected override Location<TResult> Execute(CodeActivityContext context)
    {
        Location<TLocation> location;
        using (context.InheritVariables())
        {
            location = context.GetLocation<TLocation>(_locationName);
        }
        return new FuncLocation(location, _get, _set);
    }
    class FuncLocation : Location<TResult>
    {
        private readonly Location<TLocation> _location;
        private readonly Func<TLocation, TResult> _get;
        private readonly Func<TLocation, TResult, TLocation> _set;
        public FuncLocation(Location<TLocation> location, Func<TLocation, TResult> get, Func<TLocation, TResult, TLocation> set)
        {
            _location = location;
            _get = get;
            _set = set;
        }
        public override TResult Value { get => _get(_location.Value); set => _location.Value = _set(_location.Value, value); }
    }
}