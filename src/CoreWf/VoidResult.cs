using System.Threading.Tasks;

namespace System.Activities
{
    /// <summary>
    /// Represents a void type, since <see cref="System.Void"/> is not a valid return type in C#.
    /// </summary>
    /// <remarks>Based on the Unit type from MediatR.</remarks>
    internal readonly struct VoidResult : IEquatable<VoidResult>, IComparable<VoidResult>, IComparable
    {
        private static readonly VoidResult value = new();

        /// <summary>
        /// Default and only value of the <see cref="VoidResult"/> type.
        /// </summary>
        public static ref readonly VoidResult Value => ref value;

        /// <summary>
        /// Returns a task which wraps <see cref="Value"/>.
        /// </summary>
        public static Task<VoidResult> VoidTask { get; } = Task.FromResult(value);

        /// <inheritdoc />
        public int CompareTo(VoidResult other) => 0;

        /// <inheritdoc />
        int IComparable.CompareTo(object obj) => 0;

        /// <inheritdoc />
        public override int GetHashCode() => 0;

        /// <inheritdoc />
        public bool Equals(VoidResult other) => true;

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is VoidResult;

        /// <summary>
        /// Determines whether the <paramref name="first"/> object is equal to the <paramref name="second"/> object.
        /// </summary>
        /// <param name="first">The first object.</param>
        /// <param name="second">The second object.</param>
        /// <c>true</c> if the <paramref name="first"/> object is equal to the <paramref name="second" /> object; otherwise, <c>false</c>.
        public static bool operator ==(VoidResult first, VoidResult second) => first.Equals(second);

        /// <summary>
        /// Determines whether the <paramref name="first"/> object is equal to the <paramref name="second"/> object.
        /// </summary>
        /// <param name="first">The first object.</param>
        /// <param name="second">The second object.</param>
        /// <c>true</c> if the <paramref name="first"/> object is equal to the <paramref name="second" /> object; otherwise, <c>false</c>.
        public static bool operator !=(VoidResult first, VoidResult second) => !first.Equals(second);

        /// <inheritdoc />
        public override string ToString() => "()";
    }
}
