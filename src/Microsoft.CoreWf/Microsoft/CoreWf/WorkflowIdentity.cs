// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace CoreWf
{
    [DataContract]
    //[Serializable]
    //[TypeConverter(typeof(WorkflowIdentityConverter))]
    public class WorkflowIdentity : IEquatable<WorkflowIdentity>
    {
        private static Regex s_identityString = new Regex(
            @"^(?<name>[^;]*)
               (; (\s* Version \s* = \s* (?<version>[^;]*))? )?
               (; (\s* Package \s* = \s* (?<package>.*))? )?
              $", RegexOptions.IgnorePatternWhitespace);
        private const string versionString = "; Version=";
        private const string packageString = "; Package=";

        private string _name;
        private Version _version;
        private string _package;

        public WorkflowIdentity()
        {
            _name = string.Empty;
        }

        public WorkflowIdentity(string name, Version version, string package)
        {
            _name = ValidateName(name, "name");
            _version = version;
            _package = ValidatePackage(package, "package");
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = ValidateName(value, "value");
            }
        }

        public Version Version
        {
            get
            {
                return _version;
            }
            set
            {
                _version = value;
            }
        }

        public string Package
        {
            get
            {
                return _package;
            }
            set
            {
                _package = ValidatePackage(value, "value");
            }
        }

        public static WorkflowIdentity Parse(string identity)
        {
            if (identity == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("identity");
            }
            return IdentityParser.Parse(identity, true);
        }

        public static bool TryParse(string identity, out WorkflowIdentity result)
        {
            if (identity == null)
            {
                result = null;
                return false;
            }
            result = IdentityParser.Parse(identity, false);
            return result != null;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as WorkflowIdentity);
        }

        public bool Equals(WorkflowIdentity other)
        {
            return !object.ReferenceEquals(other, null) && _name == other._name &&
                _version == other._version && _package == other._package;
        }

        public override int GetHashCode()
        {
            int result = _name.GetHashCode();
            if (_version != null)
            {
                result ^= _version.GetHashCode();
            }
            if (_package != null)
            {
                result ^= _package.GetHashCode();
            }
            return result;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder(_name);
            if (_version != null)
            {
                result.Append(versionString);
                result.Append(_version.ToString());
            }
            if (_package != null)
            {
                result.Append(packageString);
                result.Append(_package);
            }
            return result.ToString();
        }

        [DataMember(EmitDefaultValue = false, Name = "name")]
        internal string SerializedName
        {
            get { return _name; }
            set { _name = value; }
        }

        // Version is [Serializable], which isn't supported in PT, so need to convert it to string
        [DataMember(EmitDefaultValue = false, Name = "version")]
        internal string SerializedVersion
        {
            get
            {
                return (_version == null) ? null : _version.ToString();
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _version = null;
                }
                else
                {
                    try
                    {
                        _version = Version.Parse(value);
                    }
                    catch (ArgumentException ex)
                    {
                        WrapInSerializationException(ex);
                    }
                    catch (FormatException ex)
                    {
                        WrapInSerializationException(ex);
                    }
                    catch (OverflowException ex)
                    {
                        WrapInSerializationException(ex);
                    }
                }
            }
        }

        [DataMember(EmitDefaultValue = false, Name = "package")]
        internal string SerializedPackage
        {
            get { return _package; }
            set { _package = value; }
        }

        // SerializationException with an InnerException is the pattern that DCS follows when values aren't convertible.
        private static void WrapInSerializationException(Exception exception)
        {
            throw CoreWf.Internals.FxTrace.Exception.AsError(new SerializationException(exception.Message, exception));
        }

        private static string ValidateName(string name, string paramName)
        {
            if (name == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull(paramName);
            }
            if (name.Contains(";"))
            {
                throw CoreWf.Internals.FxTrace.Exception.Argument(paramName, SR.IdentityNameSemicolon);
            }
            if (HasControlCharacter(name))
            {
                throw CoreWf.Internals.FxTrace.Exception.Argument(paramName, SR.IdentityControlCharacter);
            }
            if (HasLeadingOrTrailingWhitespace(name))
            {
                throw CoreWf.Internals.FxTrace.Exception.Argument(paramName, SR.IdentityWhitespace);
            }
            return Normalize(name, paramName);
        }

        private static string ValidatePackage(string package, string paramName)
        {
            if (package == null)
            {
                return null;
            }

            if (HasControlCharacter(package))
            {
                throw CoreWf.Internals.FxTrace.Exception.Argument(paramName, SR.IdentityControlCharacter);
            }
            if (HasLeadingOrTrailingWhitespace(package))
            {
                throw CoreWf.Internals.FxTrace.Exception.Argument(paramName, SR.IdentityWhitespace);
            }
            return Normalize(package, paramName);
        }

        private static bool HasControlCharacter(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsControl(value, i))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasLeadingOrTrailingWhitespace(string value)
        {
            return value.Length > 0 &&
                (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[value.Length - 1]));
        }

        private static string Normalize(string value, string paramName, bool throwOnError = true)
        {
            return value;
            //try
            //{
            //    string result = value.Normalize(NormalizationForm.FormC);
            //    for (int i = result.Length - 1; i >= 0; i--)
            //    {
            //        if (char.GetUnicodeCategory(result, i) == UnicodeCategory.Format)
            //        {
            //            result = result.Remove(i, 1);
            //        }
            //    }
            //    return result;
            //}
            //catch (ArgumentException ex)
            //{
            //    if (throwOnError)
            //    {
            //        throw CoreWf.Internals.FxTrace.Exception.AsError(new ArgumentException(ex.Message, paramName, ex));
            //    }
            //    else
            //    {
            //        return null;
            //    }
            //}
        }

        private struct IdentityParser
        {
            private const string paramName = "identity";

            private bool _throwOnError;
            private Match _match;
            private string _name;
            private Version _version;
            private string _package;

            public static WorkflowIdentity Parse(string identity, bool throwOnError)
            {
                if (HasControlCharacter(identity))
                {
                    if (throwOnError)
                    {
                        throw CoreWf.Internals.FxTrace.Exception.Argument(paramName, SR.IdentityControlCharacter);
                    }
                    return null;
                }

                IdentityParser parser = new IdentityParser();
                parser._throwOnError = throwOnError;
                parser._match = s_identityString.Match(identity.Trim());

                if (parser._match.Success)
                {
                    return parser.Parse();
                }
                else if (throwOnError)
                {
                    throw CoreWf.Internals.FxTrace.Exception.Argument(paramName, SR.BadWorkflowIdentityFormat);
                }
                else
                {
                    return null;
                }
            }

            private WorkflowIdentity Parse()
            {
                if (!ExtractName())
                {
                    return null;
                }
                if (!ExtractVersion())
                {
                    return null;
                }
                if (!ExtractPackage())
                {
                    return null;
                }

                Fx.Assert(!_name.Contains(";"), "Regex should not have matched semi-colon");
                Fx.Assert(!HasLeadingOrTrailingWhitespace(_name), "Whitespace should have been stripped");
                Fx.Assert(_package == null || !HasLeadingOrTrailingWhitespace(_package), "Whitespace should have been stripped");

                WorkflowIdentity result = new WorkflowIdentity();
                result._name = _name;
                result._version = _version;
                result._package = _package;
                return result;
            }

            private bool ExtractName()
            {
                Group nameMatch = _match.Groups["name"];
                Fx.Assert(nameMatch.Success, "RegEx requires name, even if it's empty");
                _name = Normalize(nameMatch.Value.TrimEnd(), paramName, _throwOnError);
                return _name != null;
            }

            private bool ExtractVersion()
            {
                Group versionMatch = _match.Groups["version"];
                if (versionMatch.Success)
                {
                    string versionString = versionMatch.Value;
                    if (_throwOnError)
                    {
                        _version = Version.Parse(versionString);
                    }
                    else
                    {
                        return Version.TryParse(versionString, out _version);
                    }
                }
                return true;
            }

            private bool ExtractPackage()
            {
                Group packageMatch = _match.Groups["package"];
                if (packageMatch.Success)
                {
                    _package = Normalize(packageMatch.Value, paramName, _throwOnError);
                    return _package != null;
                }
                return true;
            }
        }
    }
}
