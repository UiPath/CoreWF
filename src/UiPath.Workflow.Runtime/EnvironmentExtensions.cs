// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities
{
    internal class EnvironmentExtensions
    {
        private readonly Dictionary<Type, object> _extensions = new();

        /// <summary>
        /// Gets the specified extension.
        /// If the extension does not exist,
        /// it will invoke the <paramref name="createExtensionFactory"/> parameter
        /// </summary>
        /// <typeparam name="TExtension">The type of the extension</typeparam>
        /// <param name="createExtensionFactory">The factory to create the extension</param>
        /// <exception cref="ArgumentNullException"></exception>
        public TExtension GetOrAdd<TExtension>(Func<TExtension> createExtensionFactory) 
            where TExtension : class 
        {
            var type = typeof(TExtension);
            if (_extensions.TryGetValue(type, out object extension))
            {
                return extension as TExtension;
            }

            return CreateAndAdd();

            TExtension CreateAndAdd()
            {
                var extension = createExtensionFactory();
                if (extension is null)
                    throw new ArgumentNullException(nameof(extension));

                _extensions[type] = extension;
                return extension;
            }
        }

        /// <summary>
        /// Retrieves the extension registered for the given type
        /// or null otherwise
        /// </summary>
        /// <typeparam name="T">The type of the extension.</typeparam>
        public T Get<T>() where T : class
        {
            if (_extensions.TryGetValue(typeof(T), out object extension))
                return extension as T;
            return null;
        }

        /// <summary>
        /// Adds the specified extension to the list.
        /// The extension is treated as a singleton, 
        /// so if a second extension with the same type is added, it will
        /// throw an <see cref="InvalidOperationException"/> 
        /// </summary>
        /// <typeparam name="TExtension">The type of the extension</typeparam>
        /// <param name="extension">The extension</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void Add<TExtension>(TExtension extension) where TExtension : class
        {
            if (_extensions.ContainsKey(typeof(TExtension)))
                throw new InvalidOperationException($"Service '{typeof(TExtension).FullName}' already exists");

            _extensions[typeof(TExtension)] = extension;
        }

        internal IReadOnlyCollection<object> All => _extensions.Values;
    }
}