// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Globalization;
using System.Text.Json.Serialization;

namespace System.Activities.Validation;

[Fx.Tag.XamlVisible(false)]
public class ValidationError
{
    private Activity _source;

    public ValidationError(string message)
        : this(message, false, string.Empty) { }

    public ValidationError(string message, bool isWarning)
        : this(message, isWarning, string.Empty) { }

    public ValidationError(string message, bool isWarning, string propertyName)
        : this(message, isWarning, propertyName, null) { }

    public ValidationError(string message, bool isWarning, string propertyName, object sourceDetail)
        : this(message, isWarning, propertyName, null)
    {
        SourceDetail = sourceDetail;
    }

    internal ValidationError(string message, Activity activity)
        : this(message, false, string.Empty, activity) { }

    internal ValidationError(string message, bool isWarning, Activity activity)
        : this(message, isWarning, string.Empty, activity) { }

    internal ValidationError(string message, bool isWarning, string propertyName, Activity activity)
    {
        Message = message;
        IsWarning = isWarning;
        PropertyName = propertyName;

        if (activity != null)
        {
            Source = activity;
            Id = activity.Id;
            SourceDetail = activity.Origin;
        }
    }

    public string Message { get; internal set; }

    public bool IsWarning { get; private set; }
        
    public string PropertyName { get; private set; }

    public string Id { get; set; }

    [JsonIgnore]
    public Activity Source
    {
        get => _source;
        set
        {
            _source = value;
            if (_source != null && SourceDetail == null)
            {
                SourceDetail = _source.Origin;
            }
        }
    }
    [JsonIgnore]
    public object SourceDetail { get; set; }

    public override string ToString()
        => string.Format(CultureInfo.InvariantCulture,
            "ValidationError {{ Message = {0}, Source = {1}, PropertyName = {2}, IsWarning = {3} }}",
            Message,
            Source,
            PropertyName,
            IsWarning);
}