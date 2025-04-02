using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Activities.Statements;

/// <summary>
/// An extension that configures/changes the behavior of some statements like Delay.
/// </summary>
public class StatementsBehaviorExtension
{
    /// <summary>
    /// When true, the delay activity will block the persistance idle event until the delay is over, 
    /// preventing the workflow from being persisted.
    /// </summary>
    public bool BlockingDelay { get; set; } = false;
}
