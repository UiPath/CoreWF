// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;

public class ExceptionPersistenceExtension
{
    private bool _persistExceptions;

    public ExceptionPersistenceExtension()
    {
        _persistExceptions = true;
    }

    public bool PersistExceptions
    {
        get => _persistExceptions;
        set => _persistExceptions = value;
    }
}
