// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Xaml;

namespace System.Activities.XamlIntegration;

internal class ConcatenatingXamlReader : XamlReader
{
    private readonly IEnumerator<XamlReader> _readers;

    // Invariant: !isEof => readers.Current != null
    private bool _isEof;

    public ConcatenatingXamlReader(params XamlReader[] readers)
    {
        _readers = ((IEnumerable<XamlReader>) readers).GetEnumerator();
        if (_readers.MoveNext())
        {
            SchemaContext = _readers.Current.SchemaContext;
        }
        else
        {
            _isEof = true;
        }
    }

    public override bool IsEof => _isEof ? true : _readers.Current.IsEof;

    public override XamlMember Member => _isEof ? null : _readers.Current.Member;

    public override NamespaceDeclaration Namespace => _isEof ? null : _readers.Current.Namespace;

    public override XamlNodeType NodeType => _isEof ? XamlNodeType.None : _readers.Current.NodeType;

    public override XamlSchemaContext SchemaContext { get; }

    public override XamlType Type => _isEof ? null : _readers.Current.Type;

    public override object Value => _isEof ? null : _readers.Current.Value;

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_isEof)
        {
            _readers.Current.Close();
            while (_readers.MoveNext())
            {
                _readers.Current.Close();
            }

            _isEof = true;
        }

        base.Dispose(disposing);
    }

    public override bool Read()
    {
        if (!_isEof)
        {
            do
            {
                if (_readers.Current.Read())
                {
                    return true;
                }

                _readers.Current.Close();
            } while (_readers.MoveNext());

            _isEof = true;
        }

        return false;
    }
}
