// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

// This causes xUnit test runs to fail with a StackOverflowException 
[assembly: Xunit.CollectionBehavior(Xunit.CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true)]
