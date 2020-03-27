// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities
{
    using System.Activities.Internals;
    using System.Activities.Runtime;
    using System.Activities.XamlIntegration;
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Text.RegularExpressions;

    [DataContract]
    [Serializable]
    [TypeConverter(TypeConverters.WorkflowIdentityConverter)]
    public class WorkflowIdentity : IEquatable<WorkflowIdentity>
    {
        private static Regex identityString = new Regex(
            @"^(?<name>[^;]*)
               (; (\s* Version \s* = \s* (?<version>[^;]*))? )?
               (; (\s* Package \s* = \s* (?<package>.*))? )?
              $", RegexOptions.IgnorePatternWhitespace);
        private const string versionString = "; Version=";
        private const string packageString = "; Package=";
        private string name;
        private Version version;
        private string package;

        public WorkflowIdentity()
        {
            this.name = string.Empty;
        }

        public WorkflowIdentity(string name, Version version, string package)
        {
            this.name = ValidateName(name, nameof(name));
            this.version = version;
            this.package = ValidatePackage(package, nameof(package));
        }

        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                this.name = ValidateName(value, nameof(value));
            }
        }

        public Version Version
        {
            get
            {
                return this.version;
            }
            set
            {
                this.version = value;
            }
        }

        public string Package
        {
            get
            {
                return this.package;
            }
            set
            {
                this.package = ValidatePackage(value, nameof(value));
            }
        }

        public static WorkflowIdentity Parse(string identity)
        {
            if (identity == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(identity));
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
            return !object.ReferenceEquals(other, null) && this.name == other.name &&
                this.version == other.version && this.package == other.package;
        }

        public override int GetHashCode()
        {
            int result = this.name.GetHashCode();
            if (this.version != null)
            {
                result ^= this.version.GetHashCode();
            }
            if (this.package != null)
            {
                result ^= this.package.GetHashCode();
            }
            return result;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder(this.name);
            if (this.version != null)
            {
                result.Append(versionString);
                result.Append(this.version.ToString());
            }
            if (this.package != null)
            {
                result.Append(packageString);
                result.Append(this.package);
            }
            return result.ToString();
        }

        [DataMember(EmitDefaultValue = false, Name = "name")]
        internal string SerializedName
        {
            get { return this.name; }
            set { this.name = value; }
        }

        // Version is [Serializable], which isn't supported in PT, so need to convert it to string
        [DataMember(EmitDefaultValue = false, Name = "version")]
        internal string SerializedVersion
        {
            get
            {
                return (this.version == null) ? null : this.version.ToString();
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    this.version = null;
                }
                else
                {
                    try
                    {
                        this.version = Version.Parse(value);
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
            get { return this.package; }
            set { this.package = value; }
        }

        // SerializationException with an InnerException is the pattern that DCS follows when values aren't convertible.
        private static void WrapInSerializationException(Exception exception)
        {
            throw FxTrace.Exception.AsError(new SerializationException(exception.Message, exception));
        }

        private static string ValidateName(string name, string paramName)
        {
            if (name == null)
            {
                throw FxTrace.Exception.ArgumentNull(paramName);
            }
            if (name.Contains(";"))
            {
                throw FxTrace.Exception.Argument(paramName, SR.IdentityNameSemicolon);
            }
            if (HasControlCharacter(name))
            {
                throw FxTrace.Exception.Argument(paramName, SR.IdentityControlCharacter);
            }
            if (HasLeadingOrTrailingWhitespace(name))
            {
                throw FxTrace.Exception.Argument(paramName, SR.IdentityWhitespace);
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
                throw FxTrace.Exception.Argument(paramName, SR.IdentityControlCharacter);
            }
            if (HasLeadingOrTrailingWhitespace(package))
            {
                throw FxTrace.Exception.Argument(paramName, SR.IdentityWhitespace);
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
            try
            {
                string result = value.Normalize(NormalizationForm.FormC);
                for (int i = result.Length - 1; i >= 0; i--)
                {
                    if (char.GetUnicodeCategory(result, i) == UnicodeCategory.Format)
                    {
                        result = result.Remove(i, 1);
                    }
                }
                return result;
            }
            catch (ArgumentException ex)
            {
                if (throwOnError)
                {
                    throw FxTrace.Exception.AsError(new ArgumentException(ex.Message, paramName, ex));
                }
                else
                {
                    return null;
                }
            }
        }

        private struct IdentityParser
        {
            private const string paramName = "identity";
            private bool throwOnError;
            private Match match;
            private string name;
            private Version version;
            private string package;

            public static WorkflowIdentity Parse(string identity, bool throwOnError)
            {
                if (HasControlCharacter(identity))
                {
                    if (throwOnError)
                    {
                        throw FxTrace.Exception.Argument(paramName, SR.IdentityControlCharacter);
                    }
                    return null;
                }

                IdentityParser parser = new IdentityParser
                {
                    throwOnError = throwOnError,
                    match = identityString.Match(identity.Trim())
                };

                if (parser.match.Success)
                {
                    return parser.Parse();
                }
                else if (throwOnError)
                {
                    throw FxTrace.Exception.Argument(paramName, SR.BadWorkflowIdentityFormat);
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

                Fx.Assert(!this.name.Contains(";"), "Regex should not have matched semi-colon");
                Fx.Assert(!HasLeadingOrTrailingWhitespace(this.name), "Whitespace should have been stripped");
                Fx.Assert(this.package == null || !HasLeadingOrTrailingWhitespace(this.package), "Whitespace should have been stripped");

                WorkflowIdentity result = new WorkflowIdentity
                {
                    name = this.name,
                    version = this.version,
                    package = this.package
                };
                return result;
            }

            private bool ExtractName()
            {
                Group nameMatch = this.match.Groups["name"];
                Fx.Assert(nameMatch.Success, "RegEx requires name, even if it's empty");
                this.name = Normalize(nameMatch.Value.TrimEnd(), paramName, this.throwOnError);
                return this.name != null;
            }

            private bool ExtractVersion()
            {
                Group versionMatch = this.match.Groups["version"];
                if (versionMatch.Success)
                {
                    string versionString = versionMatch.Value;
                    if (throwOnError)
                    {
                        this.version = Version.Parse(versionString);
                    }
                    else
                    {
                        return Version.TryParse(versionString, out this.version);
                    }
                }
                return true;
            }

            private bool ExtractPackage()
            {
                Group packageMatch = match.Groups["package"];
                if (packageMatch.Success)
                {
                    this.package = Normalize(packageMatch.Value, paramName, this.throwOnError);
                    return this.package != null;
                }
                return true;
            }
        }
    }
}
